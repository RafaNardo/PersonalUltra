namespace PersonalUltra.StudentApi.Contracts;

public sealed record StudentWorkoutSummary(Guid Id, string Name, string Notes, int RecommendedDay, bool IsRecommended, int ExerciseCount);
public sealed record StudentTrainingResponse(StudentWorkoutSummary? Recommended, IReadOnlyList<StudentWorkoutSummary> Available, IReadOnlyList<StudentTrainingHistoryItem> History);
public sealed record StudentTrainingHistoryItem(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CompletedSets);
public sealed record StudentWorkoutSessionResponse(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, IReadOnlyList<StudentSessionExercise> Exercises);
public sealed record StudentSessionExercise(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes, int CompletedSets);
public sealed record CompleteSetRequest(int SetNumber, decimal WeightKg, int Repetitions);
