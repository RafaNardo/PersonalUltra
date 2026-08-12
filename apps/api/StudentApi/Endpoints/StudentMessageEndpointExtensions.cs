using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class StudentMessageEndpointExtensions
{
    public static void MapStudentMessageApi(this WebApplication app)
    {
        app.MapGet("/api/v1/home/trainer-message", async (PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (user.FindFirstValue("subject") != "student" || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
                return ApiEndpointExtensions.ApiError("STUDENT_SESSION_REQUIRED", "Use uma sessão de convite válida.", StatusCodes.Status403Forbidden);

            var now = clock.GetUtcNow();
            var message = await db.TrainerMessages.AsNoTracking()
                .Where(item => item.StudentId == studentId && item.StartsAt <= now && (item.ExpiresAt == null || item.ExpiresAt > now))
                .OrderByDescending(item => item.StartsAt)
                .Select(item => new ActiveTrainerMessageResponse(item.Id, item.Message, item.StartsAt, item.ExpiresAt))
                .FirstOrDefaultAsync(cancellationToken);

            return Results.Ok(message);
        }).RequireAuthorization();
    }
}
