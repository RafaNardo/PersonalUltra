namespace PersonalUltra.Domain;

public sealed class NutritionPlan
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public Guid StudentId { get; set; }
    public string Name { get; set; } = null!;
    public string Notes { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public List<Meal> Meals { get; } = [];
}
public sealed class Meal
{
    public Guid Id { get; set; }
    public Guid NutritionPlanId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public string Notes { get; set; } = "";
    public NutritionPlan NutritionPlan { get; set; } = null!;
    public List<MealFood> Foods { get; } = [];
}
public sealed class MealFood
{
    public Guid Id { get; set; }
    public Guid MealId { get; set; }
    public string FoodName { get; set; } = null!;
    public decimal QuantityGrams { get; set; }
    public Meal Meal { get; set; } = null!;
}
public sealed class WeightEntry
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public decimal WeightKg { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public Student Student { get; set; } = null!;
}
