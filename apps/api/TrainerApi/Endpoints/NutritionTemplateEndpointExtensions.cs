using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class NutritionTemplateEndpointExtensions
{
    public static void MapNutritionTemplateApi(this WebApplication app)
    {
        var templates = app.MapGroup("/api/v1/nutrition/templates").RequireAuthorization();

        templates.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var trainerId = Id(user);
            var items = await Query(db).AsNoTracking()
                .Where(x => x.TrainerId == trainerId)
                .OrderBy(x => x.Name).ThenBy(x => x.Id)
                .ToListAsync(ct);
            return Results.Ok(items.Select(x => new NutritionMealTemplateSummary(
                x.Id, x.Name, x.Notes, x.Meals.SelectMany(meal => meal.Foods).Count(),
                x.Meals.SelectMany(meal => meal.Foods).OrderBy(food => food.Sequence).Select(food => food.FoodName).ToArray(),
                x.CreatedAt, x.UpdatedAt)).ToArray());
        });

        templates.MapGet("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var template = await Query(db).AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de refeição não encontrado.", 404);
            return template.Meals.Count == 1
                ? Results.Ok(ToResponse(template))
                : context.ApiError("INVALID_NUTRITION_TEMPLATE", "Este preset não representa uma única refeição.", 409);
        });

        templates.MapPost("/", async (NutritionMealTemplateRequest? request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (Validate(request) is { } error)
                return context.ApiError("VALIDATION_ERROR", error, 400);

            var now = clock.GetUtcNow();
            var template = new NutritionTemplate
            {
                Id = Guid.NewGuid(), TrainerId = Id(user), Name = request!.Name!.Trim(),
                Notes = request.Notes?.Trim() ?? "", CreatedAt = now, UpdatedAt = now,
            };
            template.Meals.Add(CreateTemplateMeal(template.Id, request));
            db.NutritionTemplates.Add(template);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/nutrition/templates/{template.Id}", ToResponse(template));
        });

        templates.MapPut("/{id:guid}", async (Guid id, NutritionMealTemplateRequest? request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (Validate(request) is { } error)
                return context.ApiError("VALIDATION_ERROR", error, 400);

            var template = await Query(db).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de refeição não encontrado.", 404);

            template.Name = request!.Name!.Trim();
            template.Notes = request.Notes?.Trim() ?? "";
            template.UpdatedAt = clock.GetUtcNow();
            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            db.NutritionTemplateFoods.RemoveRange(template.Meals.SelectMany(x => x.Foods));
            db.NutritionTemplateMeals.RemoveRange(template.Meals);
            template.Meals.Clear();
            template.Meals.Add(CreateTemplateMeal(template.Id, request));
            db.NutritionTemplateMeals.AddRange(template.Meals);
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Results.Ok(ToResponse(template));
        });

        templates.MapDelete("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var template = await Query(db).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de refeição não encontrado.", 404);
            db.NutritionTemplates.Remove(template);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        templates.MapPost("/{id:guid}/duplicate", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var source = await Query(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (source is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de refeição não encontrado.", 404);
            if (source.Meals.Count != 1)
                return context.ApiError("INVALID_NUTRITION_TEMPLATE", "Este preset não representa uma única refeição.", 409);
            var now = clock.GetUtcNow();
            var sourceMeal = source.Meals.OrderBy(x => x.Sequence).Single();
            var copy = new NutritionTemplate
            {
                Id = Guid.NewGuid(), TrainerId = source.TrainerId, Name = CopyName(source.Name),
                Notes = source.Notes, CreatedAt = now, UpdatedAt = now,
            };
            copy.Meals.Add(CopyTemplateMeal(copy.Id, sourceMeal));
            db.NutritionTemplates.Add(copy);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(copy));
        });

        app.MapPost("/api/v1/students/{studentId:guid}/nutrition/meals/from-template/{templateId:guid}", async (
            Guid studentId, Guid templateId, PersonalUltraDbContext db, ClaimsPrincipal user,
            TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = Id(user);
            var studentName = await db.TrainerStudents.AsNoTracking()
                .Where(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null)
                .Select(x => x.Student.FirstName)
                .SingleOrDefaultAsync(ct);
            if (studentName is null)
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var template = await Query(db).AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == templateId && x.TrainerId == trainerId, ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de refeição não encontrado.", 404);
            if (template.Meals.Count != 1)
                return context.ApiError("INVALID_NUTRITION_TEMPLATE", "Este preset não representa uma única refeição.", 409);

            var plan = await db.NutritionPlans.Include(x => x.Meals)
                .SingleOrDefaultAsync(x => x.StudentId == studentId, ct);
            if (plan?.Meals.Count >= 20)
                return context.ApiError("NUTRITION_MEAL_LIMIT_REACHED", "A alimentação já possui o limite de 20 refeições.", 409);

            var now = clock.GetUtcNow();
            if (plan is null)
            {
                plan = new NutritionPlan
                {
                    Id = Guid.NewGuid(), StudentId = studentId, TrainerId = trainerId,
                    CreatedByTrainerId = trainerId, UpdatedByTrainerId = trainerId,
                    Name = $"Alimentação de {studentName}", Notes = "", CreatedAt = now,
                };
                db.NutritionPlans.Add(plan);
            }

            plan.TrainerId = trainerId;
            plan.UpdatedByTrainerId = trainerId;
            plan.UpdatedAt = now;
            var meal = CopyPlanMeal(plan.Id, template.Meals.Single(), plan.Meals.Count + 1);
            plan.Meals.Add(meal);
            db.Meals.Add(meal);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new ApplyNutritionMealTemplateResponse(plan.Id, plan.StudentId, meal.Id, meal.Name, plan.UpdatedAt, plan.Meals.Count));
        }).RequireAuthorization();
    }

    private static IQueryable<NutritionTemplate> Query(PersonalUltraDbContext db) =>
        db.NutritionTemplates.Include(x => x.Meals).ThenInclude(x => x.Foods);

    private static NutritionTemplateMeal CreateTemplateMeal(Guid templateId, NutritionMealTemplateRequest request)
    {
        var meal = new NutritionTemplateMeal
        {
            Id = Guid.NewGuid(), NutritionTemplateId = templateId, Name = request.Name!.Trim(),
            Notes = request.Notes?.Trim() ?? "", Sequence = 1,
        };
        meal.Foods.AddRange(request.Foods!.Select(x => x!).OrderBy(x => x.Sequence).Select((food, index) => new NutritionTemplateFood
        {
            Id = Guid.NewGuid(), NutritionTemplateMealId = meal.Id, FoodName = food.FoodName!.Trim(),
            Quantity = food.Quantity, Unit = food.Unit!.Trim(), Sequence = index + 1,
        }));
        return meal;
    }

    private static NutritionTemplateMeal CopyTemplateMeal(Guid templateId, NutritionTemplateMeal source)
    {
        var meal = new NutritionTemplateMeal
        {
            Id = Guid.NewGuid(), NutritionTemplateId = templateId, Name = source.Name,
            Notes = source.Notes, Sequence = 1,
        };
        meal.Foods.AddRange(source.Foods.OrderBy(x => x.Sequence).Select((food, index) => new NutritionTemplateFood
        {
            Id = Guid.NewGuid(), NutritionTemplateMealId = meal.Id, FoodName = food.FoodName,
            Quantity = food.Quantity, Unit = food.Unit, Sequence = index + 1,
        }));
        return meal;
    }

    private static Meal CopyPlanMeal(Guid planId, NutritionTemplateMeal source, int sequence)
    {
        var meal = new Meal
        {
            Id = Guid.NewGuid(), NutritionPlanId = planId, Name = source.Name,
            Notes = source.Notes, Sequence = sequence,
        };
        meal.Foods.AddRange(source.Foods.OrderBy(x => x.Sequence).Select((food, index) => new MealFood
        {
            Id = Guid.NewGuid(), MealId = meal.Id, FoodName = food.FoodName,
            Quantity = food.Quantity, Unit = food.Unit, Sequence = index + 1,
        }));
        return meal;
    }

    private static NutritionMealTemplateResponse ToResponse(NutritionTemplate template)
    {
        var meal = template.Meals.OrderBy(x => x.Sequence).Single();
        return new NutritionMealTemplateResponse(
            template.Id, template.Name, template.Notes, template.CreatedAt, template.UpdatedAt,
            meal.Foods.OrderBy(x => x.Sequence).Select(food => new NutritionMealTemplateFoodResponse(
                food.Id, food.FoodName, food.Quantity, food.Unit, food.Sequence)).ToArray());
    }

    private static string? Validate(NutritionMealTemplateRequest? request)
    {
        if (request is null) return "Informe o preset de refeição.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200) return "Informe um nome de refeição com até 200 caracteres.";
        if (request.Notes?.Length > 1000) return "As observações da refeição devem ter até 1000 caracteres.";
        if (request.Foods is null || request.Foods.Count is < 1 or > 30) return "A refeição deve ter de 1 a 30 itens.";
        if (request.Foods.Any(x => x is null)) return "Informe itens válidos para a refeição.";
        var foods = request.Foods.Select(x => x!).ToArray();
        if (foods.Any(x => string.IsNullOrWhiteSpace(x.FoodName) || x.FoodName.Length > 200)) return "Cada item deve ter nome com até 200 caracteres.";
        if (foods.Any(x => x.Quantity <= 0 || x.Quantity > 10000)) return "A quantidade de cada item deve ser maior que zero e menor ou igual a 10000.";
        if (foods.Any(x => string.IsNullOrWhiteSpace(x.Unit) || x.Unit.Length > 40)) return "Cada item deve ter uma unidade com até 40 caracteres.";
        if (foods.Any(x => x.Sequence <= 0) || foods.Select(x => x.Sequence).Distinct().Count() != foods.Length) return "As sequências dos itens devem ser positivas e distintas.";
        return null;
    }

    private static Guid Id(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string CopyName(string sourceName)
    {
        const string suffix = " (cópia)";
        return $"{sourceName[..Math.Min(sourceName.Length, 200 - suffix.Length)]}{suffix}";
    }
}
