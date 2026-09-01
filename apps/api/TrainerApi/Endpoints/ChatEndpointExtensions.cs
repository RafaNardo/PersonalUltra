using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class ChatEndpointExtensions
{
    public static void MapChatApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/students").RequireAuthorization();
        api.MapGet("/{studentId:guid}/chat", async (Guid studentId, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await Owns(db, trainerId, studentId, ct)) return context.ApiError("STUDENT_NOT_FOUND", "O aluno não foi encontrado no seu acompanhamento.", 404);
            var messages = await db.ChatMessages.AsNoTracking().Where(x => x.StudentId == studentId && x.TrainerId == trainerId).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new TrainerChatMessage(x.Id, x.StudentId, x.Sender.ToString(), x.Content, x.CreatedAt)).ToListAsync(ct);
            return Results.Ok(messages);
        });
        api.MapPost("/{studentId:guid}/chat", async (Guid studentId, SendChatMessageRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, HttpContext context, TimeProvider clock, CancellationToken ct) =>
        {
            var trainerId = TrainerId(user);
            if (!await Owns(db, trainerId, studentId, ct)) return context.ApiError("STUDENT_NOT_FOUND", "O aluno não foi encontrado no seu acompanhamento.", 404);
            var content = request.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000) return context.ApiError("VALIDATION_ERROR", "A mensagem deve ter entre 1 e 1000 caracteres.", 400);
            var message = new ChatMessage { Id = Guid.NewGuid(), StudentId = studentId, TrainerId = trainerId, Sender = ChatMessageSender.Trainer, Content = content, CreatedAt = clock.GetUtcNow() };
            db.ChatMessages.Add(message); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/students/{studentId}/chat/{message.Id}", new TrainerChatMessage(message.Id, studentId, message.Sender.ToString(), message.Content, message.CreatedAt));
        });
    }
    private static Guid TrainerId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static Task<bool> Owns(PersonalUltraDbContext db, Guid trainerId, Guid studentId, CancellationToken ct) => db.TrainerStudents.AnyAsync(x => x.TrainerId == trainerId && x.StudentId == studentId && x.EndedAt == null, ct);
}
