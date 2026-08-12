namespace PersonalUltra.TrainerApi.Contracts;

public sealed record StudentListResponse(IReadOnlyList<TrainerStudentSummary> Students);

public sealed record TrainerStudentSummary(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? Email,
    string AnamnesisStatus,
    DateTimeOffset StartedAt);

public sealed record StudentDetailResponse(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? Email,
    string AnamnesisStatus,
    DateTimeOffset StartedAt);

public sealed record TrainerAnamnesisResponse(
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
    string NutritionRestrictions,
    DateTimeOffset CompletedAt);
