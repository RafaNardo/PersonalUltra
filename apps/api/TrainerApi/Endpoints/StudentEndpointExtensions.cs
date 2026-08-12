using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class StudentEndpointExtensions
{
    public static void MapStudentApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/students").RequireAuthorization();

        api.MapGet("/", async (PersonalUltraDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var students = await db.TrainerStudents.AsNoTracking()
                .Where(link => link.TrainerId == trainerId && link.EndedAt == null)
                .OrderBy(link => link.Student.FirstName).ThenBy(link => link.Student.LastName)
                .Select(link => new TrainerStudentSummary(
                    link.StudentId,
                    link.Student.FirstName,
                    link.Student.LastName,
                    link.Student.Email,
                    link.Student.Phone,
                    link.Student.Anamnesis == null
                        ? "NotStarted"
                        : link.Student.Anamnesis.CompletedAt == null ? "InProgress" : "Completed",
                    link.StartedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new StudentListResponse(students));
        });

        api.MapGet("/{studentId:guid}", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var student = await db.TrainerStudents.AsNoTracking()
                .Where(link => link.TrainerId == trainerId && link.StudentId == studentId && link.EndedAt == null)
                .Select(link => new StudentDetailResponse(
                    link.StudentId,
                    link.Student.FirstName,
                    link.Student.LastName,
                    link.Student.Email,
                    link.Student.Phone,
                    link.Student.Anamnesis == null
                        ? "NotStarted"
                        : link.Student.Anamnesis.CompletedAt == null ? "InProgress" : "Completed",
                    link.StartedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return student is null
                ? context.ApiError("STUDENT_NOT_FOUND", "O aluno não foi encontrado no seu acompanhamento.", StatusCodes.Status404NotFound)
                : Results.Ok(student);
        });

        api.MapPost("/{studentId:guid}/messages", async (Guid studentId, CreateTrainerMessageRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var message = request.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
                return context.ApiError("VALIDATION_ERROR", "A mensagem deve ter entre 1 e 1000 caracteres.", StatusCodes.Status400BadRequest);

            var ownsStudent = await db.TrainerStudents.AnyAsync(link => link.TrainerId == trainerId && link.StudentId == studentId && link.EndedAt == null, cancellationToken);
            if (!ownsStudent)
                return context.ApiError("STUDENT_NOT_FOUND", "O aluno não foi encontrado no seu acompanhamento.", StatusCodes.Status404NotFound);

            var now = clock.GetUtcNow();
            var startsAt = request.StartsAt ?? now;
            if (request.ExpiresAt is not null && request.ExpiresAt <= startsAt)
                return context.ApiError("VALIDATION_ERROR", "O fim da mensagem deve ser posterior ao início.", StatusCodes.Status400BadRequest);

            var trainerMessage = new PersonalUltra.Domain.TrainerMessage
            {
                Id = Guid.NewGuid(), TrainerId = trainerId, StudentId = studentId, Message = message,
                StartsAt = startsAt, ExpiresAt = request.ExpiresAt, CreatedAt = now,
            };
            db.TrainerMessages.Add(trainerMessage);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/students/{studentId}/messages/{trainerMessage.Id}", new TrainerMessageResponse(
                trainerMessage.Id, trainerMessage.StudentId, trainerMessage.Message, trainerMessage.StartsAt, trainerMessage.ExpiresAt, trainerMessage.CreatedAt));
        });

        api.MapGet("/{studentId:guid}/anamnesis", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken cancellationToken) =>
        {
            var trainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var anamnesis = await db.Anamneses.AsNoTracking()
                .Where(item => item.StudentId == studentId && item.CompletedAt != null && item.Student.Trainers.Any(link => link.TrainerId == trainerId && link.EndedAt == null))
                .SingleOrDefaultAsync(cancellationToken);
            if (anamnesis is null) return context.ApiError("ANAMNESIS_NOT_FOUND", "A anamnese deste aluno ainda não foi concluída.", StatusCodes.Status404NotFound);
            var answers = JsonSerializer.Deserialize<PersonalUltra.Domain.AnamnesisAnswers>(anamnesis.AnswersJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (answers is null) return context.ApiError("ANAMNESIS_NOT_FOUND", "A anamnese deste aluno ainda não está disponível.", StatusCodes.Status404NotFound);
            return Results.Ok(new TrainerAnamnesisResponse(answers.Goal, answers.ExperienceLevel, answers.TrainingDaysPerWeek, answers.SessionDurationMinutes, answers.TrainingLocation, answers.EquipmentNotes, answers.HeightCm, answers.WeightKg, answers.HealthConditions, answers.MovementRestrictions, answers.CurrentPainDescription, answers.NutritionPreferences, answers.NutritionRestrictions, anamnesis.CompletedAt!.Value));
        });
    }
}
