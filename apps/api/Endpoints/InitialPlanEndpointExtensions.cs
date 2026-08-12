using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Api.Application.Plans;
using PersonalUltra.Api.Infrastructure;

namespace PersonalUltra.Api.Endpoints;

public static class InitialPlanEndpointExtensions
{
    public static void MapInitialPlanApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/plans/initial").RequireAuthorization();

        api.MapGet("", async (PersonalUltraDbContext db, StandardPlanProvisioner provisioner, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var memberId = MemberId(user);
            if (!await HasCompletedOnboardingAsync(db, memberId, cancellationToken))
                return ApiEndpointExtensions.ApiError("ONBOARDING_INCOMPLETE", "Conclua o onboarding antes de consultar seu plano.", StatusCodes.Status409Conflict);
            return Results.Ok(await provisioner.GetAsync(memberId, cancellationToken));
        });

        api.MapPost("", async (PersonalUltraDbContext db, StandardPlanProvisioner provisioner, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var memberId = MemberId(user);
            if (!await HasCompletedOnboardingAsync(db, memberId, cancellationToken))
                return ApiEndpointExtensions.ApiError("ONBOARDING_INCOMPLETE", "Conclua o onboarding antes de preparar seu plano.", StatusCodes.Status409Conflict);

            var plan = await provisioner.ProvisionAsync(memberId, cancellationToken);
            return plan.WasAlreadyProvisioned ? Results.Ok(plan) : Results.Created($"/api/v1/plans/initial/{plan.PlanId}", plan);
        });
    }

    private static Guid MemberId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static Task<bool> HasCompletedOnboardingAsync(PersonalUltraDbContext db, Guid memberId, CancellationToken cancellationToken) =>
        db.Members.AsNoTracking().AnyAsync(x => x.Id == memberId && x.OnboardingCompletedAt != null, cancellationToken);
}
