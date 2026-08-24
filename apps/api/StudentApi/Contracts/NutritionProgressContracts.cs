namespace PersonalUltra.StudentApi.Contracts;
public sealed record StudentMealFood(Guid Id, string FoodName, decimal Quantity, string Unit, int Sequence);
public sealed record StudentMeal(Guid Id, string Name, int Sequence, string Notes, IReadOnlyList<StudentMealFood> Foods);
public sealed record StudentNutrition(Guid Id, string Name, string Notes, DateTimeOffset UpdatedAt, string ResponsibleTrainerName, IReadOnlyList<StudentMeal> Meals);
public sealed record StudentWeight(Guid Id, decimal WeightKg, DateTimeOffset RecordedAt);
public sealed record AddWeightRequest(decimal WeightKg, DateTimeOffset? RecordedAt);
public sealed record CoachAnswer(string Answer, IReadOnlyList<string> Sources);
