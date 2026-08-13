namespace PersonalUltra.StudentApi.Contracts;

public sealed record StudentWorkoutSummary(
    Guid Id,
    string Name,
    string Notes,
    int RecommendedDay,
    bool IsRecommended,
    int SuggestedOrder,
    int ExerciseCount,
    int PrescribedSets,
    string State,
    Guid? ActiveSessionId,
    DateTimeOffset? LastCompletedAt);
public sealed record StudentTrainingResponse(StudentWorkoutSummary? Recommended, IReadOnlyList<StudentWorkoutSummary> Available, IReadOnlyList<StudentTrainingHistoryItem> History);
public sealed record StudentTrainingHistoryItem(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CompletedSets);
public sealed record StudentWorkoutPreviewResponse(Guid Id, string Name, string Notes, int RecommendedDay, bool IsRecommended, int SuggestedOrder, string State, Guid? ActiveSessionId, DateTimeOffset? LastCompletedAt, IReadOnlyList<StudentWorkoutExercisePreview> Exercises);
public sealed record StudentWorkoutExercisePreview(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
public sealed record StudentWorkoutSessionResponse(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, IReadOnlyList<StudentSessionExercise> Exercises);
public sealed record StudentSessionDetailResponse(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, IReadOnlyList<StudentSessionExerciseDetail> Exercises);
public sealed record StudentSessionExercise(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes, int CompletedSets, StudentSetPerformance? PreviousPerformance);
public sealed record StudentSessionExerciseDetail(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes, int CompletedSets, StudentSetPerformance? PreviousPerformance, IReadOnlyList<StudentSetPerformance> Performances);
public sealed record StudentSetPerformance(int SetNumber, decimal WeightKg, int Repetitions, DateTimeOffset CompletedAt);
public sealed record CompleteSetRequest(string ClientOperationId, int SetNumber, decimal WeightKg, int Repetitions);
