namespace PersonalUltra.StudentApi.Contracts;

public sealed record StudentWorkoutSummary(Guid Id, string Name, string Notes, int RecommendedDay, bool IsRecommended, int ExerciseCount);
public sealed record StudentTrainingResponse(StudentWorkoutSummary? Recommended, IReadOnlyList<StudentWorkoutSummary> Available, IReadOnlyList<StudentTrainingHistoryItem> History);
public sealed record StudentTrainingHistoryItem(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CompletedSets);
public sealed record StudentWorkoutSessionResponse(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, IReadOnlyList<StudentSessionExercise> Exercises);
public sealed record StudentSessionExercise(Guid Id, string Name, int Sequence, int Sets, int Repetitions, int CompletedSets);
public sealed record CompleteSetRequest(int SetNumber, decimal WeightKg, int Repetitions);
