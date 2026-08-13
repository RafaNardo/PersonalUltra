using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class TrainerSettingsEndpointExtensions
{
    private static readonly PrescriptionSettingsResponse Defaults = new(3, 8, 12, 60, false);

    public static void MapTrainerSettingsApi(this WebApplication app)
    {
        app.MapGet("/api/v1/settings/prescription", async (
            PersonalUltraDbContext db,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var settings = await db.TrainerPrescriptionSettings
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.TrainerId == trainerId, cancellationToken);

            return Results.Ok(settings is null
                ? Defaults
                : ToResponse(settings));
        }).RequireAuthorization();

        app.MapPut("/api/v1/settings/prescription", async (
            UpdatePrescriptionSettingsRequest request,
            PersonalUltraDbContext db,
            ClaimsPrincipal user,
            HttpContext context,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            if (request.Sets is < 1 or > 20 ||
                request.RepetitionsMin is < 1 or > 100 ||
                request.RepetitionsMax is < 1 or > 100 ||
                request.RepetitionsMin > request.RepetitionsMax ||
                request.RestSeconds is < 0 or > 900)
            {
                return context.ApiError(
                    "VALIDATION_ERROR",
                    "Revise séries, faixa de repetições e descanso.",
                    StatusCodes.Status400BadRequest);
            }

            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var settings = await db.TrainerPrescriptionSettings
                .SingleOrDefaultAsync(x => x.TrainerId == trainerId, cancellationToken);

            if (settings is null)
            {
                settings = new TrainerPrescriptionSettings
                {
                    Id = Guid.NewGuid(),
                    TrainerId = trainerId,
                };
                db.TrainerPrescriptionSettings.Add(settings);
            }

            settings.Sets = request.Sets;
            settings.RepetitionsMin = request.RepetitionsMin;
            settings.RepetitionsMax = request.RepetitionsMax;
            settings.RestSeconds = request.RestSeconds;
            settings.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToResponse(settings));
        }).RequireAuthorization();
    }

    private static PrescriptionSettingsResponse ToResponse(TrainerPrescriptionSettings settings) =>
        new(settings.Sets, settings.RepetitionsMin, settings.RepetitionsMax, settings.RestSeconds, true);
}
