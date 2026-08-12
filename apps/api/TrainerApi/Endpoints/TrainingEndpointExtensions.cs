using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class TrainingEndpointExtensions
{
    public static void MapTrainingApi(this WebApplication app)
    {
        var templates = app.MapGroup("/api/v1/training/templates").RequireAuthorization();
        templates.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            var result = await db.WorkoutTemplates.AsNoTracking().Where(x => x.TrainerId == trainerId).OrderBy(x => x.Name).Select(x => new WorkoutTemplateSummary(x.Id, x.Name, x.Notes, x.Exercises.Count, x.UpdatedAt)).ToListAsync(ct);
            return Results.Ok(result);
        });
        templates.MapGet("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var item = await db.WorkoutTemplates.AsNoTracking().Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct);
            return item is null ? context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404) : Results.Ok(new WorkoutTemplateResponse(item.Id, item.Name, item.Notes, item.Exercises.OrderBy(x => x.Sequence).Select(x => new WorkoutTemplateExerciseInput(x.Name, x.Sequence, x.Sets, x.Repetitions, x.RestSeconds, x.Notes)).ToArray()));
        });
        templates.MapPost("/", async (WorkoutTemplateRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var validation = Validate(request); if (validation is not null) return context.ApiError("VALIDATION_ERROR", validation, 400);
            var now = clock.GetUtcNow(); var template = new WorkoutTemplate { Id = Guid.NewGuid(), TrainerId = TrainerId(user), Name = request.Name.Trim(), Notes = request.Notes?.Trim() ?? "", CreatedAt = now, UpdatedAt = now };
            template.Exercises.AddRange(request.Exercises.OrderBy(x => x.Sequence).Select((x, i) => new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = template.Id, Name = x.Name.Trim(), Sequence = i + 1, Sets = x.Sets, Repetitions = x.Repetitions, RestSeconds = x.RestSeconds, Notes = x.Notes?.Trim() ?? "" }));
            db.WorkoutTemplates.Add(template); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/training/templates/{template.Id}", ToResponse(template));
        });
        templates.MapPut("/{id:guid}", async (Guid id, WorkoutTemplateRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var validation = Validate(request); if (validation is not null) return context.ApiError("VALIDATION_ERROR", validation, 400);
            var template = await db.WorkoutTemplates.Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct); if (template is null) return context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404);
            template.Name = request.Name.Trim(); template.Notes = request.Notes?.Trim() ?? ""; template.UpdatedAt = clock.GetUtcNow(); db.WorkoutTemplateExercises.RemoveRange(template.Exercises); template.Exercises.Clear(); template.Exercises.AddRange(request.Exercises.OrderBy(x => x.Sequence).Select((x, i) => new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = id, Name = x.Name.Trim(), Sequence = i + 1, Sets = x.Sets, Repetitions = x.Repetitions, RestSeconds = x.RestSeconds, Notes = x.Notes?.Trim() ?? "" })); await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(template));
        });
        templates.MapPost("/{id:guid}/duplicate", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var source = await db.WorkoutTemplates.AsNoTracking().Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct); if (source is null) return context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404);
            var now = clock.GetUtcNow(); var copy = new WorkoutTemplate { Id = Guid.NewGuid(), TrainerId = source.TrainerId, Name = $"{source.Name} (cópia)", Notes = source.Notes, CreatedAt = now, UpdatedAt = now }; copy.Exercises.AddRange(source.Exercises.Select(x => new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = copy.Id, Name = x.Name, Sequence = x.Sequence, Sets = x.Sets, Repetitions = x.Repetitions, RestSeconds = x.RestSeconds, Notes = x.Notes })); db.WorkoutTemplates.Add(copy); await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(copy));
        });
        templates.MapPost("/{id:guid}/apply", async (Guid id, ApplyWorkoutRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user); var source = await db.WorkoutTemplates.AsNoTracking().Include(x => x.Exercises).SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == trainerId, ct); if (source is null) return context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404);
            if (!await db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == request.StudentId && x.EndedAt == null, ct)) return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            var applied = new StudentWorkout { Id = Guid.NewGuid(), TrainerId = trainerId, StudentId = request.StudentId, Name = source.Name, Notes = source.Notes, RecommendedDay = Math.Clamp(request.RecommendedDay, 1, 7), IsRecommended = request.IsRecommended, CreatedAt = clock.GetUtcNow() }; applied.Exercises.AddRange(source.Exercises.Select(x => new StudentWorkoutExercise { Id = Guid.NewGuid(), StudentWorkoutId = applied.Id, Name = x.Name, Sequence = x.Sequence, Sets = x.Sets, Repetitions = x.Repetitions, RestSeconds = x.RestSeconds, Notes = x.Notes })); db.StudentWorkouts.Add(applied); await db.SaveChangesAsync(ct); return Results.Ok(new AppliedWorkoutResponse(applied.Id, applied.StudentId, applied.Name, applied.RecommendedDay, applied.IsRecommended, applied.Exercises.Count));
        });
        app.MapGet("/api/v1/students/{studentId:guid}/training-history", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user); if (!await db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, ct)) return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            var sessions = await db.WorkoutSessions.AsNoTracking().Where(x => x.StudentId == studentId).OrderByDescending(x => x.StartedAt).Take(30).Select(x => new TrainingHistoryItem(x.Id, x.StudentWorkout.Name, x.Status, x.StartedAt, x.CompletedAt, x.Exercises.Sum(e => e.CompletedSets))).ToListAsync(ct); return Results.Ok(new StudentTrainingHistoryResponse(sessions));
        });
    }
    private static Guid TrainerId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static WorkoutTemplateResponse ToResponse(WorkoutTemplate item) => new(item.Id, item.Name, item.Notes, item.Exercises.OrderBy(x => x.Sequence).Select(x => new WorkoutTemplateExerciseInput(x.Name, x.Sequence, x.Sets, x.Repetitions, x.RestSeconds, x.Notes)).ToArray());
    private static string? Validate(WorkoutTemplateRequest request) => string.IsNullOrWhiteSpace(request.Name) ? "Informe o nome do treino." : request.Exercises.Count is < 1 or > 30 ? "Adicione entre 1 e 30 exercícios." : request.Exercises.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Sets is < 1 or > 20 || x.Repetitions is < 1 or > 100 || x.RestSeconds is < 0 or > 900) ? "Revise exercícios, séries, repetições e descanso." : null;
}
