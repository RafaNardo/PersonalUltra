extern alias trainerapi;

using System.Net;
using System.Net.Http.Headers;
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

public sealed class TrainerDashboardEndpointTests : IClassFixture<TrainerApiFactory>
{
    private readonly HttpClient client;

    public TrainerDashboardEndpointTests(TrainerApiFactory factory) => client = factory.CreateClient();

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

    private sealed record DashboardResponse(string TrainerName, int ActiveStudents, int PendingAnamneses, int CompletedAnamneses, IReadOnlyList<DashboardStudentSummary> RecentStudents);
    private sealed record DashboardStudentSummary(Guid StudentId, string FirstName, string LastName, string? Email, string AnamnesisStatus, DateTimeOffset StartedAt);
    private sealed record StudentListResponse(IReadOnlyList<DashboardStudentSummary> Students);
    private sealed record ErrorResponse(string Code, string Message, object? Details, string TraceId);
    private sealed record TrainerMessageResponse(Guid Id, Guid StudentId, string Message, DateTimeOffset StartsAt, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);
    private sealed record StudentInviteResponse(Guid Id, string Token, string InviteUrl, string? Email, DateTimeOffset ExpiresAt);
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
