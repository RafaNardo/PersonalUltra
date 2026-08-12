namespace PersonalUltra.StudentApi.Contracts;

public sealed record ErrorResponse(string Code, string Message, object? Details, string TraceId);
public sealed record DevLoginRequest(string? Email);
public sealed record DevLoginResponse(string AccessToken, string TokenType, MemberDto Member, bool IsNewMember);
public sealed record InviteResolutionResponse(string TrainerName, string? Email, DateTimeOffset ExpiresAt);
public sealed record MemberDto(Guid Id, string FirstName, string LastName, string Email);
public sealed record BootstrapResponse(MemberDto Member, ActivePlanDto? ActivePlan, string NextRoute);
public sealed record InitialPlanResponse(bool IsProvisioned, Guid? PlanId, string? Name, int? SessionsPerWeek,
    DateOnly? StartsOn, DateTimeOffset? ReviewDueAt, IReadOnlyList<InitialPlanWorkoutDto> Workouts, InitialPlanNutritionDto? Nutrition, bool WasAlreadyProvisioned)
{
    public static readonly InitialPlanResponse NotProvisioned = new(false, null, null, null, null, null, [], null, false);
}
public sealed record InitialPlanWorkoutDto(Guid Id, string Name, int Sequence, int ExerciseCount);
public sealed record InitialPlanNutritionDto(int CaloriesTarget, int ProteinGramsTarget, int CarbsGramsTarget, int FatGramsTarget, IReadOnlyList<string> Meals);
public sealed record OnboardingProfileDto(
    string FirstName, string LastName, string Goal, string ExperienceLevel, int TrainingDaysPerWeek,
    int SessionDurationMinutes, string TrainingLocation, string EquipmentNotes, decimal HeightCm,
    decimal WeightKg, string HealthConditions, string MovementRestrictions, string CurrentPainDescription,
    string NutritionPreferences, string NutritionRestrictions, int CurrentStep, bool IsCompleted);
public sealed record SaveOnboardingProfileRequest(
    string? FirstName, string? LastName, string? Goal, string? ExperienceLevel, int? TrainingDaysPerWeek,
    int? SessionDurationMinutes, string? TrainingLocation, string? EquipmentNotes, decimal? HeightCm,
    decimal? WeightKg, string? HealthConditions, string? MovementRestrictions, string? CurrentPainDescription,
    string? NutritionPreferences, string? NutritionRestrictions, int CurrentStep);
public sealed record ActivePlanDto(Guid Id, string Name, int SessionsPerWeek, DateTimeOffset? ReviewDueAt);
public sealed record TodayWorkoutSummaryDto(Guid Id, string Name, string Status, int ExerciseCount);
public sealed record HomeResponse(string Greeting, ActivePlanDto ActivePlan, TodayWorkoutSummaryDto? TodayWorkout, int CompletedWorkoutsThisWeek);
public sealed record TrainingTodayResponse(Guid Id, string Name, string Status, DateOnly ScheduledFor, DateTimeOffset? StartedAt, IReadOnlyList<WorkoutExerciseDto> Exercises);
public sealed record WorkoutExerciseDto(Guid Id, string Name, string PrimaryMuscleGroup, int Sequence, int PrescribedSets, int MinimumRepetitions, int MaximumRepetitions, int RestSeconds, int CompletedSets);
public sealed record TrainingPlanResponse(string Name, int SessionsPerWeek, IReadOnlyList<TrainingPlanWorkoutDto> Workouts);
public sealed record TrainingPlanWorkoutDto(Guid Id, string Name, int Sequence, IReadOnlyList<TrainingPlanExerciseDto> Exercises);
public sealed record TrainingPlanExerciseDto(Guid Id, string Name, string PrimaryMuscleGroup, int Sequence, int PrescribedSets, int MinimumRepetitions, int MaximumRepetitions, int RestSeconds);
public sealed record StartWorkoutResponse(Guid Id, string Status, DateTimeOffset StartedAt, bool WasAlreadyStarted);
public sealed record CompleteSetRequest(Guid ClientOperationId, int SetNumber, decimal WeightKg, int Repetitions, int? RepsInReserve);
public sealed record CompleteSetResponse(Guid Id, Guid ClientOperationId, int SetNumber, decimal WeightKg, int Repetitions, int? RepsInReserve, DateTimeOffset CompletedAt, bool WasAlreadyProcessed);
public sealed record CompleteWorkoutResponse(Guid Id, string Status, DateTimeOffset CompletedAt, int CompletedSets);
