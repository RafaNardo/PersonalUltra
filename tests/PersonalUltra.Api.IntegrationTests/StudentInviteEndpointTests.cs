using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class StudentInviteEndpointTests : IClassFixture<StudentApiFactory>
{
    private readonly StudentApiFactory factory;
    private readonly HttpClient client;

    public StudentInviteEndpointTests(StudentApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Invite_resolution_returns_the_active_invites_minimum_context_without_authentication()
    {
        const string token = "valid-student-invite-token";
        await SeedInviteAsync(token, DateTimeOffset.UtcNow.AddDays(1));

        var response = await client.GetAsync($"/api/v1/invite/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invite = await response.Content.ReadFromJsonAsync<InviteResolutionResponse>();
        Assert.Equal("Severo", invite!.TrainerName);
        Assert.Equal("aluna@example.com", invite.Email);
    }

    [Fact]
    public async Task Invite_resolution_hides_expired_invites()
    {
        const string token = "expired-student-invite-token";
        await SeedInviteAsync(token, DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await client.GetAsync($"/api/v1/invite/{token}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invite_code_resolves_and_accepts_an_active_invite()
    {
        const string token = "invite-by-code";
        const string code = "234567";
        await SeedInviteAsync(token, DateTimeOffset.UtcNow.AddDays(1), "code@example.com", code);

        var resolution = await client.GetAsync($"/api/v1/invite/code/{code[..3]}-{code[3..]}");
        var accepted = await client.PostAsJsonAsync($"/api/v1/invite/code/{code}/accept", new { firstName = "Código", email = "code@example.com", phone = "11999990000" });

        Assert.Equal(HttpStatusCode.OK, resolution.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task Student_email_login_only_accepts_an_existing_core_student()
    {
        var known = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "unknown@example.com" });

        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        var session = await known.Content.ReadFromJsonAsync<InviteAcceptanceResponse>();
        Assert.Equal(DemoIds.StudentId, session!.StudentId);
    }

    [Fact]
    public async Task Invite_acceptance_creates_a_student_linked_to_the_inviting_trainer_and_a_student_session()
    {
        const string token = "invite-to-accept";
        await SeedInviteAsync(token, DateTimeOffset.UtcNow.AddDays(1), "ana@example.com");

        var response = await client.PostAsJsonAsync($"/api/v1/invite/{token}/accept", new { firstName = "Ana", lastName = "Souza", email = "ana@example.com", phone = "(11) 99999-8888" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var acceptance = await response.Content.ReadFromJsonAsync<InviteAcceptanceResponse>();
        Assert.Equal("Ana", acceptance!.FirstName);
        Assert.Equal(DemoIds.TrainerId, acceptance.TrainerId);
        Assert.NotEmpty(acceptance.AccessToken);
        Assert.Equal("+11999998888", acceptance.Phone);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        Assert.True(await db.TrainerStudents.AnyAsync(link => link.TrainerId == DemoIds.TrainerId && link.StudentId == acceptance.StudentId));
        var acceptedInvite = await db.StudentInvites.SingleAsync(invite => invite.Token == token);
        Assert.NotNull(acceptedInvite.AcceptedAt);
    }

    [Fact]
    public async Task Invited_student_can_save_and_complete_a_typed_anamnesis()
    {
        const string token = "invite-for-anamnesis";
        await SeedInviteAsync(token, DateTimeOffset.UtcNow.AddDays(1), "beatriz@example.com");
        var acceptanceResponse = await client.PostAsJsonAsync($"/api/v1/invite/{token}/accept", new { firstName = "Beatriz", lastName = "Lima", email = "beatriz@example.com", phone = "11988887777" });
        var acceptance = await acceptanceResponse.Content.ReadFromJsonAsync<InviteAcceptanceResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", acceptance!.AccessToken);
        var answers = new { goal = "Ganhar força", experienceLevel = "Iniciante", trainingDaysPerWeek = 3, sessionDurationMinutes = 45, trainingLocation = "Academia", equipmentNotes = "Academia completa", heightCm = 165, weightKg = 62, healthConditions = "Nenhuma", movementRestrictions = "Nenhuma", currentPainDescription = "Sem dor", nutritionPreferences = "4 refeições", nutritionRestrictions = "Nenhuma" };

        var save = await client.PutAsJsonAsync("/api/v1/anamnesis", answers);
        var complete = await client.PostAsync("/api/v1/anamnesis/complete", null);

        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var result = await complete.Content.ReadFromJsonAsync<AnamnesisResponse>();
        Assert.True(result!.IsCompleted);
        Assert.Equal("Ganhar força", result.Goal);
    }

    [Fact]
    public async Task Invited_student_can_read_their_active_trainer_message()
    {
        const string token = "invite-for-message";
        await SeedInviteAsync(token, DateTimeOffset.UtcNow.AddDays(1), "carla@example.com");
        var acceptanceResponse = await client.PostAsJsonAsync($"/api/v1/invite/{token}/accept", new { firstName = "Carla", email = "carla@example.com", phone = "11977776666" });
        var acceptance = await acceptanceResponse.Content.ReadFromJsonAsync<InviteAcceptanceResponse>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
            db.TrainerMessages.Add(new TrainerMessage { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = acceptance!.StudentId, Message = "Bem-vinda, Carla!", StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1), CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", acceptance!.AccessToken);

        var response = await client.GetAsync("/api/v1/home/trainer-message");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<ActiveTrainerMessageResponse>();
        Assert.Equal("Bem-vinda, Carla!", message!.Message);
    }

    private async Task SeedInviteAsync(string token, DateTimeOffset expiresAt, string email = "aluna@example.com", string? inviteCode = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        db.StudentInvites.Add(new StudentInvite { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, Token = token, InviteCode = inviteCode, Email = email, CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = expiresAt });
        await db.SaveChangesAsync();
    }

    private sealed record InviteResolutionResponse(string TrainerName, string? Email, DateTimeOffset ExpiresAt);
    private sealed record InviteAcceptanceResponse(string AccessToken, string TokenType, Guid StudentId, string FirstName, string LastName, string Email, string Phone, Guid TrainerId);
    private sealed record AnamnesisResponse(string Goal, string ExperienceLevel, int TrainingDaysPerWeek, int SessionDurationMinutes, string TrainingLocation, string EquipmentNotes, decimal HeightCm, decimal WeightKg, string HealthConditions, string MovementRestrictions, string CurrentPainDescription, string NutritionPreferences, string NutritionRestrictions, bool IsCompleted);
    private sealed record ActiveTrainerMessageResponse(Guid Id, string Message, DateTimeOffset StartsAt, DateTimeOffset? ExpiresAt);
}

public sealed class StudentApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"student-api-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        ConfigureExerciseMediaTestBucket(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PersonalUltraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PersonalUltraDbContext>>();
            services.AddDbContext<PersonalUltraDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }

    private static void ConfigureExerciseMediaTestBucket(IWebHostBuilder builder)
    {
        builder.UseSetting("RailwayBucket:EndpointUrl", "https://test.storageapi.dev");
        builder.UseSetting("RailwayBucket:Region", "auto");
        builder.UseSetting("RailwayBucket:ForcePathStyle", "false");
        builder.UseSetting("RailwayBucket:SignedUrlLifetimeMinutes", "15");
        builder.UseSetting("RailwayBucket:BucketName", "personal-ultra-tests");
        builder.UseSetting("RailwayBucket:AccessKeyId", "test-access-key");
        builder.UseSetting("RailwayBucket:SecretAccessKey", "test-secret-key");
    }
}
