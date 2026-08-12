using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class StudentInviteEndpointExtensions
{
    public static void MapStudentInviteApi(this WebApplication app)
    {
        app.MapGet("/api/v1/invite/{token}", async (string token, PersonalUltraDbContext db, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var invite = await db.StudentInvites.AsNoTracking().Include(x => x.Trainer)
                .SingleOrDefaultAsync(x => x.Token == token && x.AcceptedAt == null && x.ExpiresAt > clock.GetUtcNow(), cancellationToken);
            return invite is null
                ? ApiEndpointExtensions.ApiError("INVITE_NOT_FOUND", "Este convite não está disponível.", StatusCodes.Status404NotFound)
                : Results.Ok(new InviteResolutionResponse(invite.Trainer.Name, invite.Email, invite.ExpiresAt));
        }).AllowAnonymous();
    }
}
