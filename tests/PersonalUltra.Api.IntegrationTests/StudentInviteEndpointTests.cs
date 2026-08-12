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
        Assert.Equal("Alex Personal", invite!.TrainerName);
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

    private async Task SeedInviteAsync(string token, DateTimeOffset expiresAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalUltraDbContext>();
        db.StudentInvites.Add(new StudentInvite { Id = Guid.NewGuid(), TrainerId = DemoIds.TrainerId, Token = token, Email = "aluna@example.com", CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = expiresAt });
        await db.SaveChangesAsync();
    }

    private sealed record InviteResolutionResponse(string TrainerName, string? Email, DateTimeOffset ExpiresAt);
}

public sealed class StudentApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PersonalUltraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PersonalUltraDbContext>>();
            services.AddDbContext<PersonalUltraDbContext>(options => options.UseInMemoryDatabase("student-invite-tests"));
        });
    }
}
