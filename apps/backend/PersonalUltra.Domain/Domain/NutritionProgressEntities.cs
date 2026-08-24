namespace PersonalUltra.Domain;

public sealed class NutritionPlan
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public Guid CreatedByTrainerId { get; set; }
    public Guid UpdatedByTrainerId { get; set; }
    public Guid StudentId { get; set; }
    public string Name { get; set; } = null!;
    public string Notes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public Trainer CreatedByTrainer { get; set; } = null!;
    public Trainer UpdatedByTrainer { get; set; } = null!;
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
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public int Sequence { get; set; }
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
