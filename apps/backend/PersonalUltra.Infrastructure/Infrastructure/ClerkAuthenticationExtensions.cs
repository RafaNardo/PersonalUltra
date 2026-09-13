using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using PersonalUltra.Domain;

namespace PersonalUltra.Infrastructure;

public enum ClerkActor
{
    Trainer,
    Student,
}

public static class ClerkAuthenticationExtensions
{
    public const string SessionPolicy = "ClerkSession";
    internal const string ActorIdClaim = "personal-ultra-actor-id";

    public static IServiceCollection AddClerkAuthentication(this IServiceCollection services, IConfiguration configuration, ClerkActor actor)
    {
        if (configuration.GetValue<bool>("Clerk:IntegrationTestAuthentication"))
        {
            services.AddAuthentication(IntegrationTestAuthenticationHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthenticationHandler>(IntegrationTestAuthenticationHandler.TestScheme, _ => { });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(IntegrationTestAuthenticationHandler.TestScheme).RequireAuthenticatedUser().RequireClaim(ActorIdClaim).Build();
                options.AddPolicy(SessionPolicy, new AuthorizationPolicyBuilder(IntegrationTestAuthenticationHandler.TestScheme).RequireAuthenticatedUser().Build());
            });
            return services;
        }

        var issuer = Required(configuration, "Clerk:Issuer");
        var jwksUrl = Required(configuration, "Clerk:JwksUrl");
        var audience = Required(configuration, "Clerk:Audience");
        var jwks = new ConfigurationManager<JsonWebKeySet>(jwksUrl, new ClerkJwksRetriever(), new HttpClient());

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    IssuerSigningKeyResolver = (_, _, _, _) => jwks.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult().Keys,
                };
            });

        services.AddScoped<IClaimsTransformation>(_ => new ClerkActorClaimsTransformation(actor, _.GetRequiredService<PersonalUltraDbContext>()));
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(ActorIdClaim)
                .Build();
            options.AddPolicy(SessionPolicy, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());
        });

        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"A configuração {key} é obrigatória.");

    private sealed class ClerkJwksRetriever : IConfigurationRetriever<JsonWebKeySet>
    {
        public async Task<JsonWebKeySet> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var document = await retriever.GetDocumentAsync(address, cancel);
            return new JsonWebKeySet(document);
        }
    }

    private sealed class ClerkActorClaimsTransformation(ClerkActor actor, PersonalUltraDbContext db) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.HasClaim(claim => claim.Type == ActorIdClaim)) return principal;
            var subject = principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject)) return principal;

            var actorId = actor switch
            {
                ClerkActor.Trainer => await db.Trainers.AsNoTracking().Where(item => item.ClerkUserId == subject).Select(item => (Guid?)item.Id).SingleOrDefaultAsync(),
                ClerkActor.Student => await db.Students.AsNoTracking().Where(item => item.ClerkUserId == subject).Select(item => (Guid?)item.Id).SingleOrDefaultAsync(),
                _ => null,
            };
            if (actorId is null) return principal;

            var identity = principal.Identity as ClaimsIdentity;
            identity?.AddClaim(new Claim(ActorIdClaim, actorId.Value.ToString()));
            identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, actorId.Value.ToString()));
            identity?.AddClaim(new Claim("actor", actor.ToString()));
            identity?.AddClaim(new Claim("subject", actor == ClerkActor.Student ? "student" : "trainer"));
            return principal;
        }
    }
}

/// <summary>Authentication used only by in-process integration tests; it is opt-in through test configuration.</summary>
public sealed class IntegrationTestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestScheme = "PersonalUltraIntegrationTest";
    public const string TrainerToken = "personal-ultra-integration-trainer";
    public const string StudentToken = "personal-ultra-integration-student";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers.Authorization.ToString();
        var isTrainer = string.Equals(token, $"Bearer {TrainerToken}", StringComparison.Ordinal);
        var isStudent = string.Equals(token, $"Bearer {StudentToken}", StringComparison.Ordinal);
        if (!isTrainer && !isStudent) return Task.FromResult(AuthenticateResult.NoResult());
        var id = isTrainer ? DemoIds.TrainerId : DemoIds.StudentId;
        var identity = new ClaimsIdentity([
            new Claim(ClerkAuthenticationExtensions.ActorIdClaim, id.ToString()), new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("subject", isTrainer ? "trainer" : "student"), new Claim("actor", isTrainer ? "Trainer" : "Student")
        ], TestScheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), TestScheme)));
    }
}
