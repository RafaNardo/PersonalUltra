namespace PersonalUltra.Domain;

public sealed class Exercise
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string PrimaryMuscleGroup { get; set; } = null!;
    public string? Equipment { get; set; }
    public string ImageRef { get; set; } = null!;
    public string? Instructions { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WorkoutTemplate
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public string Name { get; set; } = null!;
    public string Notes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public List<WorkoutTemplateExercise> Exercises { get; } = [];
}

public sealed class WorkoutTemplateExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutTemplateId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int RestSeconds { get; set; }
    public string Notes { get; set; } = "";
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
}

public sealed class StudentWorkout
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public Guid StudentId { get; set; }
    public string Name { get; set; } = null!;
    public string Notes { get; set; } = "";
    public int RecommendedDay { get; set; }
    public bool IsRecommended { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public List<StudentWorkoutExercise> Exercises { get; } = [];
    public List<WorkoutSession> Sessions { get; } = [];
}

public sealed class StudentWorkoutExercise
{
    public Guid Id { get; set; }
    public Guid StudentWorkoutId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int RestSeconds { get; set; }
    public string Notes { get; set; } = "";
    public StudentWorkout StudentWorkout { get; set; } = null!;
}

public sealed class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid StudentWorkoutId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = "InProgress";
    public Student Student { get; set; } = null!;
    public StudentWorkout StudentWorkout { get; set; } = null!;
    public List<WorkoutSessionExercise> Exercises { get; } = [];
}

public sealed class WorkoutSessionExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int CompletedSets { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public List<SetPerformance> Performances { get; } = [];
}

public sealed class SetPerformance
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionExerciseId { get; set; }
    public int SetNumber { get; set; }
    public decimal WeightKg { get; set; }
    public int Repetitions { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public WorkoutSessionExercise WorkoutSessionExercise { get; set; } = null!;
}
