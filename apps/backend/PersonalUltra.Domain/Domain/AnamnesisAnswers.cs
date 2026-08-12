namespace PersonalUltra.Domain;

/// <summary>
/// Descriptive information supplied by a student for their Trainer. It does
/// not contain a diagnosis, a prescription, or an automatic training decision.
/// </summary>
public sealed record AnamnesisAnswers(
    string Goal,
    string ExperienceLevel,
    int TrainingDaysPerWeek,
    int SessionDurationMinutes,
    string TrainingLocation,
    string EquipmentNotes,
    decimal HeightCm,
    decimal WeightKg,
    string HealthConditions,
    string MovementRestrictions,
    string CurrentPainDescription,
    string NutritionPreferences,
    string NutritionRestrictions);
