using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Application.Training;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class TrainingEndpointExtensions
{
    public static void MapTrainingApi(this WebApplication app)
    {
        var exercises = app.MapGroup("/api/v1/training/exercises").RequireAuthorization();

        exercises.MapGet("/", async (string? search, string? muscleGroup, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, HttpContext context, CancellationToken ct) =>
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
                .Select(x => new TrainerExerciseCatalogItem(x.Id, x.Name, x.Slug, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, null, x.Instructions, x.IsActive, x.DefaultTrackingMode, x.DefaultDurationSeconds))
                .ToListAsync(ct);

            return Results.Ok(result.Select(item => item with { ImageUrl = mediaResolver.ResolveUrl(item.ImageRef) }).ToArray());
        });

        var templates = app.MapGroup("/api/v1/training/templates").RequireAuthorization();

        templates.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            var result = await db.WorkoutTemplates
                .AsNoTracking()
                .Where(x => x.TrainerId == trainerId)
                .OrderBy(x => x.Name)
                .Select(x => new WorkoutTemplateSummary(x.Id, x.Name, x.Notes, x.Exercises.Count, x.UpdatedAt, x.Exercises.Select(exercise => exercise.Exercise.PrimaryMuscleGroup).Distinct().OrderBy(group => group).ToArray()))
                .ToListAsync(ct);
            return Results.Ok(result);
        });

        templates.MapGet("/{id:guid}", async (Guid id, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var item = await db.WorkoutTemplates
                .AsNoTracking()
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct);
            return item is null
                ? context.ApiError("TEMPLATE_NOT_FOUND", "Treino não encontrado.", 404)
                : Results.Ok(ToResponse(item, mediaResolver));
        });

        templates.MapPost("/", async (WorkoutTemplateRequest request, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
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
            return Results.Created($"/api/v1/training/templates/{template.Id}", ToResponse(template, mediaResolver));
        });

        templates.MapPut("/{id:guid}", async (Guid id, WorkoutTemplateRequest request, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
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
            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            db.WorkoutTemplateExercises.RemoveRange(template.Exercises);
            await db.SaveChangesAsync(ct);
            var replacements = request.Exercises.Select(x => ToEntity(id, x, catalog[x.ExerciseId])).ToArray();
            db.WorkoutTemplateExercises.AddRange(replacements);
            await db.SaveChangesAsync(ct);
            if (transaction is not null)
                await transaction.CommitAsync(ct);

            var updated = await db.WorkoutTemplates.AsNoTracking().Include(x => x.Exercises).ThenInclude(x => x.Exercise).SingleAsync(x => x.Id == id, ct);
            return Results.Ok(ToResponse(updated, mediaResolver));
        });

        templates.MapDelete("/{id:guid}", async (Guid id, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var template = await db.WorkoutTemplates
                .Include(x => x.Exercises)
                .SingleOrDefaultAsync(x => x.Id == id && x.TrainerId == TrainerId(user), ct);
            if (template is null)
                return context.ApiError("TEMPLATE_NOT_FOUND", "Modelo de treino não encontrado.", 404);

            db.WorkoutTemplateExercises.RemoveRange(template.Exercises);
            db.WorkoutTemplates.Remove(template);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        templates.MapPost("/{id:guid}/duplicate", async (Guid id, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
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
                TrackingMode = x.TrackingMode,
                TargetDurationSeconds = x.TargetDurationSeconds,
                RestSeconds = x.RestSeconds,
                Notes = x.Notes,
            }));

            db.WorkoutTemplates.Add(copy);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(copy, mediaResolver));
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
            var suggestedOrder = await NextSuggestedOrder(db, request.StudentId, ct);

            var applied = new StudentWorkout
            {
                Id = Guid.NewGuid(),
                TrainerId = trainerId,
                StudentId = request.StudentId,
                Name = source.Name,
                Notes = source.Notes,
                SuggestedOrder = suggestedOrder,
                CreatedAt = clock.GetUtcNow(),
            };
            applied.Exercises.AddRange(source.Exercises.Select(x => StudentWorkoutExercise.FromTemplate(applied.Id, x)));

            db.StudentWorkouts.Add(applied);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new AppliedWorkoutResponse(applied.Id, applied.StudentId, applied.Name, applied.SuggestedOrder, applied.Exercises.Count));
        });

        app.MapGet("/api/v1/students/{studentId:guid}/workouts", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await OwnsStudent(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var workouts = await db.StudentWorkouts
                .AsNoTracking()
                .Where(x => x.TrainerId == trainerId && x.StudentId == studentId && x.IsActive)
                .OrderBy(x => x.SuggestedOrder)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => new TrainerStudentWorkoutSummary(x.Id, x.Name, x.Notes, x.SuggestedOrder, x.Exercises.Count, x.CreatedAt))
                .ToListAsync(ct);
            return Results.Ok(new TrainerStudentWorkoutListResponse(workouts));
        }).RequireAuthorization();

        app.MapPost("/api/v1/students/{studentId:guid}/workouts", async (Guid studentId, TrainerStudentWorkoutCreateRequest request, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await OwnsStudent(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var name = request.Name?.Trim() ?? "";
            var notes = request.Notes?.Trim() ?? "";
            if (name.Length is < 1 or > 200)
                return context.ApiError("VALIDATION_ERROR", "Informe um nome de treino com até 200 caracteres.", 400);
            if (notes.Length > 2000)
                return context.ApiError("VALIDATION_ERROR", "As observações devem ter até 2000 caracteres.", 400);
            var suggestedOrder = await NextSuggestedOrder(db, studentId, ct);
            var workout = new StudentWorkout
            {
                Id = Guid.NewGuid(),
                TrainerId = trainerId,
                StudentId = studentId,
                Name = name,
                Notes = notes,
                SuggestedOrder = suggestedOrder,
                CreatedAt = clock.GetUtcNow(),
            };
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToStudentWorkoutDetail(workout, mediaResolver));
        }).RequireAuthorization();

        app.MapGet("/api/v1/students/{studentId:guid}/workouts/{workoutId:guid}", async (Guid studentId, Guid workoutId, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await OwnsStudent(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var workout = await db.StudentWorkouts
                .AsNoTracking()
                .Where(x => x.Id == workoutId && x.TrainerId == trainerId && x.StudentId == studentId && x.IsActive)
                .Select(x => new TrainerStudentWorkoutDetail(
                    x.Id,
                    x.StudentId,
                    x.Name,
                    x.Notes,
                    x.SuggestedOrder,
                    x.CreatedAt,
                    x.Exercises
                        .OrderBy(exercise => exercise.Sequence)
                        .Select(exercise => new TrainerStudentWorkoutExercise(exercise.Id, exercise.ExerciseId, exercise.Name, exercise.PrimaryMuscleGroup, exercise.Equipment, exercise.ImageRef, null, exercise.Instructions, exercise.Sequence, exercise.Sets, exercise.RepetitionsMin, exercise.RepetitionsMax, exercise.RestSeconds, exercise.Notes, exercise.TrackingMode, exercise.TargetDurationSeconds))
                        .ToArray()))
                .SingleOrDefaultAsync(ct);
            return workout is null
                ? context.ApiError("WORKOUT_NOT_FOUND", "Treino não encontrado para este aluno.", 404)
                : Results.Ok(workout with { Exercises = workout.Exercises.Select(exercise => exercise with { ImageUrl = mediaResolver.ResolveUrl(exercise.ImageRef) }).ToArray() });
        }).RequireAuthorization();

        app.MapPut("/api/v1/students/{studentId:guid}/workouts/order", async (Guid studentId, ReorderTrainerStudentWorkoutsRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await OwnsStudent(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var workoutIds = request.WorkoutIds?.ToArray() ?? [];
            var workouts = await db.StudentWorkouts
                .Include(x => x.Exercises)
                .Where(x => x.TrainerId == trainerId && x.StudentId == studentId && x.IsActive)
                .ToListAsync(ct);
            var expectedIds = workouts.Select(x => x.Id).ToHashSet();
            if (workoutIds.Length != workouts.Count || workoutIds.Distinct().Count() != workoutIds.Length || !workoutIds.All(expectedIds.Contains))
                return context.ApiError("VALIDATION_ERROR", "A ordem precisa incluir cada treino disponível uma única vez.", 400);

            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            foreach (var (workout, index) in workouts.Select((workout, index) => (workout, index)))
                workout.SuggestedOrder = -(index + 1);
            await db.SaveChangesAsync(ct);
            foreach (var (workoutId, index) in workoutIds.Select((id, index) => (id, index)))
                workouts.Single(x => x.Id == workoutId).SuggestedOrder = index + 1;
            await db.SaveChangesAsync(ct);
            if (transaction is not null)
                await transaction.CommitAsync(ct);

            return Results.Ok(new TrainerStudentWorkoutListResponse(workouts
                .OrderBy(x => x.SuggestedOrder)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => new TrainerStudentWorkoutSummary(x.Id, x.Name, x.Notes, x.SuggestedOrder, x.Exercises.Count, x.CreatedAt))
                .ToArray()));
        }).RequireAuthorization();

        app.MapDelete("/api/v1/students/{studentId:guid}/workouts/{workoutId:guid}", async (Guid studentId, Guid workoutId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await OwnsStudent(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var workout = await db.StudentWorkouts
                .Include(x => x.Exercises)
                .SingleOrDefaultAsync(x => x.Id == workoutId && x.TrainerId == trainerId && x.StudentId == studentId && x.IsActive, ct);
            if (workout is null)
                return context.ApiError("WORKOUT_NOT_FOUND", "Treino não encontrado para este aluno.", 404);

            if (await db.WorkoutSessions.AnyAsync(x => x.StudentWorkoutId == workoutId && x.StudentId == studentId && x.Status == "InProgress", ct))
                return context.ApiError("WORKOUT_SESSION_IN_PROGRESS", "Conclua ou retome a sessão em andamento antes de remover este treino.", 409);

            workout.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPut("/api/v1/students/{studentId:guid}/workouts/{workoutId:guid}", async (Guid studentId, Guid workoutId, TrainerStudentWorkoutUpdateRequest request, PersonalUltraDbContext db, IExerciseMediaResolver mediaResolver, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await OwnsStudent(db, trainerId, studentId, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);

            var validation = ValidateStudentWorkout(request);
            if (validation is not null)
                return context.ApiError("VALIDATION_ERROR", validation, 400);

            var workout = await db.StudentWorkouts
                .Include(x => x.Exercises)
                .SingleOrDefaultAsync(x => x.Id == workoutId && x.TrainerId == trainerId && x.StudentId == studentId && x.IsActive, ct);
            if (workout is null)
                return context.ApiError("WORKOUT_NOT_FOUND", "Treino não encontrado para este aluno.", 404);

            var existingById = workout.Exercises.ToDictionary(x => x.Id);
            var requestedExisting = request.Exercises.Where(x => x.Id.HasValue).ToArray();
            if (requestedExisting.Any(x => !existingById.TryGetValue(x.Id!.Value, out var existing) || x.ExerciseId != existing.ExerciseId))
                return context.ApiError("VALIDATION_ERROR", "A lista de exercícios está desatualizada. Recarregue o treino.", 400);

            var additions = request.Exercises.Where(x => !x.Id.HasValue).ToArray();
            var additionIds = additions.Select(x => x.ExerciseId!.Value).Distinct().ToArray();
            var activeCatalog = await db.Exercises
                .Where(x => additionIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, ct);
            if (activeCatalog.Count != additionIds.Length)
                return context.ApiError("EXERCISE_NOT_FOUND", "Um ou mais exercícios não existem ou estão inativos.", 400);

            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;

            try
            {
                if (request.Name is not null)
                    workout.Name = request.Name.Trim();

                if (transaction is not null)
                {
                    // Free the unique (workout, sequence) slots before arbitrary reorder.
                    foreach (var (exercise, index) in workout.Exercises.Select((exercise, index) => (exercise, index)))
                        exercise.Sequence = -(index + 1);
                    await db.SaveChangesAsync(ct);
                }

                var retainedIds = requestedExisting.Select(x => x.Id!.Value).ToHashSet();
                var removed = workout.Exercises.Where(x => !retainedIds.Contains(x.Id)).ToArray();
                db.StudentWorkoutExercises.RemoveRange(removed);

                foreach (var input in request.Exercises.OrderBy(x => x.Sequence))
                {
                    if (input.Id.HasValue)
                    {
                        var existing = existingById[input.Id.Value];
                        existing.Sequence = input.Sequence;
                        existing.Sets = input.Sets;
                        existing.RepetitionsMin = input.RepetitionsMin;
                        existing.RepetitionsMax = input.RepetitionsMax;
                        existing.TrackingMode = input.TrackingMode ?? existing.TrackingMode;
                        existing.TargetDurationSeconds = existing.TrackingMode == ExerciseTrackingModes.Duration ? input.TargetDurationSeconds ?? existing.TargetDurationSeconds : null;
                        existing.RestSeconds = input.RestSeconds;
                        existing.Notes = input.Notes?.Trim() ?? "";
                        continue;
                    }

                    var added = StudentWorkoutExercise.FromCatalog(
                        workout.Id,
                        activeCatalog[input.ExerciseId!.Value],
                        input.Sequence,
                        input.Sets,
                        input.RepetitionsMin,
                        input.RepetitionsMax,
                        input.RestSeconds,
                        input.Notes?.Trim() ?? "",
                        input.TrackingMode,
                        input.TargetDurationSeconds);
                    db.StudentWorkoutExercises.Add(added);
                    workout.Exercises.Add(added);
                }

                await db.SaveChangesAsync(ct);
                if (transaction is not null)
                    await transaction.CommitAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(ct);
                return context.ApiError("WORKOUT_CONFLICT", "O treino foi alterado em outro lugar. Recarregue antes de publicar novamente.", 409);
            }

            var updated = await db.StudentWorkouts
                .AsNoTracking()
                .Include(x => x.Exercises)
                .SingleAsync(x => x.Id == workout.Id, ct);
            return Results.Ok(ToStudentWorkoutDetail(updated, mediaResolver));
        }).RequireAuthorization();

        app.MapGet("/api/v1/students/{studentId:guid}/training-history", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, ct))
                return context.ApiError("STUDENT_NOT_FOUND", "Aluno não encontrado.", 404);
            var sessions = await db.WorkoutSessions
                .AsNoTracking()
                .Include(x => x.StudentWorkout)
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Performances)
                .Where(x => x.StudentId == studentId)
                .OrderByDescending(x => x.StartedAt)
                .Take(30)
                .ToListAsync(ct);
            return Results.Ok(new StudentTrainingHistoryResponse(sessions.Select(x => new TrainingHistoryItem(
                x.Id,
                x.StudentWorkout.Name,
                x.Status,
                x.StartedAt,
                x.CompletedAt,
                x.Exercises.Sum(e => e.CompletedSets),
                x.Exercises.OrderBy(e => e.Sequence).Select(e => new TrainingHistoryExerciseItem(
                    e.Name,
                    e.Sequence,
                    e.TrackingMode,
                    e.ConfirmedCompletedAt.HasValue,
                    e.Performances.OrderBy(p => p.SetNumber).Select(p => new TrainingHistorySetItem(p.SetNumber, p.WeightKg, p.Repetitions, p.DurationSeconds, p.CompletedAt)).ToArray())).ToArray())).ToArray()));
        });
    }

    private static Guid TrainerId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static Task<bool> OwnsStudent(PersonalUltraDbContext db, Guid trainerId, Guid studentId, CancellationToken cancellationToken) =>
        db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, cancellationToken);

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
        TrackingMode = input.TrackingMode ?? exercise.DefaultTrackingMode,
        TargetDurationSeconds = (input.TrackingMode ?? exercise.DefaultTrackingMode) == ExerciseTrackingModes.Duration ? input.TargetDurationSeconds ?? exercise.DefaultDurationSeconds : null,
        RestSeconds = input.RestSeconds,
        Notes = input.Notes?.Trim() ?? "",
    };

    private static WorkoutTemplateResponse ToResponse(WorkoutTemplate item, IExerciseMediaResolver mediaResolver) => new(
        item.Id,
        item.Name,
        item.Notes,
        item.Exercises
            .OrderBy(x => x.Sequence)
            .Select(x => new WorkoutTemplateExerciseResponse(x.ExerciseId, x.Exercise.Name, x.Exercise.PrimaryMuscleGroup, x.Exercise.Equipment, x.Exercise.ImageRef, mediaResolver.ResolveUrl(x.Exercise.ImageRef), x.Exercise.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes, x.TrackingMode, x.TargetDurationSeconds))
            .ToArray());

    private static TrainerStudentWorkoutDetail ToStudentWorkoutDetail(StudentWorkout workout, IExerciseMediaResolver mediaResolver) => new(
        workout.Id,
        workout.StudentId,
        workout.Name,
        workout.Notes,
        workout.SuggestedOrder,
        workout.CreatedAt,
        workout.Exercises
            .OrderBy(x => x.Sequence)
            .Select(x => new TrainerStudentWorkoutExercise(x.Id, x.ExerciseId, x.Name, x.PrimaryMuscleGroup, x.Equipment, x.ImageRef, mediaResolver.ResolveUrl(x.ImageRef), x.Instructions, x.Sequence, x.Sets, x.RepetitionsMin, x.RepetitionsMax, x.RestSeconds, x.Notes, x.TrackingMode, x.TargetDurationSeconds))
            .ToArray());

    private static async Task<int> NextSuggestedOrder(PersonalUltraDbContext db, Guid studentId, CancellationToken cancellationToken)
    {
        var currentMaximum = await db.StudentWorkouts
            .Where(x => x.StudentId == studentId && x.IsActive)
            .Select(x => (int?)x.SuggestedOrder)
            .MaxAsync(cancellationToken);
        return checked((currentMaximum ?? 0) + 1);
    }

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
                (x.TrackingMode is not null && !ExerciseTrackingModes.IsValid(x.TrackingMode)) ||
                (x.TrackingMode == ExerciseTrackingModes.Duration && x.TargetDurationSeconds is null or < 5 or > 86400) ||
                x.RestSeconds is < 0 or > 900 ||
                x.Notes?.Length > 1000))
            return "Revise exercício, ordem, modo de acompanhamento, quantidade, alvo, descanso e observações.";
        return null;
    }

    private static string? ValidateStudentWorkout(TrainerStudentWorkoutUpdateRequest request)
    {
        if (request.Name is not null && (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200))
            return "O nome do treino deve ter entre 1 e 200 caracteres.";
        if (request.Exercises is null || request.Exercises.Count > 30)
            return "O treino deve ter no máximo 30 exercícios.";
        if (!request.Exercises.Select(x => x.Sequence).Order().SequenceEqual(Enumerable.Range(1, request.Exercises.Count)))
            return "As posições dos exercícios devem formar uma sequência contínua.";
        if (request.Exercises.Where(x => x.Id.HasValue).Select(x => x.Id).Distinct().Count() != request.Exercises.Count(x => x.Id.HasValue))
            return "Cada item existente deve aparecer apenas uma vez.";
        if (request.Exercises.Any(x =>
                x.Id == Guid.Empty ||
                x.ExerciseId == Guid.Empty ||
                (!x.Id.HasValue && !x.ExerciseId.HasValue) ||
                x.Sets is < 1 or > 20 ||
                x.RepetitionsMin is < 1 or > 100 ||
                x.RepetitionsMax is < 1 or > 100 ||
                x.RepetitionsMin > x.RepetitionsMax ||
                (x.TrackingMode is not null && !ExerciseTrackingModes.IsValid(x.TrackingMode)) ||
                (x.TrackingMode == ExerciseTrackingModes.Duration && x.TargetDurationSeconds is null or < 5 or > 86400) ||
                x.RestSeconds is < 0 or > 900 ||
                x.Notes?.Length > 1000))
            return "Revise exercício, ordem, modo de acompanhamento, quantidade, alvo, descanso e observações.";
        return null;
    }
}
