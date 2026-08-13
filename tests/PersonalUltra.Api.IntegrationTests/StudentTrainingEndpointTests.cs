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

        var clientOperationId = $"test-{Guid.NewGuid():N}";
        var setResponse = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId, setNumber = 1, weightKg = 42.5m, repetitions = 11 });
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var retryResponse = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId, setNumber = 1, weightKg = 42.5m, repetitions = 11 });
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);

        var reusedOperation = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId, setNumber = 1, weightKg = 50m, repetitions = 9 });
        Assert.Equal(HttpStatusCode.Conflict, reusedOperation.StatusCode);

        var duplicateSet = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId = $"test-{Guid.NewGuid():N}", setNumber = 1, weightKg = 42.5m, repetitions = 11 });
        Assert.Equal(HttpStatusCode.Conflict, duplicateSet.StatusCode);

        var completeResponse = await client.PostAsync($"/api/v1/training/sessions/{started.SessionId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<CompletionResponse>();
        var completeRetryResponse = await client.PostAsync($"/api/v1/training/sessions/{started.SessionId}/complete", null);
        var completedRetry = await completeRetryResponse.Content.ReadFromJsonAsync<CompletionResponse>();
        Assert.Equal(HttpStatusCode.OK, completeRetryResponse.StatusCode);
        Assert.Equal(completed!.CompletedAt, completedRetry!.CompletedAt);

        var afterCompletion = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId = $"test-{Guid.NewGuid():N}", setNumber = 2, weightKg = 40m, repetitions = 10 });
        Assert.Equal(HttpStatusCode.Conflict, afterCompletion.StatusCode);

        var trainingResponse = await client.GetAsync("/api/v1/training");
        var training = await trainingResponse.Content.ReadFromJsonAsync<TrainingResponse>();
        var history = Assert.Single(training!.History, x => x.SessionId == started.SessionId);
        Assert.Equal("Completed", history.Status);
        Assert.Equal(1, history.CompletedSets);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var performance = await verifyDb.SetPerformances.AsNoTracking().SingleAsync(x => x.WorkoutSessionExerciseId == historical.Id);
        Assert.Equal(clientOperationId, performance.ClientOperationId);
        Assert.Equal(42.5m, performance.WeightKg);
        Assert.Equal(11, performance.Repetitions);
    }

    [Fact]
    public async Task Student_cannot_record_sets_or_complete_another_students_session()
    {
        var otherStudentId = Guid.NewGuid();
        var otherEmail = $"other-{Guid.NewGuid():N}@student.test";
        var workoutId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        Guid sessionExerciseId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var catalog = await db.Exercises.AsNoTracking().FirstAsync(x => x.IsActive);
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Sessão protegida", RecommendedDay = 1, CreatedAt = DateTimeOffset.UtcNow };
            var prescription = StudentWorkoutExercise.FromCatalog(workoutId, catalog, 1, 3, 8, 12, 60);
            workout.Exercises.Add(prescription);
            var protectedSession = new WorkoutSession { Id = sessionId, StudentId = DemoIds.StudentId, StudentWorkoutId = workoutId, StartedAt = DateTimeOffset.UtcNow, Status = "InProgress" };
            var snapshot = WorkoutSessionExercise.FromStudentWorkout(sessionId, prescription);
            sessionExerciseId = snapshot.Id;
            protectedSession.Exercises.Add(snapshot);
            db.Students.Add(new Student { Id = otherStudentId, FirstName = "Outro", LastName = "Aluno", Email = otherEmail, CreatedAt = DateTimeOffset.UtcNow });
            db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = otherStudentId, StartedAt = DateTimeOffset.UtcNow });
            db.StudentWorkouts.Add(workout);
            db.WorkoutSessions.Add(protectedSession);
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = otherEmail });
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var setResponse = await client.PostAsJsonAsync($"/api/v1/training/sessions/{sessionId}/exercises/{sessionExerciseId}/sets", new { clientOperationId = $"ownership-{Guid.NewGuid():N}", setNumber = 1, weightKg = 20m, repetitions = 10 });
        var completionResponse = await client.PostAsync($"/api/v1/training/sessions/{sessionId}/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, setResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, completionResponse.StatusCode);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.WorkoutSessionExercises.RemoveRange(cleanupDb.WorkoutSessionExercises.Where(x => x.WorkoutSessionId == sessionId));
        cleanupDb.WorkoutSessions.RemoveRange(cleanupDb.WorkoutSessions.Where(x => x.Id == sessionId));
        cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == workoutId));
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == workoutId));
        cleanupDb.TrainerStudents.RemoveRange(cleanupDb.TrainerStudents.Where(x => x.StudentId == otherStudentId));
        cleanupDb.Students.RemoveRange(cleanupDb.Students.Where(x => x.Id == otherStudentId));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Workout_preview_is_read_only_and_returns_ordered_student_snapshot()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);
        var workoutId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var catalog = await db.Exercises.AsNoTracking().Where(x => x.IsActive).Take(2).ToArrayAsync();
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Preview read-only", RecommendedDay = 6, CreatedAt = DateTimeOffset.UtcNow };
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, catalog[1], 2, 3, 10, 12, 60, "Segundo"));
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, catalog[0], 1, 4, 8, 10, 90, "Primeiro"));
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        var before = await CountSessions(workoutId);
        var response = await client.GetAsync($"/api/v1/training/{workoutId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.Equal("Available", preview!.State);
        Assert.Null(preview.ActiveSessionId);
        Assert.Collection(preview.Exercises,
            first => { Assert.Equal(1, first.Sequence); Assert.Equal(4, first.Sets); Assert.Equal("Primeiro", first.Notes); },
            second => { Assert.Equal(2, second.Sequence); Assert.Equal(3, second.Sets); Assert.Equal("Segundo", second.Notes); });
        Assert.Equal(before, await CountSessions(workoutId));

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == workoutId));
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == workoutId));
        await cleanupDb.SaveChangesAsync();
    }

    private async Task<int> CountSessions(Guid workoutId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        return await db.WorkoutSessions.CountAsync(x => x.StudentWorkoutId == workoutId);
    }

    private sealed record LoginResponse(string AccessToken);
    private sealed record SessionResponse(Guid SessionId, IReadOnlyList<SessionExerciseResponse> Exercises);
    private sealed record SessionExerciseResponse(Guid Id, string Name, string? ImageRef, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes, int CompletedSets);
    private sealed record TrainingResponse(IReadOnlyList<TrainingHistoryItem> History);
    private sealed record TrainingHistoryItem(Guid SessionId, string Status, int CompletedSets);
    private sealed record PreviewResponse(Guid Id, string State, Guid? ActiveSessionId, IReadOnlyList<PreviewExerciseResponse> Exercises);
    private sealed record PreviewExerciseResponse(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
    private sealed record CompletionResponse(Guid Id, string Status, DateTimeOffset? CompletedAt);
}
