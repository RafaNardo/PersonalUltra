using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class StudentTrainingEndpointTests : IClassFixture<StudentApiFactory>
{
    private readonly HttpClient client;
    private readonly StudentApiFactory factory;

    public StudentTrainingEndpointTests(StudentApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Starting_a_workout_copies_an_immutable_session_snapshot_and_preserves_actual_performance()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);

        Guid workoutId;
        Guid studentWorkoutExerciseId;
        Exercise catalogExercise;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            catalogExercise = await db.Exercises.AsNoTracking().FirstAsync(x => x.IsActive);
            workoutId = Guid.NewGuid();
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Sessão snapshot", RecommendedDay = 3, CreatedAt = DateTimeOffset.UtcNow };
            var prescription = StudentWorkoutExercise.FromCatalog(workoutId, catalogExercise, 1, 4, 8, 12, 75, "Nota original");
            studentWorkoutExerciseId = prescription.Id;
            workout.Exercises.Add(prescription);
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        var startResponse = await client.PostAsync($"/api/v1/training/{workoutId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var started = await startResponse.Content.ReadFromJsonAsync<SessionResponse>();
        var startedExercise = Assert.Single(started!.Exercises);
        Assert.Equal(8, startedExercise.RepetitionsMin);
        Assert.Equal(12, startedExercise.RepetitionsMax);
        Assert.Equal(catalogExercise.ImageRef, startedExercise.ImageRef);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var prescription = await db.StudentWorkoutExercises.SingleAsync(x => x.Id == studentWorkoutExerciseId);
            prescription.Name = "Workout alterado";
            prescription.ImageRef = "assets/training/workout-alterado.png";
            prescription.Sets = 2;
            prescription.RepetitionsMin = 20;
            prescription.RepetitionsMax = 25;
            prescription.RestSeconds = 15;
            prescription.Notes = "Nota alterada";
            await db.SaveChangesAsync();
        }

        var resumedResponse = await client.PostAsync($"/api/v1/training/{workoutId}/start", null);
        var resumed = await resumedResponse.Content.ReadFromJsonAsync<SessionResponse>();
        var historical = Assert.Single(resumed!.Exercises);
        Assert.Equal(catalogExercise.Name, historical.Name);
        Assert.Equal(catalogExercise.ImageRef, historical.ImageRef);
        Assert.Equal(4, historical.Sets);
        Assert.Equal(8, historical.RepetitionsMin);
        Assert.Equal(12, historical.RepetitionsMax);
        Assert.Equal(75, historical.RestSeconds);
        Assert.Equal("Nota original", historical.Notes);

        var setResponse = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { setNumber = 1, weightKg = 42.5m, repetitions = 11 });
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var performance = await verifyDb.SetPerformances.AsNoTracking().SingleAsync(x => x.WorkoutSessionExerciseId == historical.Id);
        Assert.Equal(42.5m, performance.WeightKg);
        Assert.Equal(11, performance.Repetitions);
    }

    private sealed record LoginResponse(string AccessToken);
    private sealed record SessionResponse(Guid SessionId, IReadOnlyList<SessionExerciseResponse> Exercises);
    private sealed record SessionExerciseResponse(Guid Id, string Name, string? ImageRef, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
}
