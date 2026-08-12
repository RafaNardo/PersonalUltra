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
            var workouts = await db.StudentWorkouts.AsNoTracking().Where(x => x.StudentId == studentId).OrderBy(x => x.RecommendedDay).ThenBy(x => x.Name).Select(x => new StudentWorkoutSummary(x.Id, x.Name, x.Notes, x.RecommendedDay, x.IsRecommended, x.Exercises.Count)).ToListAsync(ct);
            var history = await db.WorkoutSessions.AsNoTracking().Where(x => x.StudentId == studentId).OrderByDescending(x => x.StartedAt).Take(20).Select(x => new StudentTrainingHistoryItem(x.Id, x.StudentWorkoutId, x.StudentWorkout.Name, x.Status, x.StartedAt, x.CompletedAt, x.Exercises.Sum(e => e.CompletedSets))).ToListAsync(ct);
            return Results.Ok(new StudentTrainingResponse(workouts.FirstOrDefault(x => x.IsRecommended), workouts.Where(x => !x.IsRecommended).ToArray(), history));
        });
        api.MapPost("/{workoutId:guid}/start", async (Guid workoutId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var workout = await db.StudentWorkouts.AsNoTracking().Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == workoutId && x.StudentId == studentId, ct); if (workout is null) return ApiEndpointExtensions.ApiError("WORKOUT_NOT_FOUND", "Este treino não está disponível.", 404);
            var existing = await db.WorkoutSessions.Include(x => x.StudentWorkout).Include(x => x.Exercises).SingleOrDefaultAsync(x => x.StudentId == studentId && x.StudentWorkoutId == workoutId && x.Status == "InProgress", ct);
            if (existing is not null) return Results.Ok(ToResponse(existing));
            var session = new PersonalUltra.Domain.WorkoutSession { Id = Guid.NewGuid(), StudentId = studentId, StudentWorkoutId = workoutId, StudentWorkout = workout, StartedAt = clock.GetUtcNow(), Status = "InProgress" }; session.Exercises.AddRange(workout.Exercises.OrderBy(x => x.Sequence).Select(x => new PersonalUltra.Domain.WorkoutSessionExercise { Id = Guid.NewGuid(), WorkoutSessionId = session.Id, Name = x.Name, Sequence = x.Sequence, Sets = x.Sets, Repetitions = x.Repetitions })); db.WorkoutSessions.Add(session); await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(session));
        });
        api.MapPost("/sessions/{sessionId:guid}/exercises/{exerciseId:guid}/sets", async (Guid sessionId, Guid exerciseId, CompleteSetRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            if (request.SetNumber < 1 || request.Repetitions < 1 || request.WeightKg < 0) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "Informe uma série válida.", 400);
            var exercise = await db.WorkoutSessionExercises.Include(x => x.WorkoutSession).SingleOrDefaultAsync(x => x.Id == exerciseId && x.WorkoutSessionId == sessionId && x.WorkoutSession.StudentId == studentId, ct); if (exercise is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404);
            if (await db.SetPerformances.AnyAsync(x => x.WorkoutSessionExerciseId == exerciseId && x.SetNumber == request.SetNumber, ct)) return Results.Ok(new { saved = true });
            db.SetPerformances.Add(new PersonalUltra.Domain.SetPerformance { Id = Guid.NewGuid(), WorkoutSessionExerciseId = exerciseId, SetNumber = request.SetNumber, WeightKg = request.WeightKg, Repetitions = request.Repetitions, CompletedAt = clock.GetUtcNow() }); exercise.CompletedSets = await db.SetPerformances.CountAsync(x => x.WorkoutSessionExerciseId == exerciseId, ct) + 1; await db.SaveChangesAsync(ct); return Results.Ok(new { saved = true, exercise.CompletedSets });
        });
        api.MapPost("/sessions/{sessionId:guid}/complete", async (Guid sessionId, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken ct) =>
        {
            if (!StudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var session = await db.WorkoutSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.StudentId == studentId, ct); if (session is null) return ApiEndpointExtensions.ApiError("SESSION_NOT_FOUND", "Sessão de treino não encontrada.", 404); session.Status = "Completed"; session.CompletedAt = clock.GetUtcNow(); await db.SaveChangesAsync(ct); return Results.Ok(new { session.Id, session.Status, session.CompletedAt });
        });
    }
    private static bool StudentId(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && user.FindFirstValue("subject") == "student";
    private static StudentWorkoutSessionResponse ToResponse(PersonalUltra.Domain.WorkoutSession session) => new(session.Id, session.StudentWorkoutId, session.StudentWorkout?.Name ?? "Treino", session.Status, session.Exercises.OrderBy(x => x.Sequence).Select(x => new StudentSessionExercise(x.Id, x.Name, x.Sequence, x.Sets, x.Repetitions, x.CompletedSets)).ToArray());
}
