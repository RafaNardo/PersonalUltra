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
            return Results.Ok(await db.NutritionTemplates.AsNoTracking()
                .Where(x => x.TrainerId == trainerId)
                .OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Select(x => new NutritionTemplateSummary(
                    x.Id, x.Name, x.Notes, x.Meals.Count, x.Meals.SelectMany(meal => meal.Foods).Count(), x.CreatedAt, x.UpdatedAt))
                .ToListAsync(ct));
        });

        templates.MapGet("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var template = await Query(db).AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            return template is null
                ? context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de alimentação não encontrado.", 404)
                : Results.Ok(ToResponse(template));
        });

        templates.MapPost("/", async (NutritionPlanRequest? request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (Validate(request) is { } error)
                return context.ApiError("VALIDATION_ERROR", error, 400);

            var now = clock.GetUtcNow();
            var template = new NutritionTemplate
            {
                Id = Guid.NewGuid(), TrainerId = Id(user), Name = request!.Name!.Trim(),
                Notes = request.Notes?.Trim() ?? "", CreatedAt = now, UpdatedAt = now,
            };
            AddMeals(template, request.Meals!);
            db.NutritionTemplates.Add(template);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/nutrition/templates/{template.Id}", ToResponse(template));
        });

        templates.MapPut("/{id:guid}", async (Guid id, NutritionPlanRequest? request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (Validate(request) is { } error)
                return context.ApiError("VALIDATION_ERROR", error, 400);

            var template = await Query(db).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de alimentação não encontrado.", 404);

            template.Name = request!.Name!.Trim();
            template.Notes = request.Notes?.Trim() ?? "";
            template.UpdatedAt = clock.GetUtcNow();
            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            db.NutritionTemplateFoods.RemoveRange(template.Meals.SelectMany(x => x.Foods));
            db.NutritionTemplateMeals.RemoveRange(template.Meals);
            template.Meals.Clear();
            AddMeals(template, request.Meals!);
            db.NutritionTemplateMeals.AddRange(template.Meals);
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Results.Ok(ToResponse(template));
        });

        templates.MapDelete("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var template = await Query(db).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de alimentação não encontrado.", 404);
            db.NutritionTemplates.Remove(template);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        templates.MapPost("/{id:guid}/duplicate", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var source = await Query(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == Id(user), ct);
            if (source is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de alimentação não encontrado.", 404);
            var now = clock.GetUtcNow();
            var copy = new NutritionTemplate
            {
                Id = Guid.NewGuid(), TrainerId = source.TrainerId, Name = CopyName(source.Name),
                Notes = source.Notes, CreatedAt = now, UpdatedAt = now,
            };
            CopyMeals(copy, source.Meals);
            db.NutritionTemplates.Add(copy);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(copy));
        });

        app.MapPost("/api/v1/students/{studentId:guid}/nutrition/from-template/{templateId:guid}", async (
            Guid studentId, Guid templateId, bool? replaceExisting, PersonalUltraDbContext db, ClaimsPrincipal user,
            TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = Id(user);
            if (!await db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            var template = await Query(db).AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == templateId && x.TrainerId == trainerId, ct);
            if (template is null)
                return context.ApiError("NUTRITION_TEMPLATE_NOT_FOUND", "Preset de alimentação não encontrado.", 404);

            var plan = await db.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods)
                .SingleOrDefaultAsync(x => x.StudentId == studentId, ct);
            if (plan is not null && replaceExisting is not true)
                return context.ApiError("NUTRITION_PLAN_ALREADY_EXISTS", "Este aluno já possui uma alimentação. Confirme a substituição para continuar.", 409);

            var now = clock.GetUtcNow();
            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            if (plan is null)
            {
                plan = new NutritionPlan
                {
                    Id = Guid.NewGuid(), StudentId = studentId, TrainerId = trainerId,
                    CreatedByTrainerId = trainerId, UpdatedByTrainerId = trainerId, CreatedAt = now,
                };
                db.NutritionPlans.Add(plan);
            }
            else
            {
                db.MealFoods.RemoveRange(plan.Meals.SelectMany(x => x.Foods));
                db.Meals.RemoveRange(plan.Meals);
                plan.Meals.Clear();
            }

            plan.TrainerId = trainerId;
            plan.UpdatedByTrainerId = trainerId;
            plan.Name = template.Name;
            plan.Notes = template.Notes;
            plan.UpdatedAt = now;
            CopyMeals(plan, template.Meals);
            db.Meals.AddRange(plan.Meals);
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Results.Ok(new ApplyNutritionTemplateResponse(plan.Id, plan.StudentId, plan.Name, plan.UpdatedAt, plan.Meals.Count));
        }).RequireAuthorization();
    }

    private static IQueryable<NutritionTemplate> Query(PersonalUltraDbContext db) =>
        db.NutritionTemplates.Include(x => x.Meals).ThenInclude(x => x.Foods);

    private static void AddMeals(NutritionTemplate template, IReadOnlyList<MealInput?> inputs)
    {
        foreach (var (input, mealIndex) in inputs.Select(x => x!).OrderBy(x => x.Sequence).Select((x, i) => (x, i)))
        {
            var meal = new NutritionTemplateMeal
            {
                Id = Guid.NewGuid(), NutritionTemplateId = template.Id, Name = input.Name!.Trim(),
                Notes = input.Notes?.Trim() ?? "", Sequence = mealIndex + 1,
            };
            meal.Foods.AddRange(input.Foods!.Select(x => x!).OrderBy(x => x.Sequence).Select((food, foodIndex) => new NutritionTemplateFood
            {
                Id = Guid.NewGuid(), NutritionTemplateMealId = meal.Id, FoodName = food.FoodName!.Trim(),
                Quantity = food.Quantity, Unit = food.Unit!.Trim(), Sequence = foodIndex + 1,
            }));
            template.Meals.Add(meal);
        }
    }

    private static void CopyMeals(NutritionTemplate target, IEnumerable<NutritionTemplateMeal> source)
    {
        foreach (var input in source.OrderBy(x => x.Sequence))
        {
            var meal = new NutritionTemplateMeal { Id = Guid.NewGuid(), NutritionTemplateId = target.Id, Name = input.Name, Notes = input.Notes, Sequence = input.Sequence };
            meal.Foods.AddRange(input.Foods.OrderBy(x => x.Sequence).Select(food => new NutritionTemplateFood
            {
                Id = Guid.NewGuid(), NutritionTemplateMealId = meal.Id, FoodName = food.FoodName,
                Quantity = food.Quantity, Unit = food.Unit, Sequence = food.Sequence,
            }));
            target.Meals.Add(meal);
        }
    }

    private static void CopyMeals(NutritionPlan target, IEnumerable<NutritionTemplateMeal> source)
    {
        foreach (var input in source.OrderBy(x => x.Sequence))
        {
            var meal = new Meal { Id = Guid.NewGuid(), NutritionPlanId = target.Id, Name = input.Name, Notes = input.Notes, Sequence = input.Sequence };
            meal.Foods.AddRange(input.Foods.OrderBy(x => x.Sequence).Select(food => new MealFood
            {
                Id = Guid.NewGuid(), MealId = meal.Id, FoodName = food.FoodName,
                Quantity = food.Quantity, Unit = food.Unit, Sequence = food.Sequence,
            }));
            target.Meals.Add(meal);
        }
    }

    private static NutritionTemplateResponse ToResponse(NutritionTemplate template) => new(
        template.Id, template.Name, template.Notes, template.CreatedAt, template.UpdatedAt,
        template.Meals.OrderBy(x => x.Sequence).Select(meal => new NutritionTemplateMealResponse(
            meal.Id, meal.Name, meal.Sequence, meal.Notes,
            meal.Foods.OrderBy(x => x.Sequence).Select(food => new NutritionTemplateFoodResponse(
                food.Id, food.FoodName, food.Quantity, food.Unit, food.Sequence)).ToArray())).ToArray());

    private static string? Validate(NutritionPlanRequest? request)
    {
        if (request is null) return "Informe o preset de alimentação.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200) return "Informe um nome de preset com até 200 caracteres.";
        if (request.Notes?.Length > 2000) return "As observações do preset devem ter até 2000 caracteres.";
        if (request.Meals is null || request.Meals.Count is < 1 or > 20) return "Informe de 1 a 20 refeições.";
        if (request.Meals.Any(x => x is null)) return "Informe refeições válidas.";
        var meals = request.Meals.Select(x => x!).ToArray();
        if (meals.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 200)) return "Cada refeição deve ter nome com até 200 caracteres.";
        if (meals.Any(x => x.Notes?.Length > 1000)) return "As observações de cada refeição devem ter até 1000 caracteres.";
        if (meals.Any(x => x.Sequence <= 0) || meals.Select(x => x.Sequence).Distinct().Count() != meals.Length) return "As sequências das refeições devem ser positivas e distintas.";
        if (meals.Any(x => x.Foods is null || x.Foods.Count is < 1 or > 30)) return "Cada refeição deve ter de 1 a 30 itens.";
        if (meals.SelectMany(x => x.Foods!).Any(x => x is null)) return "Informe itens válidos para cada refeição.";
        foreach (var meal in meals)
        {
            var foods = meal.Foods!.Select(x => x!).ToArray();
            if (foods.Any(x => string.IsNullOrWhiteSpace(x.FoodName) || x.FoodName.Length > 200)) return "Cada item deve ter nome com até 200 caracteres.";
            if (foods.Any(x => x.Quantity <= 0 || x.Quantity > 10000)) return "A quantidade de cada item deve ser maior que zero e menor ou igual a 10000.";
            if (foods.Any(x => string.IsNullOrWhiteSpace(x.Unit) || x.Unit.Length > 40)) return "Cada item deve ter uma unidade com até 40 caracteres.";
            if (foods.Any(x => x.Sequence <= 0) || foods.Select(x => x.Sequence).Distinct().Count() != foods.Length) return "As sequências dos itens devem ser positivas e distintas em cada refeição.";
        }
        return null;
    }

    private static Guid Id(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string CopyName(string sourceName)
    {
        const string suffix = " (cópia)";
        return $"{sourceName[..Math.Min(sourceName.Length, 200 - suffix.Length)]}{suffix}";
    }
}
