using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class StudentProfileEndpointTests : IClassFixture<StudentApiFactory>
{
    private readonly HttpClient client;
    public StudentProfileEndpointTests(StudentApiFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task Student_can_read_update_and_clear_preferred_name_but_unauthenticated_cannot()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/profile")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/student-login", new { email = "demo@student.personalultra.local" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var initial = await client.GetFromJsonAsync<Profile>("/api/v1/profile");
        Assert.Equal("Rafa", initial!.FirstName);
        var saved = await client.PutAsJsonAsync("/api/v1/profile", new { preferredName = "  Dê  " });
        var profile = await saved.Content.ReadFromJsonAsync<Profile>();
        Assert.Equal("Dê", profile!.PreferredName);
        var cleared = await client.PutAsJsonAsync("/api/v1/profile", new { preferredName = "   " });
        Assert.Null((await cleared.Content.ReadFromJsonAsync<Profile>())!.PreferredName);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/profile", new { preferredName = new string('x', 101) })).StatusCode);
    }

    private sealed record Session(string AccessToken);
    private sealed record Profile(string FirstName, string LastName, string? Email, string? Phone, string? PreferredName);
}
