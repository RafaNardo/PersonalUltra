using PersonalUltra.Api.Domain;

namespace PersonalUltra.Api.Application.Nutrition;

/// <summary>
/// Produces only food substitutions approved by the v0 nutrition rule: the
/// replacement must be in the same catalog category and preserve calories.
/// </summary>
public sealed class FoodAlternativesEngine
{
    public const string CalorieEquivalentReasonCode = "NUTRITION_CALORIE_EQUIVALENT";

    public IReadOnlyList<FoodAlternativeDecision> FindApprovedAlternatives(
        MealTemplateFood original,
        IEnumerable<Food> catalog)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!HasValidNutrition(original.Food) || original.QuantityGrams <= 0)
        {
            return [];
        }

        return catalog
            .Where(candidate => IsApprovedAlternative(original.Food, candidate))
            .OrderBy(candidate => candidate.Name, StringComparer.Ordinal)
            .Select(candidate => new FoodAlternativeDecision(
                candidate.Id,
                candidate.Name,
                CalculateCalorieEquivalentQuantity(original.QuantityGrams, original.Food, candidate),
                CalorieEquivalentReasonCode))
            .ToList();
    }

    public bool IsApprovedAlternative(Food original, Food candidate) =>
        original is not null &&
        candidate is not null &&
        original.Id != candidate.Id &&
        HasValidNutrition(original) &&
        HasValidNutrition(candidate) &&
        string.Equals(original.Category, candidate.Category, StringComparison.Ordinal);

    public decimal CalculateCalorieEquivalentQuantity(decimal originalQuantityGrams, Food original, Food replacement)
    {
        if (originalQuantityGrams <= 0 || !IsApprovedAlternative(original, replacement))
        {
            throw new ArgumentException("A substitution must use valid foods from the same category.");
        }

        // Whole grams are usable in the meal UI. AwayFromZero makes the rule
        // deterministic at .5 boundaries and keeps the suggested quantity positive.
        return Math.Max(1, Math.Round(
            originalQuantityGrams * original.CaloriesPer100g / replacement.CaloriesPer100g,
            0,
            MidpointRounding.AwayFromZero));
    }

    private static bool HasValidNutrition(Food food) =>
        food.CaloriesPer100g > 0 &&
        food.ProteinPer100g >= 0 &&
        food.CarbsPer100g >= 0 &&
        food.FatPer100g >= 0;
}

public sealed record FoodAlternativeDecision(
    Guid FoodId,
    string Name,
    decimal SuggestedQuantityGrams,
    string ReasonCode);
