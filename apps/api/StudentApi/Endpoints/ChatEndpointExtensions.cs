using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class ChatEndpointExtensions
{
    public static void MapChatApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/chat").RequireAuthorization();

        api.MapGet("", async (PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            if (!Student(user, out var studentId)) return ApiEndpointExtensions.ApiError(context, "STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var link = await db.TrainerStudents.AsNoTracking().Where(x => x.StudentId == studentId && x.EndedAt == null).Select(x => new { x.TrainerId, x.Trainer.Phone }).SingleOrDefaultAsync(ct);
            if (link is null) return ApiEndpointExtensions.ApiError(context, "TRAINER_NOT_FOUND", "Não encontramos um personal ativo para este aluno.", 404);
            var messages = await db.ChatMessages.AsNoTracking().Where(x => x.StudentId == studentId && x.TrainerId == link.TrainerId).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new StudentChatMessage(x.Id, x.Sender.ToString(), x.Content, x.CreatedAt)).ToListAsync(ct);
            return Results.Ok(new StudentChatResponse(link.Phone, messages));
        });

        api.MapPost("", async (SendChatMessageRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, TimeProvider clock, CancellationToken ct) =>
        {
            if (!Student(user, out var studentId)) return ApiEndpointExtensions.ApiError(context, "STUDENT_SESSION_REQUIRED", "Use uma sessão de aluno válida.", 403);
            var content = request.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000) return ApiEndpointExtensions.ApiError(context, "VALIDATION_ERROR", "A mensagem deve ter entre 1 e 1000 caracteres.", 400);
            var trainerId = await db.TrainerStudents.Where(x => x.StudentId == studentId && x.EndedAt == null).Select(x => (Guid?)x.TrainerId).SingleOrDefaultAsync(ct);
            if (trainerId is null) return ApiEndpointExtensions.ApiError(context, "TRAINER_NOT_FOUND", "Não encontramos um personal ativo para este aluno.", 404);
            var message = new ChatMessage { Id = Guid.NewGuid(), StudentId = studentId, TrainerId = trainerId.Value, Sender = ChatMessageSender.Student, Content = content, CreatedAt = clock.GetUtcNow() };
            db.ChatMessages.Add(message); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/chat/{message.Id}", new StudentChatMessage(message.Id, message.Sender.ToString(), message.Content, message.CreatedAt));
        });
    }

    private static bool Student(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && user.FindFirstValue("subject") == "student";
}
