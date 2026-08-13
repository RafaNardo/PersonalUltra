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
        var exercises = app.MapGroup("/api/v1/training/exercises").RequireAuthorization();

        exercises.MapGet("/", async (string? search, string? muscleGroup, PersonalUltraDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var normalizedSearch = search?.Trim();
            var normalizedMuscleGroup = muscleGroup?.Trim();
            if (normalizedSearch?.Length > 100 || normalizedMuscleGroup?.Length > 100)
                return context.ApiError("VALIDATION_ERROR", "Os filtros devem ter até 100 caracteres.", 400);

            var query = db.Exercises
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var searchTerm = normalizedSearch.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Slug.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(normalizedMuscleGroup))
            {
                var muscleGroupTerm = normalizedMuscleGroup.ToLower();
                query = query.Where(x => x.PrimaryMuscleGroup.ToLower() == muscleGroupTerm);
            }

            var result = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Slug)
                .ThenBy(x => x.Id)
                .Select(x => new TrainerExerciseCatalogItem(x.Id, x.Name, x.Slug, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, x.Instructions, x.IsActive))
                .ToListAsync(ct);

            return Results.Ok(result);
        });

        var templates = app.MapGroup("/api/v1/training/templates").RequireAuthorization();

        templates.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            var result = await db.WorkoutTemplates
                .AsNoTracking()
                .Where(x => x.TrainerId == trainerId)
                .OrderBy(x => x.Name)
                .Select(x => new WorkoutTemplateSummary(x.Id, x.Name, x.Notes, x.Exercises.Count, x.UpdatedAt))
                .ToListAsync(ct);
            return Results.Ok(result);
        });

        templates.MapGet("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var item = await db.WorkoutTemplates
                .AsNoTracking()
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct);
            return item is null
                ? context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404)
                : Results.Ok(ToResponse(item));
        });

        templates.MapPost("/", async (WorkoutTemplateRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var validation = Validate(request);
            if (validation is not null)
                return context.ApiError("VALIDATION_ERROR", validation, 400);

            var catalog = await ResolveActiveExercises(request.Exercises, db, ct);
            if (catalog is null)
                return context.ApiError("EXERCISE_NOT_FOUND", "Um ou mais exercícios não existem ou estão inativos.", 400);

            var now = clock.GetUtcNow();
            var template = new WorkoutTemplate
            {
                Id = Guid.NewGuid(),
                TrainerId = TrainerId(user),
                Name = request.Name.Trim(),
                Notes = request.Notes?.Trim() ?? "",
                CreatedAt = now,
                UpdatedAt = now,
            };
            template.Exercises.AddRange(request.Exercises.Select(x => ToEntity(template.Id, x, catalog[x.ExerciseId])));

            db.WorkoutTemplates.Add(template);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/training/templates/{template.Id}", ToResponse(template));
        });

        templates.MapPut("/{id:guid}", async (Guid id, WorkoutTemplateRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var validation = Validate(request);
            if (validation is not null)
                return context.ApiError("VALIDATION_ERROR", validation, 400);

            var template = await db.WorkoutTemplates
                .Include(x => x.Exercises)
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct);
            if (template is null)
                return context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404);

            var catalog = await ResolveActiveExercises(request.Exercises, db, ct);
            if (catalog is null)
                return context.ApiError("EXERCISE_NOT_FOUND", "Um ou mais exercícios não existem ou estão inativos.", 400);

            template.Name = request.Name.Trim();
            template.Notes = request.Notes?.Trim() ?? "";
            template.UpdatedAt = clock.GetUtcNow();
            db.WorkoutTemplateExercises.RemoveRange(template.Exercises);
            template.Exercises.Clear();
            template.Exercises.AddRange(request.Exercises.Select(x => ToEntity(id, x, catalog[x.ExerciseId])));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(template));
        });

        templates.MapPost("/{id:guid}/duplicate", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var source = await db.WorkoutTemplates
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct);
            if (source is null)
                return context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404);

            var now = clock.GetUtcNow();
            var copy = new WorkoutTemplate
            {
                Id = Guid.NewGuid(),
                TrainerId = source.TrainerId,
                Name = $"{source.Name} (cópia)",
                Notes = source.Notes,
                CreatedAt = now,
                UpdatedAt = now,
            };
            copy.Exercises.AddRange(source.Exercises.Select(x => new WorkoutTemplateExercise
            {
                Id = Guid.NewGuid(),
                WorkoutTemplateId = copy.Id,
                ExerciseId = x.ExerciseId,
                Exercise = x.Exercise,
                Sequence = x.Sequence,
                Sets = x.Sets,
                RepetitionsMin = x.RepetitionsMin,
                RepetitionsMax = x.RepetitionsMax,
                RestSeconds = x.RestSeconds,
                Notes = x.Notes,
            }));

            db.WorkoutTemplates.Add(copy);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(copy));
        });

        templates.MapPost("/{id:guid}/apply", async (Guid id, ApplyWorkoutRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            var source = await db.WorkoutTemplates
                .AsNoTracking()
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == trainerId, ct);
            if (source is null)
                return context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404);
            if (!await db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == request.StudentId && x.EndedAt == null, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var applied = new StudentWorkout
            {
                Id = Guid.NewGuid(),
                TrainerId = trainerId,
                StudentId = request.StudentId,
                Name = source.Name,
                Notes = source.Notes,
                RecommendedDay = Math.Clamp(request.RecommendedDay, 1, 7),
                IsRecommended = request.IsRecommended,
                CreatedAt = clock.GetUtcNow(),
            };
            applied.Exercises.AddRange(source.Exercises.Select(x => StudentWorkoutExercise.FromTemplate(applied.Id, x)));

            db.StudentWorkouts.Add(applied);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new AppliedWorkoutResponse(applied.Id, applied.StudentId, applied.Name, applied.RecommendedDay, applied.IsRecommended, applied.Exercises.Count));
        });

        app.MapGet("/api/v1/students/{studentId:guid}/training-history", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            var sessions = await db.WorkoutSessions
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .OrderByDescending(x => x.StartedAt)
                .Take(30)
                .Select(x => new TrainingHistoryItem(x.Id, x.StudentWorkout.Name, x.Status, x.StartedAt, x.CompletedAt, x.Exercises.Sum(e => e.CompletedSets)))
                .ToListAsync(ct);
            return Results.Ok(new StudentTrainingHistoryResponse(sessions));
        });
    }

    private static Guid TrainerId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static WorkoutTemplateExercise ToEntity(Guid templateId, WorkoutTemplateExerciseInput input, Exercise exercise) => new()
    {
        Id = Guid.NewGuid(),
        WorkoutTemplateId = templateId,
        ExerciseId = input.ExerciseId,
        Exercise = exercise,
        Sequence = input.Sequence,
        Sets = input.Sets,
        RepetitionsMin = input.RepetitionsMin,
        RepetitionsMax = input.RepetitionsMax,
        RestSeconds = input.RestSeconds,
        Notes = input.Notes?.Trim() ?? "",
    };

    private static WorkoutTemplateResponse ToResponse(WorkoutTemplate item) => new(
        item.Id,
        item.Name,
        item.Notes,
        item.Exercises
            .OrderBy(x => x.Sequence)
            .Select(x => new WorkoutTemplateExerciseResponse(x.ExerciseId, x.Exercise.Name, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes))
            .ToArray());

    private static async Task<Dictionary<Guid, Exercise>?> ResolveActiveExercises(
        IReadOnlyList<WorkoutTemplateExerciseInput> requested,
        PersonalUltraDbContext db,
        CancellationToken cancellationToken)
    {
        var ids = requested.Select(x => x.ExerciseId).Distinct().ToArray();
        var exercises = await db.Exercises
            .Where(x => ids.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        return exercises.Count == ids.Length ? exercises : null;
    }

    private static string? Validate(WorkoutTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return "Informe um nome de treino com até 200 caracteres.";
        if (request.Notes?.Length > 2000)
            return "As observações do treino devem ter até 2000 caracteres.";
        if (request.Exercises is null || request.Exercises.Count is < 1 or > 30)
            return "Adicione entre 1 e 30 exercícios.";
        if (request.Exercises.Select(x => x.Sequence).Distinct().Count() != request.Exercises.Count)
            return "Cada exercício deve ter uma posição única.";
        if (request.Exercises.Any(x =>
                x.ExerciseId == Guid.Empty ||
                x.Sequence is < 1 or > 30 ||
                x.Sets is < 1 or > 20 ||
                x.RepetitionsMin is < 1 or > 100 ||
                x.RepetitionsMax is < 1 or > 100 ||
                x.RepetitionsMin > x.RepetitionsMax ||
                x.RestSeconds is < 0 or > 900 ||
                x.Notes?.Length > 1000))
            return "Revise exercício, ordem, séries, faixa de repetições, descanso e observações.";
        return null;
    }
}
