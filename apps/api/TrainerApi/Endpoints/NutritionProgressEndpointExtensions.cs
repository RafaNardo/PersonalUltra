using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class NutritionProgressEndpointExtensions
{
    public static void MapNutritionProgressApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/students").RequireAuthorization();

        api.MapGet("/{studentId:guid}/nutrition", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = Id(user);
            if (!await Own(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var plan = await db.NutritionPlans.AsNoTracking()
                .Include(x => x.Trainer).Include(x => x.Meals).ThenInclude(x => x.Foods).ThenInclude(x => x.Alternatives)
                .SingleOrDefaultAsync(x => x.StudentId == studentId, ct);
            return plan is null ? Results.Text("null", "application/json") : Results.Ok(ToResponse(plan));
        });

        api.MapPut("/{studentId:guid}/nutrition", async (Guid studentId, NutritionPlanRequest? request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = Id(user);
            if (!await Own(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            if (ValidationError(request) is { } validationError)
                return context.ApiError("VALIDATION_ERROR", validationError, 400);

            var plan = await db.NutritionPlans.Include(x => x.Meals).ThenInclude(x => x.Foods)
                .SingleOrDefaultAsync(x => x.StudentId == studentId, ct);
            var now = clock.GetUtcNow();
            if (plan is null)
            {
                plan = new NutritionPlan
                {
                    Id = Guid.NewGuid(), TrainerId = trainerId, CreatedByTrainerId = trainerId,
                    UpdatedByTrainerId = trainerId, StudentId = studentId, CreatedAt = now,
                };
                db.NutritionPlans.Add(plan);
            }

            plan.TrainerId = trainerId;
            plan.UpdatedByTrainerId = trainerId;
            plan.Name = request!.Name!.Trim();
            plan.Notes = request.Notes?.Trim() ?? "";
            plan.DailyCalories = request.DailyGoals?.Calories;
            plan.DailyProteinGrams = request.DailyGoals?.ProteinGrams;
            plan.DailyCarbohydratesGrams = request.DailyGoals?.CarbohydratesGrams;
            plan.DailyFatGrams = request.DailyGoals?.FatGrams;
            plan.UpdatedAt = now;

            db.MealFoods.RemoveRange(plan.Meals.SelectMany(x => x.Foods));
            db.Meals.RemoveRange(plan.Meals);
            plan.Meals.Clear();

            foreach (var (inputMeal, mealIndex) in request.Meals!.Select(x => x!).OrderBy(x => x.Sequence).Select((x, index) => (x, index)))
            {
                var meal = new Meal
                {
                    Id = Guid.NewGuid(), NutritionPlanId = plan.Id, Name = inputMeal.Name!.Trim(),
                    Sequence = mealIndex + 1, Notes = inputMeal.Notes?.Trim() ?? "",
                };
                meal.Foods.AddRange(inputMeal.Foods!.Select(x => x!).OrderBy(x => x.Sequence).Select((food, foodIndex) =>
                {
                    var entity = new MealFood { Id = Guid.NewGuid(), MealId = meal.Id, FoodName = food.FoodName!.Trim(),
                        Quantity = IsFree(food.Unit) ? 1 : food.Quantity, Unit = food.Unit!.Trim(), Sequence = foodIndex + 1 };
                    entity.Alternatives.AddRange(food.Alternatives?.Where(x => x is not null).Select(x => x!).OrderBy(x => x.Sequence).Select((alternative, alternativeIndex) => new MealFoodAlternative
                    {
                        Id = Guid.NewGuid(), MealFoodId = entity.Id, FoodName = alternative.FoodName!.Trim(), Quantity = IsFree(alternative.Unit) ? 1 : alternative.Quantity,
                        Unit = alternative.Unit!.Trim(), Sequence = alternativeIndex + 1, Notes = alternative.Notes?.Trim() ?? ""
                    }) ?? []);
                    return entity;
                }));
                plan.Meals.Add(meal);
                db.Meals.Add(meal);
            }

            await db.SaveChangesAsync(ct);
            plan.Trainer = await db.Trainers.SingleAsync(x => x.Id == trainerId, ct);
            return Results.Ok(ToResponse(plan));
        });

        api.MapGet("/{studentId:guid}/progress/weight", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            if (!await Own(db, Id(user), studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            return Results.Ok(await db.WeightEntries.AsNoTracking().Where(x => x.StudentId == studentId)
                .OrderBy(x => x.RecordedAt).Select(x => new WeightResponse(x.Id, x.WeightKg, x.RecordedAt)).ToListAsync(ct));
        });
    }

    private static string? ValidationError(NutritionPlanRequest? request)
    {
        if (request is null) return "Informe o plano alimentar.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200) return "Informe um nome de plano com até 200 caracteres.";
        if (request.Notes?.Length > 2000) return "As observações do plano devem ter até 2000 caracteres.";
        if (request.DailyGoals is { } goals && new[] { goals.Calories, goals.ProteinGrams, goals.CarbohydratesGrams, goals.FatGrams }.Any(value => value is < 0)) return "As metas diárias não podem ser negativas.";
        if (request.DailyGoals is { Calories: > 20000 }) return "As calorias diárias devem ser menores ou iguais a 20000.";
        if (request.DailyGoals is { } macroGoals && new[] { macroGoals.ProteinGrams, macroGoals.CarbohydratesGrams, macroGoals.FatGrams }.Any(value => value is > 2000)) return "Os macronutrientes diários devem ser menores ou iguais a 2000 g.";
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
            if (foods.Any(x => x.Alternatives is { Count: > 10 })) return "Cada item pode ter no máximo 10 alternativas.";
            foreach (var food in foods)
            {
                var alternatives = food.Alternatives?.Where(x => x is not null).Select(x => x!).ToArray() ?? [];
                if (alternatives.Length != (food.Alternatives?.Count ?? 0)) return "Informe alternativas válidas para cada item.";
                if (alternatives.Any(x => string.IsNullOrWhiteSpace(x.FoodName) || x.FoodName.Length > 200)) return "Cada alternativa deve ter nome com até 200 caracteres.";
                if (alternatives.Any(x => x.Quantity <= 0 || x.Quantity > 10000)) return "A quantidade de cada alternativa deve ser maior que zero e menor ou igual a 10000.";
                if (alternatives.Any(x => string.IsNullOrWhiteSpace(x.Unit) || x.Unit.Length > 40)) return "Cada alternativa deve ter uma unidade com até 40 caracteres.";
                if (alternatives.Any(x => x.Notes?.Length > 1000)) return "As observações das alternativas devem ter até 1000 caracteres.";
                if (alternatives.Any(x => x.Sequence <= 0) || alternatives.Select(x => x.Sequence).Distinct().Count() != alternatives.Length) return "As sequências das alternativas devem ser positivas e distintas.";
            }
        }
        return null;
    }

    private static Guid Id(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static Task<bool> Own(PersonalUltraDbContext db, Guid trainerId, Guid studentId, CancellationToken ct) =>
        db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, ct);

    private static NutritionPlanResponse ToResponse(NutritionPlan plan) => new(
        plan.Id, plan.Name, plan.Notes, plan.UpdatedAt, plan.Trainer.Name,
        plan.Meals.OrderBy(x => x.Sequence).Select(meal => new MealResponse(
            meal.Id, meal.Name, meal.Sequence, meal.Notes,
            meal.Foods.OrderBy(x => x.Sequence).Select(food => new MealFoodResponse(
                food.Id, food.FoodName, food.Quantity, food.Unit, food.Sequence,
                food.Alternatives.OrderBy(x => x.Sequence).Select(x => new MealFoodAlternativeResponse(x.Id, x.FoodName, x.Quantity, x.Unit, x.Sequence, x.Notes)).ToArray())).ToArray())).ToArray(),
        new NutritionDailyGoalsResponse(plan.DailyCalories, plan.DailyProteinGrams, plan.DailyCarbohydratesGrams, plan.DailyFatGrams));

    private static bool IsFree(string? unit) => string.Equals(unit?.Trim(), "livre", StringComparison.OrdinalIgnoreCase);
}
