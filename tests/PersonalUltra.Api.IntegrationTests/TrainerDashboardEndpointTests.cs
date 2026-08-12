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

    private sealed record DashboardResponse(string TrainerName, int ActiveStudents, int PendingAnamneses, int CompletedAnamneses, IReadOnlyList<DashboardStudentSummary> RecentStudents);
    private sealed record DashboardStudentSummary(Guid StudentId, string FirstName, string LastName, string? Email, string AnamnesisStatus, DateTimeOffset StartedAt);
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
