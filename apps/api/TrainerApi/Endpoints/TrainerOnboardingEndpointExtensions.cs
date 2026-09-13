using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class TrainerOnboardingEndpointExtensions
{
    public static void MapTrainerOnboardingApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/auth").RequireAuthorization(ClerkAuthenticationExtensions.SessionPolicy);
        api.MapGet("/bootstrap", async (ClaimsPrincipal user, PersonalUltraDbContext db, CancellationToken ct) =>
        {
            var subject = user.FindFirstValue("sub")!;
            var trainer = await db.Trainers.AsNoTracking().Include(item => item.Branding).SingleOrDefaultAsync(item => item.ClerkUserId == subject, ct);
            return Results.Ok(trainer is null
                ? new TrainerBootstrapResponse(false, null, null, null)
                : new TrainerBootstrapResponse(true, trainer.Id, trainer.Name, trainer.Branding?.DisplayName));
        });
        api.MapPost("/onboarding", async (CompleteTrainerOnboardingRequest request, ClaimsPrincipal user, PersonalUltraDbContext db, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var subject = user.FindFirstValue("sub")!;
            var name = Text(request.Name, 200);
            var displayName = Text(request.DisplayName, 200);
            if (name is null || displayName is null) return context.ApiError("VALIDATION_ERROR", "Informe seu nome profissional e nome de exibição.", 400);
            if (await db.Students.AnyAsync(item => item.ClerkUserId == subject, ct)) return context.ApiError("ACTOR_ALREADY_LINKED", "Esta conta já está vinculada a um aluno.", 409);
            var existing = await db.Trainers.Include(item => item.Branding).SingleOrDefaultAsync(item => item.ClerkUserId == subject, ct);
            if (existing is not null) return Results.Ok(new TrainerBootstrapResponse(true, existing.Id, existing.Name, existing.Branding?.DisplayName));
            var trainer = new Trainer { Id = Guid.NewGuid(), ClerkUserId = subject, Name = name, CreatedAt = clock.GetUtcNow() };
            db.Trainers.Add(trainer);
            db.TrainerBrandings.Add(new TrainerBranding { Id = Guid.NewGuid(), TrainerId = trainer.Id, DisplayName = displayName, PrimaryColor = "#FF6A13" });
            await db.SaveChangesAsync(ct);
            return Results.Created("/api/v1/auth/bootstrap", new TrainerBootstrapResponse(true, trainer.Id, trainer.Name, displayName));
        });
    }

    private static string? Text(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
