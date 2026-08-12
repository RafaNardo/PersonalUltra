using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
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

        app.MapPost("/api/v1/invite/{token}/accept", async (string token, AcceptInviteRequest request, PersonalUltraDbContext db, DemoSessionTokenService sessions, TimeProvider clock, HttpContext context, CancellationToken cancellationToken) =>
        {
            var invite = await db.StudentInvites.SingleOrDefaultAsync(x => x.Token == token && x.AcceptedAt == null && x.ExpiresAt > clock.GetUtcNow(), cancellationToken);
            if (invite is null)
                return ApiEndpointExtensions.ApiError("INVITE_NOT_FOUND", "Este convite não está disponível.", StatusCodes.Status404NotFound);

            var firstName = Value(request.FirstName, 100);
            var lastName = Value(request.LastName, 100) ?? "";
            var suppliedEmail = NormalizeEmail(request.Email);
            var email = invite.Email ?? suppliedEmail;
            if (firstName is null || email is null || (invite.Email is not null && suppliedEmail is not null && !string.Equals(invite.Email, suppliedEmail, StringComparison.OrdinalIgnoreCase)))
                return ApiEndpointExtensions.ApiError("VALIDATION_ERROR", "Informe seu nome e o e-mail vinculado ao convite.", StatusCodes.Status400BadRequest);

            if (await db.Students.AnyAsync(student => student.Email == email, cancellationToken))
                return ApiEndpointExtensions.ApiError("STUDENT_ALREADY_EXISTS", "Já existe um aluno com este e-mail.", StatusCodes.Status409Conflict);

            var now = clock.GetUtcNow();
            var student = new Student { Id = Guid.NewGuid(), FirstName = firstName, LastName = lastName, Email = email, CreatedAt = now };
            db.Students.Add(student);
            db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = invite.TrainerId, StudentId = student.Id, StartedAt = now });
            invite.AcceptedAt = now;
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new InviteAcceptanceResponse(sessions.CreateStudent(student.Id), "Bearer", student.Id, student.FirstName, student.LastName, email, invite.TrainerId));
        }).AllowAnonymous();
    }

    private static string? Value(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed[..Math.Min(trimmed.Length, maxLength)];
    }

    private static string? NormalizeEmail(string? input)
    {
        var email = Value(input, 320)?.ToLowerInvariant();
        if (email is null) return null;
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ? email : null;
        }
        catch (FormatException) { return null; }
    }
}
