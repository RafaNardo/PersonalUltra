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
public sealed record TrainerStudentWorkoutListResponse(IReadOnlyList<TrainerStudentWorkoutSummary> Workouts);
public sealed record TrainerStudentWorkoutSummary(Guid Id, string Name, string Notes, int RecommendedDay, bool IsRecommended, int ExerciseCount, DateTimeOffset CreatedAt);
public sealed record TrainerStudentWorkoutDetail(Guid Id, Guid StudentId, string Name, string Notes, int RecommendedDay, bool IsRecommended, DateTimeOffset CreatedAt, IReadOnlyList<TrainerStudentWorkoutExercise> Exercises);
public sealed record TrainerStudentWorkoutExercise(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
public sealed record TrainerStudentWorkoutUpdateRequest(IReadOnlyList<TrainerStudentWorkoutExerciseInput> Exercises);
public sealed record TrainerStudentWorkoutExerciseInput(Guid? Id, Guid? ExerciseId, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string? Notes);
public sealed record TrainerExerciseCatalogItem(Guid Id, string Name, string Slug, string PrimaryMuscleGroup, string? Equipment, string ImageRef, string? Instructions, bool IsActive);
