using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

/// <summary>
/// Development-only actor identity seam. Each API registers only its own
/// scheme, so the demo role picker can never grant access to the other actor.
/// Production authentication deliberately remains out of scope.
/// </summary>
public sealed class DemoActorAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment,
    PersonalUltraDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TrainerScheme = "TrainerDemoBearer";
    public const string StudentScheme = "StudentDemoBearer";
    public const string TrainerToken = "personal-ultra-demo-trainer";
    public const string StudentToken = "personal-ultra-demo-student";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment()) return AuthenticateResult.NoResult();

        var token = Request.Headers.Authorization.ToString();
        if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

        var isTrainer = Scheme.Name == TrainerScheme;
        var expectedToken = isTrainer ? TrainerToken : StudentToken;
        if (!string.Equals(token["Bearer ".Length..], expectedToken, StringComparison.Ordinal)) return AuthenticateResult.NoResult();

        var id = isTrainer ? DemoIds.TrainerId : DemoIds.StudentId;
        var exists = isTrainer
            ? await db.Trainers.AsNoTracking().AnyAsync(x => x.Id == id, Context.RequestAborted)
            : await db.Students.AsNoTracking().AnyAsync(x => x.Id == id, Context.RequestAborted);
        if (!exists) return AuthenticateResult.Fail("The requested demo identity is unavailable.");

        var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim("actor", isTrainer ? "trainer" : "student")],
        Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
