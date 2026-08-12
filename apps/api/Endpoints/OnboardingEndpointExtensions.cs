using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Api.Contracts;
using PersonalUltra.Api.Domain;
using PersonalUltra.Api.Infrastructure;

namespace PersonalUltra.Api.Endpoints;

public static class OnboardingEndpointExtensions
{
    public static void MapOnboardingApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/onboarding").RequireAuthorization();

        api.MapGet("/profile", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var member = await Member(db, user, cancellationToken);
            return Results.Ok(ToDto(member, member.Profile));
        });

        api.MapPut("/profile", async (SaveOnboardingProfileRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (request.CurrentStep is < 1 or > 8)
                return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "A etapa do onboarding é inválida.", StatusCodes.Status400BadRequest);

            var member = await Member(db, user, cancellationToken);
            if (member.OnboardingCompletedAt is not null)
                return ApiEndpointExtensions.ApiError("ONBOARDING_ALREADY_COMPLETED", "O onboarding já foi concluído.", StatusCodes.Status409Conflict);

            var error = ValidateDraft(request);
            if (error is not null)
                return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", error, StatusCodes.Status400BadRequest);

            member.FirstName = Value(request.FirstName, member.FirstName, 100);
            member.LastName = Value(request.LastName, member.LastName, 100);
            var profile = member.Profile ?? new MemberProfile { Id = Guid.NewGuid(), MemberId = member.Id, CreatedAt = clock.GetUtcNow() };
            profile.Goal = Value(request.Goal, profile.Goal, 100);
            profile.ExperienceLevel = Value(request.ExperienceLevel, profile.ExperienceLevel, 50);
            profile.TrainingDaysPerWeek = request.TrainingDaysPerWeek ?? profile.TrainingDaysPerWeek;
            profile.SessionDurationMinutes = request.SessionDurationMinutes ?? profile.SessionDurationMinutes;
            profile.TrainingLocation = Value(request.TrainingLocation, profile.TrainingLocation, 100);
            profile.EquipmentNotes = Value(request.EquipmentNotes, profile.EquipmentNotes, 1200);
            profile.HeightCm = request.HeightCm ?? profile.HeightCm;
            profile.WeightKg = request.WeightKg ?? profile.WeightKg;
            profile.HealthConditions = Value(request.HealthConditions, profile.HealthConditions, 1200);
            profile.MovementRestrictions = Value(request.MovementRestrictions, profile.MovementRestrictions, 1200);
            profile.CurrentPainDescription = Value(request.CurrentPainDescription, profile.CurrentPainDescription, 1200);
            profile.NutritionPreferences = Value(request.NutritionPreferences, profile.NutritionPreferences, 1200);
            profile.NutritionRestrictions = Value(request.NutritionRestrictions, profile.NutritionRestrictions, 1200);
            profile.CurrentStep = Math.Max(profile.CurrentStep, request.CurrentStep);
            profile.UpdatedAt = clock.GetUtcNow();
            if (member.Profile is null) db.MemberProfiles.Add(profile);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToDto(member, profile));
        });

        api.MapPost("/complete", async (PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var member = await Member(db, user, cancellationToken);
            if (member.OnboardingCompletedAt is not null) return Results.Ok(ToDto(member, member.Profile));
            if (member.Profile is null || !IsComplete(member, member.Profile))
                return ApiEndpointExtensions.ApiError("ONBOARDING_INCOMPLETE", "Preencha todas as etapas antes de concluir o onboarding.", StatusCodes.Status409Conflict);

            member.OnboardingCompletedAt = clock.GetUtcNow();
            member.Profile.CurrentStep = 8;
            member.Profile.UpdatedAt = member.OnboardingCompletedAt.Value;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToDto(member, member.Profile));
        });
    }

    private static async Task<Member> Member(PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        await db.Members.Include(x => x.Profile).SingleAsync(x => x.Id == Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken);

    private static OnboardingProfileDto ToDto(Member member, MemberProfile? profile) => new(
        member.FirstName, member.LastName, profile?.Goal ?? "", profile?.ExperienceLevel ?? "", profile?.TrainingDaysPerWeek ?? 0,
        profile?.SessionDurationMinutes ?? 0, profile?.TrainingLocation ?? "", profile?.EquipmentNotes ?? "", profile?.HeightCm ?? 0,
        profile?.WeightKg ?? 0, profile?.HealthConditions ?? "", profile?.MovementRestrictions ?? "", profile?.CurrentPainDescription ?? "",
        profile?.NutritionPreferences ?? "", profile?.NutritionRestrictions ?? "", profile?.CurrentStep ?? 1, member.OnboardingCompletedAt is not null);

    private static string? ValidateDraft(SaveOnboardingProfileRequest request)
    {
        if (request.TrainingDaysPerWeek is < 1 or > 7) return "Escolha entre 1 e 7 dias de treino por semana.";
        if (request.SessionDurationMinutes is < 15 or > 180) return "Informe uma duração entre 15 e 180 minutos.";
        if (request.HeightCm is < 80 or > 260) return "Informe uma altura entre 80 e 260 cm.";
        if (request.WeightKg is < 25 or > 400) return "Informe um peso entre 25 e 400 kg.";
        return null;
    }

    private static bool IsComplete(Member member, MemberProfile profile) =>
        !string.IsNullOrWhiteSpace(member.FirstName) && !string.IsNullOrWhiteSpace(profile.Goal) && !string.IsNullOrWhiteSpace(profile.ExperienceLevel)
        && profile.TrainingDaysPerWeek is >= 1 and <= 7 && profile.SessionDurationMinutes is >= 15 and <= 180
        && !string.IsNullOrWhiteSpace(profile.TrainingLocation) && !string.IsNullOrWhiteSpace(profile.EquipmentNotes)
        && profile.HeightCm is >= 80 and <= 260 && profile.WeightKg is >= 25 and <= 400
        && !string.IsNullOrWhiteSpace(profile.HealthConditions) && !string.IsNullOrWhiteSpace(profile.MovementRestrictions)
        && !string.IsNullOrWhiteSpace(profile.CurrentPainDescription) && !string.IsNullOrWhiteSpace(profile.NutritionPreferences)
        && !string.IsNullOrWhiteSpace(profile.NutritionRestrictions);

    private static string Value(string? incoming, string? current, int maxLength) =>
        incoming is null ? current ?? "" : incoming.Trim()[..Math.Min(incoming.Trim().Length, maxLength)];
}
