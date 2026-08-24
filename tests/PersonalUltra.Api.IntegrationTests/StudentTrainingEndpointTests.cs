using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task Student_preview_returns_signed_media_url_without_changing_snapshot_reference()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);
        var workoutId = Guid.NewGuid();
        Guid workoutExerciseId;
        string stableImageRef;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var exercise = await db.Exercises.AsNoTracking().FirstAsync(x => x.ImageRef.StartsWith("media://"));
            stableImageRef = exercise.ImageRef;
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Treino com mídia remota", SuggestedOrder = 99, CreatedAt = DateTimeOffset.UtcNow };
            var snapshot = StudentWorkoutExercise.FromCatalog(workoutId, exercise, 1, 3, 8, 12, 60);
            workoutExerciseId = snapshot.Id;
            workout.Exercises.Add(snapshot);
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        try
        {
            var preview = await client.GetFromJsonAsync<PreviewResponse>($"/api/v1/training/{workoutId}");
            var item = Assert.Single(preview!.Exercises);
            Assert.Equal(stableImageRef, item.ImageRef);
            Assert.StartsWith("https://", item.ImageUrl);

            using var verificationScope = factory.Services.CreateScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            Assert.Equal(stableImageRef, await verificationDb.StudentWorkoutExercises.Where(x => x.Id == workoutExerciseId).Select(x => x.ImageRef).SingleAsync());
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == workoutId));
            cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == workoutId));
            await cleanupDb.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Training_list_and_preview_are_neutral_and_ordered_by_suggested_order()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);

        var trainingResponse = await client.GetAsync("/api/v1/training/");
        var trainingJson = await trainingResponse.Content.ReadAsStringAsync();
        using var trainingDocument = JsonDocument.Parse(trainingJson);
        Assert.False(trainingDocument.RootElement.TryGetProperty("recommended", out _));
        Assert.False(trainingDocument.RootElement.TryGetProperty("available", out _));
        foreach (var item in trainingDocument.RootElement.GetProperty("workouts").EnumerateArray())
        {
        }
        var training = JsonSerializer.Deserialize<StudentTrainingListResponse>(trainingJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var workouts = training!.Workouts.ToArray();

        Assert.Equal([1, 2, 3, 4], workouts.Select(workout => workout.SuggestedOrder));
        Assert.DoesNotContain("Recommended", workouts.Select(workout => workout.State));
        Assert.DoesNotContain("Available", workouts.Select(workout => workout.State));

        var selected = workouts[0];
        var previewResponse = await client.GetAsync($"/api/v1/training/{selected.Id}");
        var previewJson = await previewResponse.Content.ReadAsStringAsync();
        using var previewDocument = JsonDocument.Parse(previewJson);
        var preview = JsonSerializer.Deserialize<PreviewResponse>(previewJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(selected.SuggestedOrder, preview!.SuggestedOrder);
        Assert.Equal("Ready", preview.State);
    }

    [Fact]
    public async Task Empty_trainer_draft_is_hidden_from_student_until_it_has_exercises()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);
        var workoutId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.StudentWorkouts.Add(new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Rascunho Trainer", SuggestedOrder = 1, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var training = await client.GetFromJsonAsync<StudentTrainingListResponse>("/api/v1/training/");
        var preview = await client.GetAsync($"/api/v1/training/{workoutId}");
        var start = await client.PostAsync($"/api/v1/training/{workoutId}/start", null);

        Assert.DoesNotContain(training!.Workouts, workout => workout.Id == workoutId);
        Assert.Equal(HttpStatusCode.NotFound, preview.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == workoutId));
        await cleanupDb.SaveChangesAsync();
    }

    [Fact]
    public async Task Deleted_trainer_workout_is_not_available_to_student()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);
        var workoutId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var exercise = await db.Exercises.AsNoTracking().FirstAsync(x => x.IsActive);
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Treino excluído", SuggestedOrder = 1, IsActive = false, CreatedAt = DateTimeOffset.UtcNow };
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, exercise, 1, 3, 8, 12, 60));
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        var training = await client.GetFromJsonAsync<StudentTrainingListResponse>("/api/v1/training/");
        var preview = await client.GetAsync($"/api/v1/training/{workoutId}");
        var start = await client.PostAsync($"/api/v1/training/{workoutId}/start", null);

        Assert.DoesNotContain(training!.Workouts, workout => workout.Id == workoutId);
        Assert.Equal(HttpStatusCode.NotFound, preview.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => x.StudentWorkoutId == workoutId));
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => x.Id == workoutId));
        await cleanupDb.SaveChangesAsync();
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
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Sessão snapshot", SuggestedOrder = 1, CreatedAt = DateTimeOffset.UtcNow };
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

        var incompleteResponse = await client.PostAsync($"/api/v1/training/sessions/{started.SessionId}/complete", null);
        Assert.Equal(HttpStatusCode.Conflict, incompleteResponse.StatusCode);

        var skippedSet = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId = $"test-{Guid.NewGuid():N}", setNumber = 3, weightKg = 40m, repetitions = 10 });
        Assert.Equal(HttpStatusCode.Conflict, skippedSet.StatusCode);

        for (var setNumber = 2; setNumber <= historical.Sets; setNumber++)
        {
            var nextSet = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId = $"test-{Guid.NewGuid():N}", setNumber, weightKg = 40m + setNumber, repetitions = 10 });
            Assert.Equal(HttpStatusCode.OK, nextSet.StatusCode);
        }

        var completeResponse = await client.PostAsync($"/api/v1/training/sessions/{started.SessionId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<CompletionResponse>();
        var completeRetryResponse = await client.PostAsync($"/api/v1/training/sessions/{started.SessionId}/complete", null);
        var completedRetry = await completeRetryResponse.Content.ReadFromJsonAsync<CompletionResponse>();
        Assert.Equal(HttpStatusCode.OK, completeRetryResponse.StatusCode);
        Assert.Equal(completed!.CompletedAt, completedRetry!.CompletedAt);

        var replayAfterCompletion = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId, setNumber = 1, weightKg = 42.5m, repetitions = 11 });
        Assert.Equal(HttpStatusCode.OK, replayAfterCompletion.StatusCode);

        var afterCompletion = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{historical.Id}/sets", new { clientOperationId = $"test-{Guid.NewGuid():N}", setNumber = 2, weightKg = 40m, repetitions = 10 });
        Assert.Equal(HttpStatusCode.Conflict, afterCompletion.StatusCode);

        var detailResponse = await client.GetAsync($"/api/v1/training/sessions/{started.SessionId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<SessionDetailResponse>();
        Assert.Equal(started.SessionId, detail!.SessionId);
        Assert.Equal("Completed", detail.Status);
        Assert.NotNull(detail.CompletedAt);
        var detailExercise = Assert.Single(detail.Exercises);
        Assert.Equal(historical.Sets, detailExercise.Performances.Count);
        var detailPerformance = Assert.Single(detailExercise.Performances, x => x.SetNumber == 1);
        Assert.Equal(42.5m, detailPerformance.WeightKg);
        Assert.Equal(11, detailPerformance.Repetitions);

        var trainingResponse = await client.GetAsync("/api/v1/training");
        var training = await trainingResponse.Content.ReadFromJsonAsync<TrainingResponse>();
        var history = Assert.Single(training!.History, x => x.SessionId == started.SessionId);
        Assert.Equal("Completed", history.Status);
        Assert.Equal(historical.Sets, history.CompletedSets);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var performance = await verifyDb.SetPerformances.AsNoTracking().SingleAsync(x => x.WorkoutSessionExerciseId == historical.Id && x.SetNumber == 1);
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
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Sessão protegida", SuggestedOrder = 1, CreatedAt = DateTimeOffset.UtcNow };
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
        var detailResponse = await client.GetAsync($"/api/v1/training/sessions/{sessionId}");
        var listResponse = await client.GetFromJsonAsync<StudentTrainingListResponse>("/api/v1/training/");
        var previewResponse = await client.GetAsync($"/api/v1/training/{workoutId}");
        var startResponse = await client.PostAsync($"/api/v1/training/{workoutId}/start", null);

        Assert.Equal(HttpStatusCode.NotFound, setResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, completionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
        Assert.DoesNotContain(listResponse!.Workouts, workout => workout.Id == workoutId);
        Assert.Equal(HttpStatusCode.NotFound, previewResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, startResponse.StatusCode);

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
    public async Task Starting_a_workout_suggests_the_students_latest_performance_for_each_exercise()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);
        var previousWorkoutId = Guid.NewGuid();
        var previousSessionId = Guid.NewGuid();
        var currentWorkoutId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var catalog = await db.Exercises.AsNoTracking().FirstAsync(x => x.IsActive);
            var previousWorkout = new StudentWorkout { Id = previousWorkoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Treino anterior", SuggestedOrder = 1, CreatedAt = DateTimeOffset.UtcNow.AddDays(-8) };
            var previousPrescription = StudentWorkoutExercise.FromCatalog(previousWorkoutId, catalog, 1, 3, 8, 12, 60);
            previousWorkout.Exercises.Add(previousPrescription);
            var previousSession = new WorkoutSession { Id = previousSessionId, StudentId = DemoIds.StudentId, StudentWorkoutId = previousWorkoutId, StartedAt = DateTimeOffset.UtcNow.AddDays(-7), CompletedAt = DateTimeOffset.UtcNow.AddDays(-7).AddHours(1), Status = "Completed" };
            var previousExercise = WorkoutSessionExercise.FromStudentWorkout(previousSessionId, previousPrescription);
            previousExercise.CompletedSets = 2;
            previousExercise.Performances.Add(new SetPerformance { Id = Guid.NewGuid(), WorkoutSessionExerciseId = previousExercise.Id, ClientOperationId = $"previous-{Guid.NewGuid():N}", SetNumber = 1, WeightKg = 30m, Repetitions = 12, CompletedAt = DateTimeOffset.UtcNow.AddDays(-7) });
            previousExercise.Performances.Add(new SetPerformance { Id = Guid.NewGuid(), WorkoutSessionExerciseId = previousExercise.Id, ClientOperationId = $"latest-{Guid.NewGuid():N}", SetNumber = 2, WeightKg = 32.5m, Repetitions = 10, CompletedAt = DateTimeOffset.UtcNow.AddDays(-7).AddMinutes(5) });
            previousSession.Exercises.Add(previousExercise);
            var currentWorkout = new StudentWorkout { Id = currentWorkoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Treino atual", SuggestedOrder = 2, CreatedAt = DateTimeOffset.UtcNow };
            currentWorkout.Exercises.Add(StudentWorkoutExercise.FromCatalog(currentWorkoutId, catalog, 1, 3, 8, 12, 60));
            db.StudentWorkouts.AddRange(previousWorkout, currentWorkout);
            db.WorkoutSessions.Add(previousSession);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/v1/training/{currentWorkoutId}/start", null);
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestion = Assert.Single(session!.Exercises).PreviousPerformance;
        Assert.NotNull(suggestion);
        Assert.Equal(32.5m, suggestion.WeightKg);
        Assert.Equal(10, suggestion.Repetitions);

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        var workoutIds = new[] { previousWorkoutId, currentWorkoutId };
        var sessionIds = await cleanupDb.WorkoutSessions.Where(x => workoutIds.Contains(x.StudentWorkoutId)).Select(x => x.Id).ToArrayAsync();
        cleanupDb.SetPerformances.RemoveRange(cleanupDb.SetPerformances.Where(x => sessionIds.Contains(x.WorkoutSessionExercise.WorkoutSessionId)));
        cleanupDb.WorkoutSessionExercises.RemoveRange(cleanupDb.WorkoutSessionExercises.Where(x => sessionIds.Contains(x.WorkoutSessionId)));
        cleanupDb.WorkoutSessions.RemoveRange(cleanupDb.WorkoutSessions.Where(x => sessionIds.Contains(x.Id)));
        cleanupDb.StudentWorkoutExercises.RemoveRange(cleanupDb.StudentWorkoutExercises.Where(x => workoutIds.Contains(x.StudentWorkoutId)));
        cleanupDb.StudentWorkouts.RemoveRange(cleanupDb.StudentWorkouts.Where(x => workoutIds.Contains(x.Id)));
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
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Preview read-only", SuggestedOrder = 1, CreatedAt = DateTimeOffset.UtcNow };
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, catalog[1], 2, 3, 10, 12, 60, "Segundo"));
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, catalog[0], 1, 4, 8, 10, 90, "Primeiro"));
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        var before = await CountSessions(workoutId);
        var response = await client.GetAsync($"/api/v1/training/{workoutId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.Equal("Ready", preview!.State);
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

    [Fact]
    public async Task Student_can_record_a_later_sequence_before_the_first_without_changing_prescription_order()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var sessionToken = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken!.AccessToken);

        var workoutId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            var catalog = await db.Exercises.AsNoTracking().Where(x => x.IsActive).Take(2).ToArrayAsync();
            var suggestedOrder = await db.StudentWorkouts.Where(x => x.StudentId == DemoIds.StudentId).Select(x => (int?)x.SuggestedOrder).MaxAsync() ?? 0;
            var workout = new StudentWorkout { Id = workoutId, TrainerId = DemoIds.TrainerId, StudentId = DemoIds.StudentId, Name = "Ordem livre", SuggestedOrder = suggestedOrder + 1, CreatedAt = DateTimeOffset.UtcNow };
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, catalog[0], 1, 2, 8, 12, 60));
            workout.Exercises.Add(StudentWorkoutExercise.FromCatalog(workoutId, catalog[1], 2, 2, 8, 12, 60));
            db.StudentWorkouts.Add(workout);
            await db.SaveChangesAsync();
        }

        var startResponse = await client.PostAsync($"/api/v1/training/{workoutId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var started = await startResponse.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.Equal(2, started!.Exercises.Count);
        var sequenceOne = started.Exercises.Single(x => x.Sequence == 1);
        var sequenceTwo = started.Exercises.Single(x => x.Sequence == 2);
        var operationTwo = $"free-order-two-{Guid.NewGuid():N}";
        var operationOne = $"free-order-one-{Guid.NewGuid():N}";

        var secondSet = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{sequenceTwo.Id}/sets", new { clientOperationId = operationTwo, setNumber = 1, weightKg = 30m, repetitions = 10 });
        Assert.Equal(HttpStatusCode.OK, secondSet.StatusCode);

        var afterSecond = await client.GetFromJsonAsync<SessionDetailResponse>($"/api/v1/training/sessions/{started.SessionId}");
        Assert.Collection(afterSecond!.Exercises.OrderBy(x => x.Sequence),
            item => { Assert.Equal(1, item.Sequence); Assert.Equal(0, item.CompletedSets); },
            item => { Assert.Equal(2, item.Sequence); Assert.Equal(1, item.CompletedSets); });

        var firstSet = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{sequenceOne.Id}/sets", new { clientOperationId = operationOne, setNumber = 1, weightKg = 25m, repetitions = 12 });
        Assert.Equal(HttpStatusCode.OK, firstSet.StatusCode);

        var replay = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{sequenceTwo.Id}/sets", new { clientOperationId = operationTwo, setNumber = 1, weightKg = 30m, repetitions = 10 });
        var conflictingReplay = await client.PostAsJsonAsync($"/api/v1/training/sessions/{started.SessionId}/exercises/{sequenceTwo.Id}/sets", new { clientOperationId = $"different-{Guid.NewGuid():N}", setNumber = 1, weightKg = 35m, repetitions = 8 });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflictingReplay.StatusCode);

        var detail = await client.GetFromJsonAsync<SessionDetailResponse>($"/api/v1/training/sessions/{started.SessionId}");
        Assert.Collection(detail!.Exercises.OrderBy(x => x.Sequence),
            item => { Assert.Equal(1, item.Sequence); Assert.Equal(1, item.CompletedSets); },
            item => { Assert.Equal(2, item.Sequence); Assert.Equal(1, item.CompletedSets); });

        using var cleanupScope = factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        cleanupDb.SetPerformances.RemoveRange(cleanupDb.SetPerformances.Where(x => x.WorkoutSessionExercise.WorkoutSessionId == started.SessionId));
        cleanupDb.WorkoutSessionExercises.RemoveRange(cleanupDb.WorkoutSessionExercises.Where(x => x.WorkoutSessionId == started.SessionId));
        cleanupDb.WorkoutSessions.RemoveRange(cleanupDb.WorkoutSessions.Where(x => x.Id == started.SessionId));
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
    private sealed record StudentTrainingListResponse(IReadOnlyList<StudentWorkoutSummary> Workouts, IReadOnlyList<TrainingHistoryItem> History);
    private sealed record StudentWorkoutSummary(Guid Id, int SuggestedOrder, string State);
    private sealed record SessionResponse(Guid SessionId, IReadOnlyList<SessionExerciseResponse> Exercises);
    private sealed record SessionExerciseResponse(Guid Id, string Name, string? ImageRef, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes, int CompletedSets, SessionDetailPerformanceResponse? PreviousPerformance);
    private sealed record TrainingResponse(IReadOnlyList<TrainingHistoryItem> History);
    private sealed record TrainingHistoryItem(Guid SessionId, string Status, int CompletedSets);
    private sealed record PreviewResponse(Guid Id, int SuggestedOrder, string State, Guid? ActiveSessionId, DateTimeOffset? LastCompletedAt, IReadOnlyList<PreviewExerciseResponse> Exercises);
    private sealed record PreviewExerciseResponse(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? ImageUrl, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes);
    private sealed record CompletionResponse(Guid Id, string Status, DateTimeOffset? CompletedAt);
    private sealed record SessionDetailResponse(Guid SessionId, Guid WorkoutId, string WorkoutName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, IReadOnlyList<SessionDetailExerciseResponse> Exercises);
    private sealed record SessionDetailExerciseResponse(Guid Id, Guid? ExerciseId, string Name, string? PrimaryMuscleGroup, string? Equipment, string? ImageRef, string? Instructions, int Sequence, int Sets, int RepetitionsMin, int RepetitionsMax, int RestSeconds, string Notes, int CompletedSets, IReadOnlyList<SessionDetailPerformanceResponse> Performances);
    private sealed record SessionDetailPerformanceResponse(int SetNumber, decimal WeightKg, int Repetitions, DateTimeOffset CompletedAt);
}
