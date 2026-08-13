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

    private async Task<Exercise> GetActiveExercise()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.Exercises.AsNoTracking().OrderBy(x => x.Name).FirstAsync(x => x.IsActive);
    }

    private sealed record TemplateResponse(Guid Id, IReadOnlyList<TemplateExerciseResponse> Exercises);
    private sealed record TemplateExerciseResponse(Guid ExerciseId, string Name, int RepetitionsMin, int RepetitionsMax);
    private sealed record ErrorResponse(string Code);
}
