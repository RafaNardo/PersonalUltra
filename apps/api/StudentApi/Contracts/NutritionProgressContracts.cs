namespace PersonalUltra.StudentApi.Contracts;
public sealed record StudentMealFood(string FoodName, decimal QuantityGrams);
public sealed record StudentMeal(Guid Id, string Name, int Sequence, string Notes, IReadOnlyList<StudentMealFood> Foods);
public sealed record StudentNutrition(Guid Id, string Name, string Notes, IReadOnlyList<StudentMeal> Meals);
public sealed record StudentWeight(Guid Id, decimal WeightKg, DateTimeOffset RecordedAt);
public sealed record AddWeightRequest(decimal WeightKg, DateTimeOffset? RecordedAt);
public sealed record CoachAnswer(string Answer, IReadOnlyList<string> Sources);
