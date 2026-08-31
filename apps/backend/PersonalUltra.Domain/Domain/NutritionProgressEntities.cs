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
    public decimal? DailyCalories { get; set; }
    public decimal? DailyProteinGrams { get; set; }
    public decimal? DailyCarbohydratesGrams { get; set; }
    public decimal? DailyFatGrams { get; set; }
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
    public List<MealFoodAlternative> Alternatives { get; } = [];
}
public sealed class MealFoodAlternative
{
    public Guid Id { get; set; }
    public Guid MealFoodId { get; set; }
    public string FoodName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public int Sequence { get; set; }
    public string Notes { get; set; } = "";
    public MealFood MealFood { get; set; } = null!;
}
public sealed class NutritionTemplate
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public string Name { get; set; } = null!;
    public string Notes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public List<NutritionTemplateMeal> Meals { get; } = [];
}
public sealed class NutritionTemplateMeal
{
    public Guid Id { get; set; }
    public Guid NutritionTemplateId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public string Notes { get; set; } = "";
    public NutritionTemplate NutritionTemplate { get; set; } = null!;
    public List<NutritionTemplateFood> Foods { get; } = [];
}
public sealed class NutritionTemplateFood
{
    public Guid Id { get; set; }
    public Guid NutritionTemplateMealId { get; set; }
    public string FoodName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public int Sequence { get; set; }
    public NutritionTemplateMeal Meal { get; set; } = null!;
    public List<NutritionTemplateFoodAlternative> Alternatives { get; } = [];
}
public sealed class NutritionTemplateFoodAlternative
{
    public Guid Id { get; set; }
    public Guid NutritionTemplateFoodId { get; set; }
    public string FoodName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public int Sequence { get; set; }
    public string Notes { get; set; } = "";
    public NutritionTemplateFood Food { get; set; } = null!;
}
public sealed class WeightEntry
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public decimal WeightKg { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public Student Student { get; set; } = null!;
}

public sealed class HydrationEntry
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public int AmountMl { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public Student Student { get; set; } = null!;
}
