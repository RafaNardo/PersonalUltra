extern alias trainerapi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

public sealed class TrainerDashboardEndpointTests : IClassFixture<TrainerApiFactory>
{
    private readonly HttpClient client;
    private readonly TrainerApiFactory factory;

    public TrainerDashboardEndpointTests(TrainerApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Dashboard_returns_only_the_authenticated_trainers_active_students()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        var response = await client.GetAsync("/api/v1/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal("Alex Personal", dashboard!.TrainerName);
        Assert.Equal(1, dashboard.ActiveStudents);
        Assert.Equal(1, dashboard.PendingAnamneses);
        Assert.Equal(0, dashboard.CompletedAnamneses);
        var student = Assert.Single(dashboard.RecentStudents);
        Assert.Equal("Rafa", student.FirstName);
        Assert.Equal("NotStarted", student.AnamnesisStatus);
    }

    [Fact]
    public async Task Dashboard_rejects_a_request_without_the_trainer_identity()
    {
        var response = await client.GetAsync("/api/v1/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Students_returns_the_authenticated_trainers_active_students()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        var response = await client.GetAsync("/api/v1/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<StudentListResponse>();
        var student = Assert.Single(list!.Students);
        Assert.Equal("Rafa", student.FirstName);
        Assert.Equal("Silva", student.LastName);
    }

    [Fact]
    public async Task Student_detail_is_limited_to_the_authenticated_trainers_active_link()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        var response = await client.GetAsync($"/api/v1/students/{DemoIds.StudentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var student = await response.Content.ReadFromJsonAsync<DashboardStudentSummary>();
        Assert.Equal(DemoIds.StudentId, student!.StudentId);
        var missing = await client.GetAsync($"/api/v1/students/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var error = await missing.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("STUDENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task Trainer_can_create_an_in_app_message_for_an_owned_student()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        var response = await client.PostAsJsonAsync($"/api/v1/students/{DemoIds.StudentId}/messages", new { message = "Bora treinar hoje.", startsAt = (DateTimeOffset?)null, expiresAt = (DateTimeOffset?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<TrainerMessageResponse>();
        Assert.Equal(DemoIds.StudentId, message!.StudentId);
        Assert.Equal("Bora treinar hoje.", message.Message);
    }

    [Fact]
    public async Task Trainer_can_read_a_completed_anamnesis_for_an_owned_student()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);
        var studentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var answers = new AnamnesisAnswers("Hipertrofia", "Intermediário", 4, 60, "Academia", "Halteres e máquinas", 180, 82, "Nenhuma", "Nenhuma", "Sem dor", "Sem restrições", "Nenhuma");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        db.Students.Add(new Student { Id = studentId, FirstName = "Ana", LastName = "Teste", Email = "ana.teste@example.com", CreatedAt = now });
        db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, StudentId = studentId, StartedAt = now });
        db.Anamneses.Add(new Anamnesis { Id = Guid.NewGuid(), StudentId = studentId, CreatedAt = now, UpdatedAt = now, CompletedAt = now, AnswersJson = JsonSerializer.Serialize(answers) });
        await db.SaveChangesAsync();

        try
        {
            var response = await client.GetAsync($"/api/v1/students/{studentId}/anamnesis");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var anamnesis = await response.Content.ReadFromJsonAsync<TrainerAnamnesisResponse>();
            Assert.NotNull(anamnesis);
            Assert.Equal("Hipertrofia", anamnesis!.Goal);
            Assert.Equal(4, anamnesis.TrainingDaysPerWeek);
            Assert.Equal(180, anamnesis.HeightCm);

            var dashboardResponse = await client.GetAsync("/api/v1/dashboard");
            var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponse>();
            var activity = Assert.Single(dashboard!.RecentActivities);
            Assert.Equal(studentId, activity.StudentId);
            Assert.Equal("AnamnesisCompleted", activity.Type);
        }
        finally
        {
            db.Anamneses.RemoveRange(db.Anamneses.Where(item => item.StudentId == studentId));
            db.TrainerStudents.RemoveRange(db.TrainerStudents.Where(item => item.StudentId == studentId));
            db.Students.RemoveRange(db.Students.Where(item => item.Id == studentId));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Trainer_can_create_a_secure_expiring_student_invite_link()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DemoActorAuthenticationHandler.TrainerToken);

        var response = await client.PostAsJsonAsync("/api/v1/student-invites", new { email = "aluna@example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invite = await response.Content.ReadFromJsonAsync<StudentInviteResponse>();
        Assert.Equal("aluna@example.com", invite!.Email);
        Assert.StartsWith("personalultra://invite/", invite.InviteUrl);
        Assert.True(invite.Token.Length >= 40);
        Assert.True(invite.ExpiresAt > DateTimeOffset.UtcNow.AddDays(6));
    }

    private sealed record DashboardResponse(string TrainerName, int ActiveStudents, int PendingAnamneses, int CompletedAnamneses, IReadOnlyList<DashboardStudentSummary> RecentStudents, IReadOnlyList<DashboardActivity> RecentActivities);
    private sealed record DashboardActivity(Guid StudentId, string StudentName, string Type, DateTimeOffset OccurredAt);
    private sealed record DashboardStudentSummary(Guid StudentId, string FirstName, string LastName, string? Email, string AnamnesisStatus, DateTimeOffset StartedAt);
    private sealed record StudentListResponse(IReadOnlyList<DashboardStudentSummary> Students);
    private sealed record ErrorResponse(string Code, string Message, object? Details, string TraceId);
    private sealed record TrainerMessageResponse(Guid Id, Guid StudentId, string Message, DateTimeOffset StartsAt, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);
    private sealed record StudentInviteResponse(Guid Id, string Token, string InviteUrl, string? Email, DateTimeOffset ExpiresAt);
    private sealed record TrainerAnamnesisResponse(string Goal, string ExperienceLevel, int TrainingDaysPerWeek, int SessionDurationMinutes, string TrainingLocation, string EquipmentNotes, decimal HeightCm, decimal WeightKg, string HealthConditions, string MovementRestrictions, string CurrentPainDescription, string NutritionPreferences, string NutritionRestrictions, DateTimeOffset CompletedAt);
}

public sealed class TrainerApiFactory : WebApplicationFactory<trainerapi::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PersonalUltraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PersonalUltraDbContext>>();
            services.AddDbContext<PersonalUltraDbContext>(options => options.UseInMemoryDatabase("trainer-dashboard-tests"));
        });
    }
}
