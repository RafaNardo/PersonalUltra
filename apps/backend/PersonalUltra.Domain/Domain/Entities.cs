namespace PersonalUltra.Domain;

public static class DemoIds
{
    public static readonly Guid TrainerId = Guid.Parse("e9ce4c89-aa19-4e38-b4a4-50cb1e62496e");
    public static readonly Guid StudentId = Guid.Parse("f87a146d-2a1f-41a7-bd6a-732796d22384");
    public static readonly Guid UserId = Guid.Parse("3baaf4fc-856d-4cf1-8a2d-78dd637fdca9");
    public static readonly Guid MemberId = Guid.Parse("ad739a47-9194-4be4-b017-8c06f6c0383a");
    public const string Email = "demo@svr.method";
}

public sealed class AuthUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public Member Member { get; set; } = null!;
}

public sealed class Member
{
    public Guid Id { get; set; }
    public Guid AuthUserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    // The detailed onboarding data arrives in M2-A-2; this lifecycle marker is
    // needed now so bootstrap can distinguish it from plan preparation.
    public DateTimeOffset? OnboardingCompletedAt { get; set; }
    public AuthUser AuthUser { get; set; } = null!;
    public MemberProfile? Profile { get; set; }
    public List<Plan> Plans { get; } = [];
    public List<WorkoutSession> WorkoutSessions { get; } = [];
}

// Profile data is descriptive input supplied by the member. It intentionally
// contains no diagnosis, prescription or generated training decision.
public sealed class MemberProfile
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Goal { get; set; } = null!;
    public string ExperienceLevel { get; set; } = null!;
    public int TrainingDaysPerWeek { get; set; }
    public int SessionDurationMinutes { get; set; }
    public string TrainingLocation { get; set; } = null!;
    public string EquipmentNotes { get; set; } = null!;
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public string HealthConditions { get; set; } = null!;
    public string MovementRestrictions { get; set; } = null!;
    public string CurrentPainDescription { get; set; } = null!;
    public string NutritionPreferences { get; set; } = null!;
    public string NutritionRestrictions { get; set; } = null!;
    public int CurrentStep { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Member Member { get; set; } = null!;
}

public sealed class Plan
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly StartsOn { get; set; }
    public DateTimeOffset? ReviewDueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Member Member { get; set; } = null!;
    public TrainingPlan TrainingPlan { get; set; } = null!;
}

public sealed class TrainingPlan
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int SessionsPerWeek { get; set; }
    public Plan Plan { get; set; } = null!;
    public List<WorkoutTemplate> WorkoutTemplates { get; } = [];
}

public sealed class Exercise
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string PrimaryMuscleGroup { get; set; } = null!;
    public List<WorkoutTemplateExercise> WorkoutTemplateExercises { get; } = [];
}

public sealed class WorkoutTemplate
{
    public Guid Id { get; set; }
    public Guid TrainingPlanId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public TrainingPlan TrainingPlan { get; set; } = null!;
    public List<WorkoutTemplateExercise> Exercises { get; } = [];
    public List<WorkoutSession> WorkoutSessions { get; } = [];
}

public sealed class WorkoutTemplateExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutTemplateId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Sequence { get; set; }
    public int PrescribedSets { get; set; }
    public int MinimumRepetitions { get; set; }
    public int MaximumRepetitions { get; set; }
    public int RestSeconds { get; set; }
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}

public sealed class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid WorkoutTemplateId { get; set; }
    public DateOnly ScheduledFor { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Member Member { get; set; } = null!;
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
    public List<WorkoutSessionExercise> Exercises { get; } = [];
}

public sealed class WorkoutSessionExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Sequence { get; set; }
    public int PrescribedSets { get; set; }
    public int MinimumRepetitions { get; set; }
    public int MaximumRepetitions { get; set; }
    public int RestSeconds { get; set; }
    public string ExerciseSnapshotJson { get; set; } = "{}";
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public List<SetPerformance> SetPerformances { get; } = [];
}

public sealed class SetPerformance
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionExerciseId { get; set; }
    public Guid ClientOperationId { get; set; }
    public int SetNumber { get; set; }
    public decimal WeightKg { get; set; }
    public int Repetitions { get; set; }
    public int? RepsInReserve { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public WorkoutSessionExercise WorkoutSessionExercise { get; set; } = null!;
}

public sealed class WeightEntry
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public decimal WeightKg { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public Member Member { get; set; } = null!;
}

public sealed class NutritionPlan
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int CaloriesTarget { get; set; }
    public int ProteinGramsTarget { get; set; }
    public int CarbsGramsTarget { get; set; }
    public int FatGramsTarget { get; set; }
    public Plan Plan { get; set; } = null!;
    public List<MealTemplate> Meals { get; } = [];
}

public sealed class Food
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public decimal CaloriesPer100g { get; set; }
    public decimal ProteinPer100g { get; set; }
    public decimal CarbsPer100g { get; set; }
    public decimal FatPer100g { get; set; }
}

public sealed class MealTemplate
{
    public Guid Id { get; set; }
    public Guid NutritionPlanId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public NutritionPlan NutritionPlan { get; set; } = null!;
    public List<MealTemplateFood> Foods { get; } = [];
}

public sealed class MealTemplateFood
{
    public Guid Id { get; set; }
    public Guid MealTemplateId { get; set; }
    public Guid FoodId { get; set; }
    public decimal QuantityGrams { get; set; }
    public MealTemplate MealTemplate { get; set; } = null!;
    public Food Food { get; set; } = null!;
}

public sealed class DailyLog
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public DateOnly Date { get; set; }
    public Guid MealTemplateId { get; set; }
    public bool Completed { get; set; }
    public Member Member { get; set; } = null!;
    public MealTemplate MealTemplate { get; set; } = null!;
}

public sealed class Conversation
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Member Member { get; set; } = null!;
    public List<CoachMessage> Messages { get; } = [];
}

public sealed class CoachMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Conversation Conversation { get; set; } = null!;
}

public sealed class PainReport
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Area { get; set; } = null!;
    public string Side { get; set; } = null!;
    public int Intensity { get; set; }
    public string Context { get; set; } = null!;
    public string SafetyLevel { get; set; } = null!;
    public string ReasonCode { get; set; } = "PAIN_REASON_NOT_RECORDED";
    public DateTimeOffset ReportedAt { get; set; }
}
