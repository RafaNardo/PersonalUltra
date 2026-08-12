using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace PersonalUltra.Infrastructure;

public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IHostEnvironment environment,
    DemoSessionTokenService sessions,
    PersonalUltraDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevBearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment() && !configuration.GetValue<bool>("DemoAuth:Enabled")) return AuthenticateResult.NoResult();

        var authorization = Request.Headers.Authorization.ToString();
        var expected = configuration["DevAuth:Token"];
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..];
        if (!sessions.TryValidateStudent(token, out var studentId) || !await db.Students.AsNoTracking().AnyAsync(student => student.Id == studentId, Context.RequestAborted))
            return AuthenticateResult.NoResult();

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, studentId.ToString()), new Claim("subject", "student")
        ], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
