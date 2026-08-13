using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class TrainingEndpointExtensions
{
    public static void MapTrainingApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/training").RequireAuthorization();
        api.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var workouts = await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == studentId && x.IsActive && x.Exercises.Any()).OrderBy(x => x.SuggestedOrder).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new { x.Id, x.Name, x.Notes, x.SuggestedOrder, ExerciseCount = x.Exercises.Count, PrescribedSets = x.Exercises.Sum(e => e.Sets) }).ToListAsync(ct);
            var sessionStates = await db.WorkoutSessions.AsNoTracking().Where(x => x.StudentId == studentId).OrderByDescending(x => x.StartedAt).Select(x => new { x.Id, x.StudentWorkoutId, WorkoutName = x.StudentWorkout.Name, x.Status, x.StartedAt, x.CompletedAt, CompletedSets = x.Exercises.Sum(e => e.Performances.Count) }).ToListAsync(ct);
            var summaries = workouts.Select(workout =>
            {
                var sessions = sessionStates.Where(x => x.StudentWorkoutId == workout.Id).ToArray();
                var active = sessions.FirstOrDefault(x => x.Status == "InProgress");
                var lastCompleted = sessions.FirstOrDefault(x => x.Status == "Completed");
                var state = active is not null ? "InProgress" : lastCompleted is not null ? "Completed" : "Ready";
                return new StudentWorkoutSummary(workout.Id, workout.Name, workout.Notes, workout.SuggestedOrder, workout.ExerciseCount, workout.PrescribedSets, state, active?.Id, lastCompleted?.CompletedAt);
            }).ToList();
            var history = sessionStates.Take(20).Select(x => new StudentTrainingHistoryItem(x.Id, x.StudentWorkoutId, x.WorkoutName, x.Status, x.StartedAt, x.CompletedAt, x.CompletedSets)).ToList();
            return Results.Ok(new StudentTrainingResponse(summaries, history));
        });
        api.MapGet("/{workoutId:guid}", async (Guid workoutId, PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var workout = await db.StudentWorkouts.AsNoTracking().Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == workoutId && x.StudentId == studentId && x.IsActive && x.Exercises.Any(), ct);
            if (workout is null) return ApiEndpointExtensions.ApiError("WORKOUT_NOT_FOUND", "Este treino não está disponível.", 404);
            var sessions = await db.WorkoutSessions.AsNoTracking().Where(x => x.StudentId == studentId && x.StudentWorkoutId == workoutId).OrderByDescending(x => x.StartedAt).Select(x => new { x.Id, x.Status, x.CompletedAt }).ToListAsync(ct);
            var active = sessions.FirstOrDefault(x => x.Status == "InProgress");
            var lastCompleted = sessions.FirstOrDefault(x => x.Status == "Completed");
            var state = active is not null ? "InProgress" : lastCompleted is not null ? "Completed" : "Ready";
            return Results.Ok(new StudentWorkoutPreviewResponse(workout.Id, workout.Name, workout.Notes, workout.SuggestedOrder, state, active?.Id, lastCompleted?.CompletedAt, workout.Exercises.OrderBy(x => x.Sequence).Select(x => new StudentWorkoutExercisePreview(x.Id, x.ExerciseId, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, x.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes)).ToArray()));
        });
        api.MapPost("/{workoutId:guid}/start", async (Guid workoutId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var workout = await db.StudentWorkouts.Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == workoutId && x.StudentId == studentId && x.IsActive && x.Exercises.Any(), ct); if (workout is null) return ApiEndpointExtensions.ApiError("WORKOUT_NOT_FOUND", "Este treino não está disponível.", 404);
            var existing = await db.WorkoutSessions.Include(x => x.StudentWorkout).Include(x => x.Exercises).ThenInclude(x => x.Performances).SingleOrDefaultAsync(x => x.StudentId == studentId && x.StudentWorkoutId == workoutId && x.Status == "InProgress", ct);
            if (existing is not null) return Results.Ok(ToResponse(existing, await PreviousPerformances(db, studentId, existing.Id, existing.Exercises, ct)));
            var session = new PersonalUltra.Domain.WorkoutSession { Id = Guid.NewGuid(), StudentId = studentId, StudentWorkoutId = workoutId, StudentWorkout = workout, StartedAt = clock.GetUtcNow(), Status = "InProgress" }; session.Exercises.AddRange(workout.Exercises.OrderBy(x => x.Sequence).Select(x => PersonalUltra.Domain.WorkoutSessionExercise.FromStudentWorkout(session.Id, x))); db.WorkoutSessions.Add(session); await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(session, await PreviousPerformances(db, studentId, session.Id, session.Exercises, ct)));
        });
        api.MapGet("/sessions/{sessionId:guid}", async (Guid sessionId, PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var session = await db.WorkoutSessions.AsNoTracking()
                .Include(x => x.StudentWorkout)
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Performances)
                .SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == studentId, ct);
            if (session is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404);
            return Results.Ok(ToDetailResponse(session, await PreviousPerformances(db, studentId, session.Id, session.Exercises, ct)));
        });
        api.MapPost("/sessions/{sessionId:guid}/exercises/{exerciseId:guid}/sets", async (Guid sessionId, Guid exerciseId, CompleteSetRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            if (string.IsNullOrWhiteSpace(request.ClientOperationId) || request.ClientOperationId.Length > 200 || request.SetNumber < 1 || request.Repetitions < 1 || request.WeightKg < 0) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "Informe uma série válida.", 400);
            var exercise = await db.WorkoutSessionExercises.Include(x => x.WorkoutSession).SingleOrDefaultAsync(x => x.Id == exerciseId && x.WorkoutSessionId == sessionId && x.WorkoutSession.StudentId == studentId, ct); if (exercise is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404);
            if (request.SetNumber > exercise.Sets) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "O número da série excede a prescrição do exercício.", 400);

            var operationId = request.ClientOperationId.Trim();
            var existingOperation = await db.SetPerformances.SingleOrDefaultAsync(x => x.WorkoutSessionExerciseId == exerciseId && x.ClientOperationId == operationId, ct);
            if (existingOperation is not null)
            {
                if (existingOperation.SetNumber != request.SetNumber || existingOperation.WeightKg != request.WeightKg || existingOperation.Repetitions != request.Repetitions)
                    return ApiEndpointExtensions.ApiError("IDEMPOTENCY_CONFLICT", "Esta operação já foi usada com outros dados.", 409);
                var replayedCompletedSets = await db.SetPerformances.CountAsync(x => x.WorkoutSessionExerciseId == exerciseId, ct);
                return Results.Ok(new { saved = true, completedSets = replayedCompletedSets });
            }

            if (exercise.WorkoutSession.Status != "InProgress") return ApiEndpointExtensions.ApiError("SESSION_COMPLETED", "Esta sessão já foi concluída.", 409);

            var existingSet = await db.SetPerformances.SingleOrDefaultAsync(x => x.WorkoutSessionExerciseId == exerciseId && x.SetNumber == request.SetNumber, ct);
            if (existingSet is not null)
                return ApiEndpointExtensions.ApiError("SET_ALREADY_RECORDED", "Esta série já foi registrada.", 409);

            var recordedSetNumbers = await db.SetPerformances
                .Where(x => x.WorkoutSessionExerciseId == exerciseId)
                .Select(x => x.SetNumber)
                .ToListAsync(ct);
            var nextExpectedSet = Enumerable.Range(1, exercise.Sets).FirstOrDefault(setNumber => !recordedSetNumbers.Contains(setNumber));
            if (request.SetNumber != nextExpectedSet)
                return ApiEndpointExtensions.ApiError("SET_OUT_OF_ORDER", $"Registre primeiro a série {nextExpectedSet} deste exercício.", 409);

            db.SetPerformances.Add(new PersonalUltra.Domain.SetPerformance { Id = Guid.NewGuid(), WorkoutSessionExerciseId = exerciseId, ClientOperationId = operationId, SetNumber = request.SetNumber, WeightKg = request.WeightKg, Repetitions = request.Repetitions, CompletedAt = clock.GetUtcNow() });
            var persistedCompletedSets = await db.SetPerformances.CountAsync(x => x.WorkoutSessionExerciseId == exerciseId, ct);
            exercise.CompletedSets = Math.Min(exercise.Sets, persistedCompletedSets + 1);
            try
            {
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { saved = true, exercise.CompletedSets });
            }
            catch (DbUpdateException)
            {
                // The unique operation/set indexes close the race between concurrent retries.
                db.ChangeTracker.Clear();
                var concurrentOperation = await db.SetPerformances.AsNoTracking().SingleOrDefaultAsync(x => x.WorkoutSessionExerciseId == exerciseId && x.ClientOperationId == operationId, ct);
                if (concurrentOperation is not null && concurrentOperation.SetNumber == request.SetNumber && concurrentOperation.WeightKg == request.WeightKg && concurrentOperation.Repetitions == request.Repetitions)
                {
                    var completedSets = await db.SetPerformances.CountAsync(x => x.WorkoutSessionExerciseId == exerciseId, ct);
                    return Results.Ok(new { saved = true, completedSets });
                }
                return ApiEndpointExtensions.ApiError("SET_ALREADY_RECORDED", "Esta série já foi registrada.", 409);
            }
        });
        api.MapPost("/sessions/{sessionId:guid}/complete", async (Guid sessionId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var session = await db.WorkoutSessions
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Performances)
                .SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == studentId, ct);
            if (session is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404);
            if (session.Status == "Completed") return Results.Ok(new { session.Id, session.Status, session.CompletedAt });
            if (session.Exercises.Any(x => x.Performances.Count < x.Sets))
                return ApiEndpointExtensions.ApiError("SESSION_INCOMPLETE", "Registre todas as séries antes de concluir o treino.", 409);
            session.Status = "Completed";
            session.CompletedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { session.Id, session.Status, session.CompletedAt });
        });
    }
    private static bool StudentId(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && user.FindFirstValue("subject") == "student";
    private static StudentWorkoutSessionResponse ToResponse(PersonalUltra.Domain.WorkoutSession session, IReadOnlyDictionary<Guid, StudentSetPerformance> previous) => new(session.Id, session.StudentWorkoutId, session.StudentWorkout?.Name ?? "Treino", session.Status, session.StartedAt, session.CompletedAt, session.Exercises.OrderBy(x => x.Sequence).Select(x => new StudentSessionExercise(x.Id, x.ExerciseId, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, x.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes, Math.Min(x.Sets, x.Performances.Count), PreviousFor(x, previous))).ToArray());
    private static StudentSessionDetailResponse ToDetailResponse(PersonalUltra.Domain.WorkoutSession session, IReadOnlyDictionary<Guid, StudentSetPerformance> previous) => new(session.Id, session.StudentWorkoutId, session.StudentWorkout?.Name ?? "Treino", session.Status, session.StartedAt, session.CompletedAt, session.Exercises.OrderBy(x => x.Sequence).Select(x => new StudentSessionExerciseDetail(x.Id, x.ExerciseId, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, x.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes, Math.Min(x.Sets, x.Performances.Count), PreviousFor(x, previous), x.Performances.OrderBy(p => p.SetNumber).Select(p => new StudentSetPerformance(p.SetNumber, p.WeightKg, p.Repetitions, p.CompletedAt)).ToArray())).ToArray());
    private static StudentSetPerformance? PreviousFor(PersonalUltra.Domain.WorkoutSessionExercise exercise, IReadOnlyDictionary<Guid, StudentSetPerformance> previous) => exercise.ExerciseId is Guid exerciseId && previous.TryGetValue(exerciseId, out var performance) ? performance : null;
    private static async Task<IReadOnlyDictionary<Guid, StudentSetPerformance>> PreviousPerformances(PersonalUltraDbContext db, Guid studentId, Guid currentSessionId, IEnumerable<PersonalUltra.Domain.WorkoutSessionExercise> exercises, CancellationToken ct)
    {
        var exerciseIds = exercises.Where(x => x.ExerciseId.HasValue).Select(x => x.ExerciseId!.Value).Distinct().ToArray();
        if (exerciseIds.Length == 0) return new Dictionary<Guid, StudentSetPerformance>();
        var history = await db.SetPerformances.AsNoTracking()
            .Where(x => x.WorkoutSessionExercise.WorkoutSession.StudentId == studentId && x.WorkoutSessionExercise.WorkoutSessionId != currentSessionId && x.WorkoutSessionExercise.ExerciseId.HasValue && exerciseIds.Contains(x.WorkoutSessionExercise.ExerciseId.Value))
            .OrderByDescending(x => x.CompletedAt)
            .Select(x => new { ExerciseId = x.WorkoutSessionExercise.ExerciseId!.Value, x.SetNumber, x.WeightKg, x.Repetitions, x.CompletedAt })
            .ToListAsync(ct);
        return history.GroupBy(x => x.ExerciseId).ToDictionary(x => x.Key, x => { var latest = x.First(); return new StudentSetPerformance(latest.SetNumber, latest.WeightKg, latest.Repetitions, latest.CompletedAt); });
    }
}
