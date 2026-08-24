namespace PersonalUltra.TrainerApi.Contracts;
public sealed record MealFoodInput(string? FoodName, decimal Quantity, string? Unit, int Sequence);
public sealed record MealInput(string? Name, int Sequence, string? Notes, IReadOnlyList<MealFoodInput?>? Foods);
public sealed record NutritionPlanRequest(string? Name, string? Notes, IReadOnlyList<MealInput?>? Meals);
public sealed record NutritionPlanResponse(Guid Id, string Name, string Notes, DateTimeOffset UpdatedAt, string ResponsibleTrainerName, IReadOnlyList<MealResponse> Meals);
public sealed record MealResponse(Guid Id, string Name, int Sequence, string Notes, IReadOnlyList<MealFoodResponse> Foods);
public sealed record MealFoodResponse(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence);
public sealed record WeightResponse(Guid Id, decimal WeightKg, DateTimeOffset RecordedAt);
