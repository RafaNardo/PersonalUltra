using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class TrainerTrainingEndpointTests : IClassFixture<TrainerApiFactory>
{
    private readonly HttpClient client;
    private readonly TrainerApiFactory factory;

    public TrainerTrainingEndpointTests(TrainerApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
    }

    [Fact]
    public async Task Trainer_can_list_only_active_catalog_exercises_with_metadata()
    {
        var inactive = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Exercício arquivado",
            Slug = $"arquivado-{Guid.NewGuid():N}",
            PrimaryMuscleGroup = "Peito",
            ImageRef = "assets/training/arquivado.png",
            IsActive = false,
        };
        using (var seedScope = factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            seedDb.Exercises.Add(inactive);
            await seedDb.SaveChangesAsync();
        }

        try
        {
            var response = await client.GetAsync("/api/v1/training/exercises/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var items = await response.Content.ReadFromJsonAsync<TrainerExerciseCatalogItem[]>();
            Assert.NotNull(items);
            Assert.Equal(28, items!.Length);
            Assert.All(items, item =>
            {
                Assert.True(item.IsActive);
                Assert.False(string.IsNullOrWhiteSpace(item.Name));
                Assert.False(string.IsNullOrWhiteSpace(item.Slug));
                Assert.False(string.IsNullOrWhiteSpace(item.PrimaryMuscleGroup));
                Assert.False(string.IsNullOrWhiteSpace(item.ImageRef));
            });
            Assert.DoesNotContain(items, item => item.Id == inactive.Id);
            Assert.Equal(items.OrderBy(item => item.Name).ThenBy(item => item.Slug).ThenBy(item => item.Id), items);
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            cleanupDb.Exercises.Remove(inactive);
            await cleanupDb.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Trainer_catalog_search_is_trimmed_case_insensitive_and_combinable_with_muscle_group()
    {
        var query = $"/api/v1/training/exercises/?search={Uri.EscapeDataString("  SUPINO  ")}&muscleGroup={Uri.EscapeDataString(" peito ")}";

        var response = await client.GetAsync(query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<TrainerExerciseCatalogItem[]>();
        var item = Assert.Single(items!);
        Assert.Equal("Supino reto com barra", item.Name);
        Assert.Equal("Peito", item.PrimaryMuscleGroup);
    }

    [Fact]
    public async Task Trainer_catalog_rejects_oversized_filters_without_mutating_data()
    {
        var before = await CountCatalogExercises();
        var response = await client.GetAsync($"/api/v1/training/exercises/?search={new string('x', 101)}");
        var mutationResponse = await client.PostAsJsonAsync("/api/v1/training/exercises/", new { name = "Exercício livre" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, mutationResponse.StatusCode);
        Assert.Equal(before, await CountCatalogExercises());
    }

    [Fact]
    public async Task Trainer_catalog_requires_authentication()
    {
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/api/v1/training/exercises/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Trainer_lists_and_opens_owned_student_workouts_with_ordered_snapshot_details()
    {
        var listResponse = await client.GetAsync($"/api/v1/students/{DemoIds.StudentId}/workouts");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<StudentWorkoutListResponse>();
        var summary = Assert.Single(list!.Workouts);
        Assert.Equal("Força · Treino A", summary.Name);
        Assert.Equal(3, summary.ExerciseCount);

        var detailResponse = await client.GetAsync($"/api/v1/students/{DemoIds.StudentId}/workouts/{summary.Id}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<StudentWorkoutDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(DemoIds.StudentId, detail!.StudentId);
        Assert.Equal([1, 2, 3], detail.Exercises.Select(x => x.Sequence));
        Assert.All(detail.Exercises, exercise =>
        {
            Assert.NotEqual(Guid.Empty, exercise.ExerciseId);
            Assert.False(string.IsNullOrWhiteSpace(exercise.Name));
            Assert.False(string.IsNullOrWhiteSpace(exercise.ImageRef));
            Assert.True(exercise.RepetitionsMin <= exercise.RepetitionsMax);
        });
    }

    [Fact]
    public async Task Trainer_cannot_list_or_open_workouts_outside_an_active_student_link()
    {
        var otherTrainerId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var otherWorkoutId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Outro personal", CreatedAt = DateTimeOffset.UtcNow });
            db.Students.Add(new Student { Id = otherStudentId, FirstName = "Aluno", LastName = "Privado", CreatedAt = DateTimeOffset.UtcNow });
            db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = otherTrainerId, StudentId = otherStudentId, StartedAt = DateTimeOffset.UtcNow });
            db.StudentWorkouts.Add(new StudentWorkout { Id = otherWorkoutId, TrainerId = otherTrainerId, StudentId = otherStudentId, Name = "Treino privado", RecommendedDay = 1, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var listResponse = await client.GetAsync($"/api/v1/students/{otherStudentId}/workouts");
        var detailResponse = await client.GetAsync($"/api/v1/students/{otherStudentId}/workouts/{otherWorkoutId}");
        var historyResponse = await client.GetAsync($"/api/v1/students/{otherStudentId}/training-history");

        Assert.Equal(HttpStatusCode.NotFound, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, historyResponse.StatusCode);
        var listError = await listResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        var detailError = await detailResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("STUDENT_NOT_FOUND", listError!.Code);
        Assert.Equal("STUDENT_NOT_FOUND", detailError!.Code);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == otherWorkoutId));
        cleanupDb.TrainerStudents.RemoveRange(cleanupDb.TrainerStudents.Where(x => x.StudentId == otherStudentId));
        cleanupDb.Students.RemoveRange(cleanupDb.Students.Where(x => x.Id == otherStudentId));
        cleanupDb.Trainers.RemoveRange(cleanupDb.Trainers.Where(x => x.Id == otherTrainerId));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Trainer_history_returns_actual_synced_weight_and_repetitions()
    {
        var exercise = await GetActiveExercise();
        var workoutId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Histórico real", RecommendedDay = 1, CreatedAt = DateTimeOffset.UtcNow };
        var prescription = StudentWorkoutExercise.FromCatalog(workoutId, exercise, 1, 3, 8, 12, 60);
        workout.Exercises.Add(prescription);
        var session = new WorkoutSession { Id = sessionId, StudentId = DemoIds.StudentId, StudentWorkoutId = workoutId, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-20), CompletedAt = DateTimeOffset.UtcNow, Status = "Completed" };
        var snapshot = WorkoutSessionExercise.FromStudentWorkout(sessionId, prescription);
        snapshot.CompletedSets = 1;
        snapshot.Performances.Add(new SetPerformance { Id = Guid.NewGuid(), WorkoutSessionExerciseId = snapshot.Id, ClientOperationId = $"history-{Guid.NewGuid():N}", SetNumber = 1, WeightKg = 42.5m, Repetitions = 11, CompletedAt = DateTimeOffset.UtcNow.AddMinutes(-10) });
        session.Exercises.Add(snapshot);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.StudentWorkouts.Add(workout);
            db.WorkoutSessions.Add(session);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/v1/students/{DemoIds.StudentId}/training-history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<TrainingHistoryResponse>();
        var item = Assert.Single(history!.Sessions, x => x.SessionId == sessionId);
        Assert.Equal("Completed", item.Status);
        Assert.Equal(1, item.CompletedSets);
        var historyExercise = Assert.Single(item.Exercises);
        Assert.Equal(exercise.Name, historyExercise.Name);
        var set = Assert.Single(historyExercise.Sets);
        Assert.Equal(42.5m, set.WeightKg);
        Assert.Equal(11, set.Repetitions);

        await DeleteWorkoutAndSessions(workoutId);
    }

    [Fact]
    public async Task Trainer_can_add_edit_remove_and_reorder_student_workout_without_changing_started_session_snapshot()
    {
        var catalog = await GetActiveExercises(4);
        var workoutId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Editor atômico", RecommendedDay = 2, CreatedAt = DateTimeOffset.UtcNow };
        workout.Exercises.AddRange([
            StudentWorkoutExercise.FromCatalog(workoutId, catalog[0], 1, 3, 8, 10, 60, "Primeiro"),
            StudentWorkoutExercise.FromCatalog(workoutId, catalog[1], 2, 4, 10, 12, 75, "Segundo"),
            StudentWorkoutExercise.FromCatalog(workoutId, catalog[2], 3, 2, 12, 15, 45, "Remover"),
        ]);
        var originalFirst = workout.Exercises[0];
        var originalSecond = workout.Exercises[1];
        var removedId = workout.Exercises[2].Id;
        var session = new WorkoutSession { Id = sessionId, StudentId = DemoIds.StudentId, StudentWorkoutId = workoutId, StartedAt = DateTimeOffset.UtcNow, Status = "InProgress" };
        session.Exercises.AddRange(workout.Exercises.OrderBy(x => x.Sequence).Select(x => WorkoutSessionExercise.FromStudentWorkout(sessionId, x)));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.StudentWorkouts.Add(workout);
            db.WorkoutSessions.Add(session);
            await db.SaveChangesAsync();
        }

        var response = await client.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/workouts/{workoutId}", new
        {
            exercises = new object[]
            {
                new { id = originalSecond.Id, exerciseId = originalSecond.ExerciseId, sequence = 1, sets = 5, repetitionsMin = 6, repetitionsMax = 9, restSeconds = 120, notes = "Editado" },
                new { id = (Guid?)null, exerciseId = (Guid?)catalog[3].Id, sequence = 2, sets = 3, repetitionsMin = 10, repetitionsMax = 14, restSeconds = 80, notes = "Adicionado" },
                new { id = originalFirst.Id, exerciseId = originalFirst.ExerciseId, sequence = 3, sets = 3, repetitionsMin = 8, repetitionsMax = 10, restSeconds = 60, notes = "Primeiro" },
            },
        });

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var updated = await response.Content.ReadFromJsonAsync<StudentWorkoutDetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal([1, 2, 3], updated!.Exercises.Select(x => x.Sequence));
        Assert.Equal(originalSecond.Id, updated.Exercises[0].Id);
        Assert.Equal(5, updated.Exercises[0].Sets);
        Assert.Equal("Editado", updated.Exercises[0].Notes);
        Assert.Equal(catalog[3].Id, updated.Exercises[1].ExerciseId);
        Assert.Equal(catalog[3].Name, updated.Exercises[1].Name);
        Assert.Equal(catalog[3].ImageRef, updated.Exercises[1].ImageRef);
        Assert.Equal(originalFirst.Id, updated.Exercises[2].Id);
        Assert.DoesNotContain(updated.Exercises, x => x.Id == removedId);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var historical = await db.WorkoutSessionExercises.AsNoTracking().Where(x => x.WorkoutSessionId == sessionId).OrderBy(x => x.Sequence).ToListAsync();
            Assert.Equal(3, historical.Count);
            Assert.Equal([catalog[0].Name, catalog[1].Name, catalog[2].Name], historical.Select(x => x.Name));
            Assert.Equal([3, 4, 2], historical.Select(x => x.Sets));
            Assert.Equal(["Primeiro", "Segundo", "Remover"], historical.Select(x => x.Notes));
        }

        await DeleteWorkoutAndSessions(workoutId);
    }

    [Fact]
    public async Task Student_workout_update_rejects_invalid_sequence_range_and_inactive_addition_without_partial_changes()
    {
        var catalog = await GetActiveExercises(1);
        var inactive = new Exercise { Id = Guid.NewGuid(), Name = "Inativo", Slug = $"inativo-{Guid.NewGuid():N}", PrimaryMuscleGroup = "Peito", ImageRef = "assets/training/inativo.png", IsActive = false };
        var workoutId = Guid.NewGuid();
        var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Validação editor", RecommendedDay = 3, CreatedAt = DateTimeOffset.UtcNow };
        var existing = StudentWorkoutExercise.FromCatalog(workoutId, catalog[0], 1, 3, 8, 12, 60, "Original");
        workout.Exercises.Add(existing);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Exercises.Add(inactive);
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        var invalidSequence = await client.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/workouts/{workoutId}", new { exercises = new[] { new { id = existing.Id, exerciseId = existing.ExerciseId, sequence = 2, sets = 3, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 60 } } });
        var invalidRange = await client.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/workouts/{workoutId}", new { exercises = new[] { new { id = existing.Id, exerciseId = existing.ExerciseId, sequence = 1, sets = 3, repetitionsMin = 15, repetitionsMax = 8, restSeconds = 60 } } });
        var unknownAddition = await client.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/workouts/{workoutId}", new
        {
            exercises = new object[]
            {
                new { id = existing.Id, exerciseId = existing.ExerciseId, sequence = 1, sets = 3, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 60 },
                new { id = (Guid?)null, exerciseId = (Guid?)Guid.NewGuid(), sequence = 2, sets = 3, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 60 },
            },
        });
        var inactiveAddition = await client.PutAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/workouts/{workoutId}", new
        {
            exercises = new object[]
            {
                new { id = existing.Id, exerciseId = existing.ExerciseId, sequence = 1, sets = 3, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 60 },
                new { id = (Guid?)null, exerciseId = (Guid?)inactive.Id, sequence = 2, sets = 3, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 60 },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidSequence.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await invalidSequence.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await invalidRange.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, unknownAddition.StatusCode);
        Assert.Equal("EXERCISE_NOT_FOUND", (await unknownAddition.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, inactiveAddition.StatusCode);
        Assert.Equal("EXERCISE_NOT_FOUND", (await inactiveAddition.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var persisted = await db.StudentWorkoutExercises.AsNoTracking().SingleAsync(x => x.StudentWorkoutId == workoutId);
            Assert.Equal(existing.Id, persisted.Id);
            Assert.Equal(1, persisted.Sequence);
            Assert.Equal(3, persisted.Sets);
            Assert.Equal("Original", persisted.Notes);
        }

        await DeleteWorkoutAndSessions(workoutId);
        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.Exercises.RemoveRange(cleanupDb.Exercises.Where(x => x.Id == inactive.Id));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Trainer_cannot_update_student_workout_outside_owned_active_link()
    {
        var otherTrainerId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Outro personal", CreatedAt = DateTimeOffset.UtcNow });
            db.Students.Add(new Student { Id = otherStudentId, FirstName = "Aluno", LastName = "Privado", CreatedAt = DateTimeOffset.UtcNow });
            db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = otherTrainerId, StudentId = otherStudentId, StartedAt = DateTimeOffset.UtcNow });
            db.StudentWorkouts.Add(new StudentWorkout { Id = workoutId, TrainerId = otherTrainerId, StudentId = otherStudentId, Name = "Treino privado", RecommendedDay = 1, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var response = await client.PutAsJsonAsync($"/api/v1/students/{otherStudentId}/workouts/{workoutId}", new { exercises = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("STUDENT_NOT_FOUND", (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == workoutId));
        cleanupDb.TrainerStudents.RemoveRange(cleanupDb.TrainerStudents.Where(x => x.StudentId == otherStudentId));
        cleanupDb.Students.RemoveRange(cleanupDb.Students.Where(x => x.Id == otherStudentId));
        cleanupDb.Trainers.RemoveRange(cleanupDb.Trainers.Where(x => x.Id == otherTrainerId));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Trainer_creates_and_updates_a_multi_exercise_catalog_template()
    {
        var exercises = await GetActiveExercises(3);

        var response = await client.PostAsJsonAsync("/api/v1/training/templates/", new
        {
            name = "Upper teste",
            notes = "Catálogo",
            exercises = new[]
            {
                new { exerciseId = exercises[0].Id, sequence = 1, sets = 4, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 90, notes = "Primeiro" },
                new { exerciseId = exercises[1].Id, sequence = 2, sets = 3, repetitionsMin = 10, repetitionsMax = 15, restSeconds = 60, notes = "Segundo" },
                new { exerciseId = exercises[2].Id, sequence = 3, sets = 2, repetitionsMin = 12, repetitionsMax = 20, restSeconds = 45, notes = "Terceiro" },
            },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(created);
        Assert.Equal(3, created!.Exercises.Count);
        Assert.Equal(exercises.Select(x => x.Id), created.Exercises.Select(x => x.ExerciseId));

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/training/templates/{created.Id}", new
        {
            name = "Upper atualizado",
            notes = "Três exercícios completos",
            exercises = new[]
            {
                new { exerciseId = exercises[2].Id, sequence = 1, sets = 5, repetitionsMin = 6, repetitionsMax = 8, restSeconds = 120, notes = "Reordenado" },
                new { exerciseId = exercises[0].Id, sequence = 2, sets = 4, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 90, notes = "Mantido" },
                new { exerciseId = exercises[1].Id, sequence = 3, sets = 3, repetitionsMin = 10, repetitionsMax = 14, restSeconds = 75, notes = "Editado" },
            },
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(updated);
        Assert.Equal([exercises[2].Id, exercises[0].Id, exercises[1].Id], updated!.Exercises.Select(x => x.ExerciseId));
        Assert.Equal([5, 4, 3], updated.Exercises.Select(x => x.Sets));
        Assert.All(updated.Exercises, item => Assert.False(string.IsNullOrWhiteSpace(item.ImageRef)));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var saved = await db.WorkoutTemplates.Include(x => x.Exercises).SingleAsync(x => x.Id == created.Id);
        Assert.Equal(3, saved.Exercises.Count);
        Assert.Equal("Upper atualizado", saved.Name);
        db.WorkoutTemplateExercises.RemoveRange(saved.Exercises);
        db.WorkoutTemplates.Remove(saved);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Trainer_cannot_create_a_template_with_free_text_or_unknown_exercise()
    {
        var freeTextResponse = await client.PostAsJsonAsync("/api/v1/training/templates/", new
        {
            name = "Legado",
            exercises = new[]
            {
                new { name = "Exercício inventado", sequence = 1, sets = 3, repetitions = 10, restSeconds = 60 },
            },
        });
        Assert.Equal(HttpStatusCode.BadRequest, freeTextResponse.StatusCode);

        var missingResponse = await client.PostAsJsonAsync("/api/v1/training/templates/", new
        {
            name = "Referência inexistente",
            exercises = new[]
            {
                new { exerciseId = Guid.NewGuid(), sequence = 1, sets = 3, repetitionsMin = 8, repetitionsMax = 10, restSeconds = 60 },
            },
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);
        var error = await missingResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("EXERCISE_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task Trainer_cannot_create_an_invalid_prescription_range()
    {
        var exercise = await GetActiveExercise();

        var response = await client.PostAsJsonAsync("/api/v1/training/templates/", new
        {
            name = "Faixa inválida",
            exercises = new[]
            {
                new { exerciseId = exercise.Id, sequence = 1, sets = 3, repetitionsMin = 12, repetitionsMax = 8, restSeconds = 60 },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task Trainer_cannot_update_another_trainers_template()
    {
        var exercise = await GetActiveExercise();
        var otherTrainerId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Outro personal", CreatedAt = DateTimeOffset.UtcNow });
            var template = new WorkoutTemplate { Id = templateId, TrainerId = otherTrainerId, Name = "Privado", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            template.Exercises.Add(new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = templateId, ExerciseId = exercise.Id, Sequence = 1, Sets = 3, RepetitionsMin = 8, RepetitionsMax = 10, RestSeconds = 60 });
            db.WorkoutTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        var response = await client.PutAsJsonAsync($"/api/v1/training/templates/{templateId}", new
        {
            name = "Tentativa",
            exercises = new[]
            {
                new { exerciseId = exercise.Id, sequence = 1, sets = 3, repetitionsMin = 8, repetitionsMax = 10, restSeconds = 60 },
            },
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.WorkoutTemplateExercises.RemoveRange(cleanupDb.WorkoutTemplateExercises.Where(x => x.WorkoutTemplateId == templateId));
        cleanupDb.WorkoutTemplates.RemoveRange(cleanupDb.WorkoutTemplates.Where(x => x.Id == templateId));
        cleanupDb.Trainers.RemoveRange(cleanupDb.Trainers.Where(x => x.Id == otherTrainerId));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Trainer_duplicates_a_template_without_reinserting_catalog_exercises()
    {
        var exercises = await GetActiveExercises(3);
        var templateId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var template = new WorkoutTemplate { Id = templateId, TrainerId = DemoIds.TrainerId, Name = "Upper original", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            template.Exercises.AddRange(exercises.Select((exercise, index) => new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = templateId, ExerciseId = exercise.Id, Sequence = index + 1, Sets = index + 2, RepetitionsMin = 8 + index, RepetitionsMax = 12 + index, RestSeconds = 60 + index * 15, Notes = $"Nota {index + 1}" }));
            db.WorkoutTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/v1/training/templates/{templateId}/duplicate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var duplicate = await response.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(duplicate);
        Assert.NotEqual(templateId, duplicate!.Id);
        Assert.Equal(3, duplicate.Exercises.Count);
        Assert.Equal(exercises.Select(x => x.Id), duplicate.Exercises.Select(x => x.ExerciseId));
        Assert.Equal(["Nota 1", "Nota 2", "Nota 3"], duplicate.Exercises.Select(x => x.Notes));

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        Assert.Equal(3, await cleanupDb.Exercises.CountAsync(x => exercises.Select(item => item.Id).Contains(x.Id)));
        var ids = new[] { templateId, duplicate.Id };
        cleanupDb.WorkoutTemplateExercises.RemoveRange(cleanupDb.WorkoutTemplateExercises.Where(x => ids.Contains(x.WorkoutTemplateId)));
        cleanupDb.WorkoutTemplates.RemoveRange(cleanupDb.WorkoutTemplates.Where(x => ids.Contains(x.Id)));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Applying_a_template_copies_catalog_and_prescription_values_into_a_student_snapshot()
    {
        var catalogExercises = await GetActiveExercises(3);
        var catalogExercise = catalogExercises[0];
        var templateId = Guid.NewGuid();
        var originalCatalogName = catalogExercise.Name;
        var originalImageRef = catalogExercise.ImageRef;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var template = new WorkoutTemplate { Id = templateId, TrainerId = DemoIds.TrainerId, Name = "Snapshot source", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            template.Exercises.AddRange(catalogExercises.Select((exercise, index) => new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = templateId, ExerciseId = exercise.Id, Sequence = index + 1, Sets = 4 - index, RepetitionsMin = 8 + index, RepetitionsMax = 12 + index, RestSeconds = 90 - index * 15, Notes = $"Prescrição {index + 1}" }));
            db.WorkoutTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        var applyResponse = await client.PostAsJsonAsync($"/api/v1/training/templates/{templateId}/apply", new { studentId = DemoIds.StudentId, recommendedDay = 2, isRecommended = false });
        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);
        var applied = await applyResponse.Content.ReadFromJsonAsync<AppliedWorkoutResponse>();
        Assert.NotNull(applied);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var catalog = await db.Exercises.SingleAsync(x => x.Id == catalogExercise.Id);
            var templatePrescriptions = await db.WorkoutTemplateExercises.Where(x => x.WorkoutTemplateId == templateId).ToListAsync();
            catalog.Name = "Nome alterado no catálogo";
            catalog.ImageRef = "assets/training/alterado.png";
            foreach (var templatePrescription in templatePrescriptions)
            {
                templatePrescription.Sets = 9;
                templatePrescription.RepetitionsMin = 20;
                templatePrescription.RepetitionsMax = 25;
                templatePrescription.Notes = "Template alterado";
            }
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var snapshots = await db.StudentWorkoutExercises.AsNoTracking().Where(x => x.StudentWorkoutId == applied.Id).OrderBy(x => x.Sequence).ToListAsync();
            Assert.Equal(3, snapshots.Count);
            var snapshot = snapshots[0];
            Assert.Equal(catalogExercise.Id, snapshot.ExerciseId);
            Assert.Equal(originalCatalogName, snapshot.Name);
            Assert.Equal(originalImageRef, snapshot.ImageRef);
            Assert.Equal(4, snapshot.Sets);
            Assert.Equal(8, snapshot.RepetitionsMin);
            Assert.Equal(12, snapshot.RepetitionsMax);
            Assert.Equal("Prescrição 1", snapshot.Notes);
            Assert.Equal([1, 2, 3], snapshots.Select(x => x.Sequence));
            Assert.Equal([4, 3, 2], snapshots.Select(x => x.Sets));
        }

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var catalogToRestore = await cleanupDb.Exercises.SingleAsync(x => x.Id == catalogExercise.Id);
        catalogToRestore.Name = originalCatalogName;
        catalogToRestore.ImageRef = originalImageRef;
        cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == applied.Id));
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == applied.Id));
        cleanupDb.WorkoutTemplateExercises.RemoveRange(cleanupDb.WorkoutTemplateExercises.Where(x => x.WorkoutTemplateId == templateId));
        cleanupDb.WorkoutTemplates.RemoveRange(cleanupDb.WorkoutTemplates.Where(x => x.Id == templateId));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Applying_template_enforces_template_and_student_ownership_and_replaces_current_recommendation()
    {
        var exercise = await GetActiveExercise();
        var ownedTemplateId = Guid.NewGuid();
        var otherTemplateId = Guid.NewGuid();
        var otherTrainerId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        Guid existingRecommendedId;
        Guid[] priorRecommendedIds;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            priorRecommendedIds = await db.StudentWorkouts.Where(x => x.StudentId == DemoIds.StudentId && x.IsRecommended).Select(x => x.Id).ToArrayAsync();
            db.Trainers.Add(new Trainer { Id = otherTrainerId, Name = "Outro personal", CreatedAt = DateTimeOffset.UtcNow });
            db.Students.Add(new Student { Id = otherStudentId, FirstName = "Aluno", LastName = "Sem vínculo", CreatedAt = DateTimeOffset.UtcNow });
            var owned = new WorkoutTemplate { Id = ownedTemplateId, TrainerId = DemoIds.TrainerId, Name = "Modelo próprio", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            owned.Exercises.Add(new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = ownedTemplateId, ExerciseId = exercise.Id, Sequence = 1, Sets = 3, RepetitionsMin = 8, RepetitionsMax = 12, RestSeconds = 60 });
            var other = new WorkoutTemplate { Id = otherTemplateId, TrainerId = otherTrainerId, Name = "Modelo alheio", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            other.Exercises.Add(new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = otherTemplateId, ExerciseId = exercise.Id, Sequence = 1, Sets = 3, RepetitionsMin = 8, RepetitionsMax = 12, RestSeconds = 60 });
            var existingRecommended = new StudentWorkout { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Recomendação anterior", RecommendedDay = 1, IsRecommended = true, CreatedAt = DateTimeOffset.UtcNow };
            existingRecommendedId = existingRecommended.Id;
            db.WorkoutTemplates.AddRange(owned, other);
            db.StudentWorkouts.Add(existingRecommended);
            await db.SaveChangesAsync();
        }

        var otherTemplateResponse = await client.PostAsJsonAsync($"/api/v1/training/templates/{otherTemplateId}/apply", new { studentId = DemoIds.StudentId, recommendedDay = 2, isRecommended = false });
        var otherStudentResponse = await client.PostAsJsonAsync($"/api/v1/training/templates/{ownedTemplateId}/apply", new { studentId = otherStudentId, recommendedDay = 2, isRecommended = false });
        var invalidDayResponse = await client.PostAsJsonAsync($"/api/v1/training/templates/{ownedTemplateId}/apply", new { studentId = DemoIds.StudentId, recommendedDay = 8, isRecommended = false });
        var applyResponse = await client.PostAsJsonAsync($"/api/v1/training/templates/{ownedTemplateId}/apply", new { studentId = DemoIds.StudentId, recommendedDay = 4, isRecommended = true });

        Assert.Equal(HttpStatusCode.NotFound, otherTemplateResponse.StatusCode);
        Assert.Equal("TEMPLATE_NOT_FOUND", (await otherTemplateResponse.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.NotFound, otherStudentResponse.StatusCode);
        Assert.Equal("STUDENT_NOT_FOUND", (await otherStudentResponse.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidDayResponse.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await invalidDayResponse.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);
        var applied = await applyResponse.Content.ReadFromJsonAsync<AppliedWorkoutResponse>();
        Assert.NotNull(applied);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        Assert.False((await cleanupDb.StudentWorkouts.SingleAsync(x => x.Id == existingRecommendedId)).IsRecommended);
        var created = await cleanupDb.StudentWorkouts.SingleAsync(x => x.Id == applied!.Id);
        Assert.True(created.IsRecommended);
        Assert.Equal(4, created.RecommendedDay);
        var priorRecommendations = await cleanupDb.StudentWorkouts.Where(x => priorRecommendedIds.Contains(x.Id)).ToListAsync();
        foreach (var prior in priorRecommendations)
            prior.IsRecommended = true;
        cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == applied.Id));
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == applied.Id || x.Id == existingRecommendedId));
        cleanupDb.WorkoutTemplateExercises.RemoveRange(cleanupDb.WorkoutTemplateExercises.Where(x => x.WorkoutTemplateId == ownedTemplateId || x.WorkoutTemplateId == otherTemplateId));
        cleanupDb.WorkoutTemplates.RemoveRange(cleanupDb.WorkoutTemplates.Where(x => x.Id == ownedTemplateId || x.Id == otherTemplateId));
        cleanupDb.Students.RemoveRange(cleanupDb.Students.Where(x => x.Id == otherStudentId));
        cleanupDb.Trainers.RemoveRange(cleanupDb.Trainers.Where(x => x.Id == otherTrainerId));
        await cleanupDb.SaveChangesAsync();
    }

    private async Task<Exercise> GetActiveExercise()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.Exercises.AsNoTracking().OrderBy(x => x.Name).FirstAsync(x => x.IsActive);
    }

    private async Task<IReadOnlyList<Exercise>> GetActiveExercises(int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.Exercises.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Take(count).ToListAsync();
    }

    private async Task DeleteWorkoutAndSessions(Guid workoutId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var sessionIds = await db.WorkoutSessions.Where(x => x.StudentWorkoutId == workoutId).Select(x => x.Id).ToArrayAsync();
        db.SetPerformances.RemoveRange(db.SetPerformances.Where(x => sessionIds.Contains(x.WorkoutSessionExercise.WorkoutSessionId)));
        db.WorkoutSessionExercises.RemoveRange(db.WorkoutSessionExercises.Where(x => sessionIds.Contains(x.WorkoutSessionId)));
        db.WorkoutSessions.RemoveRange(db.WorkoutSessions.Where(x => x.StudentWorkoutId == workoutId));
        db.StudentWorkoutExercises.RemoveRange(db.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == workoutId));
        db.StudentWorkouts.RemoveRange(db.StudentWorkouts.Where(x => x.Id == workoutId));
        await db.SaveChangesAsync();
    }

    private async Task<int> CountCatalogExercises()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.Exercises.CountAsync();
    }

    private sealed record TemplateResponse(Guid Id, IReadOnlyList<TemplateExerciseResponse> Exercises);
    private sealed record TemplateExerciseResponse(Guid ExerciseId, string Name, string ImageRef, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
    private sealed record AppliedWorkoutResponse(Guid Id);
    private sealed record ErrorResponse(string Code);
    private sealed record TrainerExerciseCatalogItem(Guid Id, string Name, string Slug, string PrimaryMuscleGroup, string? Equipment, string ImageRef, string? Instructions, bool IsActive);
    private sealed record StudentWorkoutListResponse(IReadOnlyList<StudentWorkoutSummaryResponse> Workouts);
    private sealed record StudentWorkoutSummaryResponse(Guid Id, string Name, int ExerciseCount);
    private sealed record StudentWorkoutDetailResponse(Guid Id, Guid StudentId, IReadOnlyList<StudentWorkoutExerciseResponse> Exercises);
    private sealed record StudentWorkoutExerciseResponse(Guid Id, Guid? ExerciseId, string Name, string? ImageRef, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
    private sealed record TrainingHistoryResponse(IReadOnlyList<TrainingHistoryItemResponse> Sessions);
    private sealed record TrainingHistoryItemResponse(Guid SessionId, string Status, int CompletedSets, IReadOnlyList<TrainingHistoryExerciseResponse> Exercises);
    private sealed record TrainingHistoryExerciseResponse(string Name, IReadOnlyList<TrainingHistorySetResponse> Sets);
    private sealed record TrainingHistorySetResponse(int SetNumber, decimal WeightKg, int Repetitions);
}
