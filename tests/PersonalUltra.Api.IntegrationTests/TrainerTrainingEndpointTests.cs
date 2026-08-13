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

        Assert.Equal(HttpStatusCode.NotFound, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
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
    public async Task Trainer_creates_a_template_with_catalog_exercise_and_repetition_range()
    {
        var exercise = await GetActiveExercise();

        var response = await client.PostAsJsonAsync("/api/v1/training/templates/", new
        {
            name = "Upper teste",
            notes = "Catálogo",
            exercises = new[]
            {
                new { exerciseId = exercise.Id, sequence = 1, sets = 4, repetitionsMin = 8, repetitionsMax = 12, restSeconds = 90, notes = "Controle" },
            },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(created);
        var prescription = Assert.Single(created!.Exercises);
        Assert.Equal(exercise.Id, prescription.ExerciseId);
        Assert.Equal(exercise.Name, prescription.Name);
        Assert.Equal(8, prescription.RepetitionsMin);
        Assert.Equal(12, prescription.RepetitionsMax);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var saved = await db.WorkoutTemplates.Include(x => x.Exercises).SingleAsync(x => x.Id == created.Id);
        var savedPrescription = Assert.Single(saved.Exercises);
        Assert.Equal(exercise.Id, savedPrescription.ExerciseId);
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
        var exercise = await GetActiveExercise();
        var templateId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var template = new WorkoutTemplate { Id = templateId, TrainerId = DemoIds.TrainerId, Name = "Upper original", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            template.Exercises.Add(new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = templateId, ExerciseId = exercise.Id, Sequence = 1, Sets = 4, RepetitionsMin = 8, RepetitionsMax = 12, RestSeconds = 90 });
            db.WorkoutTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/v1/training/templates/{templateId}/duplicate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var duplicate = await response.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.NotNull(duplicate);
        Assert.NotEqual(templateId, duplicate!.Id);
        Assert.Equal(exercise.Id, Assert.Single(duplicate.Exercises).ExerciseId);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        Assert.Equal(1, await cleanupDb.Exercises.CountAsync(x => x.Id == exercise.Id));
        var ids = new[] { templateId, duplicate.Id };
        cleanupDb.WorkoutTemplateExercises.RemoveRange(cleanupDb.WorkoutTemplateExercises.Where(x => ids.Contains(x.WorkoutTemplateId)));
        cleanupDb.WorkoutTemplates.RemoveRange(cleanupDb.WorkoutTemplates.Where(x => ids.Contains(x.Id)));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Applying_a_template_copies_catalog_and_prescription_values_into_a_student_snapshot()
    {
        var catalogExercise = await GetActiveExercise();
        var templateId = Guid.NewGuid();
        var originalCatalogName = catalogExercise.Name;
        var originalImageRef = catalogExercise.ImageRef;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var template = new WorkoutTemplate { Id = templateId, TrainerId = DemoIds.TrainerId, Name = "Snapshot source", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            template.Exercises.Add(new WorkoutTemplateExercise { Id = Guid.NewGuid(), WorkoutTemplateId = templateId, ExerciseId = catalogExercise.Id, Sequence = 1, Sets = 4, RepetitionsMin = 8, RepetitionsMax = 12, RestSeconds = 90, Notes = "Prescrição original" });
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
            var templatePrescription = await db.WorkoutTemplateExercises.SingleAsync(x => x.WorkoutTemplateId == templateId);
            catalog.Name = "Nome alterado no catálogo";
            catalog.ImageRef = "assets/training/alterado.png";
            templatePrescription.Sets = 9;
            templatePrescription.RepetitionsMin = 20;
            templatePrescription.RepetitionsMax = 25;
            templatePrescription.Notes = "Template alterado";
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var snapshot = await db.StudentWorkoutExercises.AsNoTracking().SingleAsync(x => x.StudentWorkoutId == applied.Id);
            Assert.Equal(catalogExercise.Id, snapshot.ExerciseId);
            Assert.Equal(originalCatalogName, snapshot.Name);
            Assert.Equal(originalImageRef, snapshot.ImageRef);
            Assert.Equal(4, snapshot.Sets);
            Assert.Equal(8, snapshot.RepetitionsMin);
            Assert.Equal(12, snapshot.RepetitionsMax);
            Assert.Equal("Prescrição original", snapshot.Notes);
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

    private async Task<Exercise> GetActiveExercise()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.Exercises.AsNoTracking().OrderBy(x => x.Name).FirstAsync(x => x.IsActive);
    }

    private async Task<int> CountCatalogExercises()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.Exercises.CountAsync();
    }

    private sealed record TemplateResponse(Guid Id, IReadOnlyList<TemplateExerciseResponse> Exercises);
    private sealed record TemplateExerciseResponse(Guid ExerciseId, string Name, int RepetitionsMin, int RepetitionsMax);
    private sealed record AppliedWorkoutResponse(Guid Id);
    private sealed record ErrorResponse(string Code);
    private sealed record TrainerExerciseCatalogItem(Guid Id, string Name, string Slug, string PrimaryMuscleGroup, string? Equipment, string ImageRef, string? Instructions, bool IsActive);
    private sealed record StudentWorkoutListResponse(IReadOnlyList<StudentWorkoutSummaryResponse> Workouts);
    private sealed record StudentWorkoutSummaryResponse(Guid Id, string Name, int ExerciseCount);
    private sealed record StudentWorkoutDetailResponse(Guid Id, Guid StudentId, IReadOnlyList<StudentWorkoutExerciseResponse> Exercises);
    private sealed record StudentWorkoutExerciseResponse(Guid? ExerciseId, string Name, string? ImageRef, int Sequence, int RepetitionsMin, int RepetitionsMax);
}
