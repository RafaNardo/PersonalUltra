namespace PersonalUltra.TrainerApi.Contracts;

public sealed record WorkoutTemplateSummary(Guid Id, string Name, string Notes, int ExerciseCount, DateTimeOffset UpdatedAt);
public sealed record WorkoutTemplateExerciseInput(Guid ExerciseId, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string? Notes);
public sealed record WorkoutTemplateExerciseResponse(Guid ExerciseId, string Name, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string? Notes);
public sealed record WorkoutTemplateRequest(string Name, string? Notes, IReadOnlyList<WorkoutTemplateExerciseInput> Exercises);
public sealed record WorkoutTemplateResponse(Guid Id, string Name, string Notes, IReadOnlyList<WorkoutTemplateExerciseResponse> Exercises);
public sealed record ApplyWorkoutRequest(Guid StudentId, int RecommendedDay, bool IsRecommended);
public sealed record AppliedWorkoutResponse(Guid Id, Guid StudentId, string Name, int RecommendedDay, bool IsRecommended, int ExerciseCount);
public sealed record StudentTrainingHistoryResponse(IReadOnlyList<TrainingHistoryItem> Sessions);
public sealed record TrainingHistoryItem(Guid SessionId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CompletedSets);
