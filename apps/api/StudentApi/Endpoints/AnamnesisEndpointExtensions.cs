using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class AnamnesisEndpointExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapAnamnesisApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/anamnesis").RequireAuthorization();
        api.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryStudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de convite válida.", StatusCodes.Status403Forbidden);
            var anamnesis = await db.Anamneses.AsNoTracking().SingleOrDefaultAsync(item => item.StudentId == studentId, cancellationToken);
            return Results.Ok(ToResponse(anamnesis));
        });

        api.MapPut("/", async (SaveAnamnesisRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (!TryStudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de convite válida.", StatusCodes.Status403Forbidden);
            var answers = ToAnswers(request);
            if (ValidationError(answers) is { } error) return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", error, StatusCodes.Status400BadRequest);
            var now = clock.GetUtcNow();
            var anamnesis = await db.Anamneses.SingleOrDefaultAsync(item => item.StudentId == studentId, cancellationToken);
            if (anamnesis is null)
            {
                anamnesis = new Anamnesis { Id = Guid.NewGuid(), StudentId = studentId, CreatedAt = now };
                db.Anamneses.Add(anamnesis);
            }
            if (anamnesis.CompletedAt is not null) return ApiEndpointExtensions.ApiError("ANAMNESIS_ALREADY_COMPLETED", "A anamnese já foi concluída.", StatusCodes.Status409Conflict);
            anamnesis.AnswersJson = JsonSerializer.Serialize(answers, JsonOptions);
            anamnesis.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(anamnesis));
        });

        api.MapPost("/complete", async (PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            if (!TryStudentId(user, out var studentId)) return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de convite válida.", StatusCodes.Status403Forbidden);
            var anamnesis = await db.Anamneses.SingleOrDefaultAsync(item => item.StudentId == studentId, cancellationToken);
            if (anamnesis is null) return ApiEndpointExtensions.ApiError("ANAMNESIS_INCOMPLETE", "Preencha a anamnese antes de concluir.", StatusCodes.Status409Conflict);
            if (anamnesis.CompletedAt is not null) return Results.Ok(ToResponse(anamnesis));
            var answers = JsonSerializer.Deserialize<AnamnesisAnswers>(anamnesis.AnswersJson, JsonOptions);
            if (answers is null || ValidationError(answers) is not null) return ApiEndpointExtensions.ApiError("ANAMNESIS_INCOMPLETE", "Preencha todos os campos obrigatórios antes de concluir.", StatusCodes.Status409Conflict);
            anamnesis.CompletedAt = clock.GetUtcNow();
            anamnesis.UpdatedAt = anamnesis.CompletedAt.Value;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(anamnesis));
        });
    }

    private static bool TryStudentId(ClaimsPrincipal user, out Guid studentId)
    {
        studentId = Guid.Empty;
        return user.FindFirstValue("subject") == "student" && Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out studentId);
    }
    private static AnamnesisAnswers ToAnswers(SaveAnamnesisRequest request) => new(request.Goal?.Trim() ?? "", request.ExperienceLevel?.Trim() ?? "", request.TrainingDaysPerWeek, request.SessionDurationMinutes, request.TrainingLocation?.Trim() ?? "", request.EquipmentNotes?.Trim() ?? "", request.HeightCm, request.WeightKg, request.HealthConditions?.Trim() ?? "", request.MovementRestrictions?.Trim() ?? "", request.CurrentPainDescription?.Trim() ?? "", request.NutritionPreferences?.Trim() ?? "", request.NutritionRestrictions?.Trim() ?? "");
    private static string? ValidationError(AnamnesisAnswers answers) => string.IsNullOrWhiteSpace(answers.Goal) || string.IsNullOrWhiteSpace(answers.ExperienceLevel) || string.IsNullOrWhiteSpace(answers.TrainingLocation) || string.IsNullOrWhiteSpace(answers.EquipmentNotes) || string.IsNullOrWhiteSpace(answers.HealthConditions) || string.IsNullOrWhiteSpace(answers.MovementRestrictions) || string.IsNullOrWhiteSpace(answers.CurrentPainDescription) || string.IsNullOrWhiteSpace(answers.NutritionPreferences) || string.IsNullOrWhiteSpace(answers.NutritionRestrictions) ? "Preencha todos os campos obrigatórios." : answers.TrainingDaysPerWeek is < 1 or > 7 ? "Escolha entre 1 e 7 dias de treino." : answers.SessionDurationMinutes is < 15 or > 180 ? "Informe uma duração entre 15 e 180 minutos." : answers.HeightCm is < 80 or > 260 ? "Informe uma altura entre 80 e 260 cm." : answers.WeightKg is < 25 or > 400 ? "Informe um peso entre 25 e 400 kg." : null;
    private static AnamnesisResponse ToResponse(Anamnesis? anamnesis)
    {
        var answers = anamnesis is null ? new AnamnesisAnswers("", "", 0, 0, "", "", 0, 0, "", "", "", "", "") : JsonSerializer.Deserialize<AnamnesisAnswers>(anamnesis.AnswersJson, JsonOptions) ?? new AnamnesisAnswers("", "", 0, 0, "", "", 0, 0, "", "", "", "", "");
        return new AnamnesisResponse(answers.Goal, answers.ExperienceLevel, answers.TrainingDaysPerWeek, answers.SessionDurationMinutes, answers.TrainingLocation, answers.EquipmentNotes, answers.HeightCm, answers.WeightKg, answers.HealthConditions, answers.MovementRestrictions, answers.CurrentPainDescription, answers.NutritionPreferences, answers.NutritionRestrictions, anamnesis?.CompletedAt is not null);
    }
}
