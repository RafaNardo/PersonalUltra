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
            var workouts = await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == studentId).OrderBy(x => x.RecommendedDay).ThenBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Notes, x.RecommendedDay, x.IsRecommended, ExerciseCount = x.Exercises.Count, PrescribedSets = x.Exercises.Sum(e => e.Sets) }).ToListAsync(ct);
            var sessionStates = await db.WorkoutSessions.AsNoTracking().Where(x => x.StudentId == studentId).OrderByDescending(x => x.StartedAt).Select(x => new { x.Id, x.StudentWorkoutId, x.Status, x.StartedAt, x.CompletedAt, CompletedSets = x.Exercises.Sum(e => e.CompletedSets) }).ToListAsync(ct);
            var summaries = workouts.Select(workout =>
            {
                var sessions = sessionStates.Where(x => x.StudentWorkoutId == workout.Id).ToArray();
                var active = sessions.FirstOrDefault(x => x.Status == "InProgress");
                var lastCompleted = sessions.FirstOrDefault(x => x.Status == "Completed");
                var state = active is not null ? "InProgress" : lastCompleted is not null ? "Completed" : workout.IsRecommended ? "Recommended" : "Available";
                return new StudentWorkoutSummary(workout.Id, workout.Name, workout.Notes, workout.RecommendedDay, workout.IsRecommended, workout.ExerciseCount, workout.PrescribedSets, state, active?.Id, lastCompleted?.CompletedAt);
            }).ToList();
            var history = sessionStates.Take(20).Select(x => new StudentTrainingHistoryItem(x.Id, x.StudentWorkoutId, workouts.First(w => w.Id == x.StudentWorkoutId).Name, x.Status, x.StartedAt, x.CompletedAt, x.CompletedSets)).ToList();
            return Results.Ok(new StudentTrainingResponse(summaries.FirstOrDefault(x => x.IsRecommended), summaries.Where(x => !x.IsRecommended).ToArray(), history));
        });
        api.MapGet("/{workoutId:guid}", async (Guid workoutId, PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var workout = await db.StudentWorkouts.AsNoTracking().Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == workoutId && x.StudentId == studentId, ct);
            if (workout is null) return ApiEndpointExtensions.ApiError("WORKOUT_NOT_FOUND", "Este treino não está disponível.", 404);
            var sessions = await db.WorkoutSessions.AsNoTracking().Where(x => x.StudentId == studentId && x.StudentWorkoutId == workoutId).OrderByDescending(x => x.StartedAt).Select(x => new { x.Id, x.Status, x.CompletedAt }).ToListAsync(ct);
            var active = sessions.FirstOrDefault(x => x.Status == "InProgress");
            var lastCompleted = sessions.FirstOrDefault(x => x.Status == "Completed");
            var state = active is not null ? "InProgress" : lastCompleted is not null ? "Completed" : workout.IsRecommended ? "Recommended" : "Available";
            return Results.Ok(new StudentWorkoutPreviewResponse(workout.Id, workout.Name, workout.Notes, workout.RecommendedDay, workout.IsRecommended, state, active?.Id, lastCompleted?.CompletedAt, workout.Exercises.OrderBy(x => x.Sequence).Select(x => new StudentWorkoutExercisePreview(x.Id, x.ExerciseId, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, x.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes)).ToArray()));
        });
        api.MapPost("/{workoutId:guid}/start", async (Guid workoutId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var workout = await db.StudentWorkouts.Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == workoutId && x.StudentId == studentId, ct); if (workout is null) return ApiEndpointExtensions.ApiError("WORKOUT_NOT_FOUND", "Este treino não está disponível.", 404);
            var existing = await db.WorkoutSessions.Include(x => x.StudentWorkout).Include(x => x.Exercises).SingleOrDefaultAsync(x => x.StudentId == studentId && x.StudentWorkoutId == workoutId && x.Status == "InProgress", ct);
            if (existing is not null) return Results.Ok(ToResponse(existing));
            var session = new PersonalUltra.Domain.WorkoutSession { Id = Guid.NewGuid(), StudentId = studentId, StudentWorkoutId = workoutId, StudentWorkout = workout, StartedAt = clock.GetUtcNow(), Status = "InProgress" }; session.Exercises.AddRange(workout.Exercises.OrderBy(x => x.Sequence).Select(x => PersonalUltra.Domain.WorkoutSessionExercise.FromStudentWorkout(session.Id, x))); db.WorkoutSessions.Add(session); await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(session));
        });
        api.MapPost("/sessions/{sessionId:guid}/exercises/{exerciseId:guid}/sets", async (Guid sessionId, Guid exerciseId, CompleteSetRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            if (string.IsNullOrWhiteSpace(request.ClientOperationId) || request.ClientOperationId.Length > 200 || request.SetNumber < 1 || request.Repetitions < 1 || request.WeightKg < 0) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "Informe uma série válida.", 400);
            var exercise = await db.WorkoutSessionExercises.Include(x => x.WorkoutSession).SingleOrDefaultAsync(x => x.Id == exerciseId && x.WorkoutSessionId == sessionId && x.WorkoutSession.StudentId == studentId, ct); if (exercise is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404);
            if (exercise.WorkoutSession.Status != "InProgress") return ApiEndpointExtensions.ApiError("SESSION_COMPLETED", "Esta sessão já foi concluída.", 409);
            if (request.SetNumber > exercise.Sets) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "O número da série excede a prescrição do exercício.", 400);

            var operationId = request.ClientOperationId.Trim();
            var existingOperation = await db.SetPerformances.SingleOrDefaultAsync(x => x.WorkoutSessionExerciseId == exerciseId && x.ClientOperationId == operationId, ct);
            if (existingOperation is not null)
            {
                if (existingOperation.SetNumber != request.SetNumber || existingOperation.WeightKg != request.WeightKg || existingOperation.Repetitions != request.Repetitions)
                    return ApiEndpointExtensions.ApiError("IDEMPOTENCY_CONFLICT", "Esta operação já foi usada com outros dados.", 409);
                return Results.Ok(new { saved = true, exercise.CompletedSets });
            }

            var existingSet = await db.SetPerformances.SingleOrDefaultAsync(x => x.WorkoutSessionExerciseId == exerciseId && x.SetNumber == request.SetNumber, ct);
            if (existingSet is not null)
                return ApiEndpointExtensions.ApiError("SET_ALREADY_RECORDED", "Esta série já foi registrada.", 409);

            db.SetPerformances.Add(new PersonalUltra.Domain.SetPerformance { Id = Guid.NewGuid(), WorkoutSessionExerciseId = exerciseId, ClientOperationId = operationId, SetNumber = request.SetNumber, WeightKg = request.WeightKg, Repetitions = request.Repetitions, CompletedAt = clock.GetUtcNow() });
            exercise.CompletedSets = await db.SetPerformances.CountAsync(x => x.WorkoutSessionExerciseId == exerciseId, ct) + 1;
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
            var session = await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == studentId, ct); if (session is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404); if (session.Status == "Completed") return Results.Ok(new { session.Id, session.Status, session.CompletedAt }); session.Status = "Completed"; session.CompletedAt = clock.GetUtcNow(); await db.SaveChangesAsync(ct); return Results.Ok(new { session.Id, session.Status, session.CompletedAt });
        });
    }
    private static bool StudentId(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && user.FindFirstValue("subject") == "student";
    private static StudentWorkoutSessionResponse ToResponse(PersonalUltra.Domain.WorkoutSession session) => new(session.Id, session.StudentWorkoutId, session.StudentWorkout?.Name ?? "Treino", session.Status, session.Exercises.OrderBy(x => x.Sequence).Select(x => new StudentSessionExercise(x.Id, x.ExerciseId, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, x.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes, x.CompletedSets)).ToArray());
}
