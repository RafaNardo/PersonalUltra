using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class StudentOnboardingEndpointExtensions
{
    public static void MapStudentOnboardingApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/auth").RequireAuthorization(ClerkAuthenticationExtensions.SessionPolicy);
        api.MapGet("/bootstrap", async (ClaimsPrincipal user, PersonalUltraDbContext db, CancellationToken ct) =>
        {
            var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(item => item.ClerkUserId == user.FindFirstValue("sub"), ct);
            return Results.Ok(new StudentBootstrapResponse(student is not null, student?.Id));
        });
        api.MapPost("/claim-invite", async (ClaimStudentInviteRequest request, ClaimsPrincipal user, PersonalUltraDbContext db, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            var subject = user.FindFirstValue("sub")!;
            var email = user.FindFirstValue("email")?.Trim().ToLowerInvariant();
            var code = new string((request.Code ?? "").Where(char.IsDigit).ToArray());
            var firstName = Text(request.FirstName, 100); var lastName = Text(request.LastName, 100) ?? "";
            if (email is null || code.Length != 6 || firstName is null) return ApiEndpointExtensions.ApiError(context, "VALIDATION_ERROR", "Confira o código e nome informados.", 400);
            if (await db.Trainers.AnyAsync(item => item.ClerkUserId == subject, ct)) return ApiEndpointExtensions.ApiError(context, "ACTOR_ALREADY_LINKED", "Esta conta já está vinculada a um personal.", 409);
            if (await db.Students.AnyAsync(item => item.ClerkUserId == subject, ct)) return ApiEndpointExtensions.ApiError(context, "STUDENT_ALREADY_ONBOARDED", "Esta conta já concluiu o acesso como aluno.", 409);
            var invite = await db.StudentInvites.SingleOrDefaultAsync(item => item.InviteCode == code && item.AcceptedAt == null && item.ExpiresAt > clock.GetUtcNow(), ct);
            if (invite is null) return ApiEndpointExtensions.ApiError(context, "INVITE_NOT_FOUND", "Este código de convite não está disponível.", 404);
            if (invite.Email is not null && !string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase)) return ApiEndpointExtensions.ApiError(context, "INVITE_EMAIL_MISMATCH", "Use a conta criada com o e-mail informado pelo seu personal.", 403);
            if (await db.Students.AnyAsync(item => item.Email == email, ct)) return ApiEndpointExtensions.ApiError(context, "STUDENT_ALREADY_EXISTS", "Já existe um aluno com este e-mail.", 409);
            var now = clock.GetUtcNow(); var student = new Student { Id = Guid.NewGuid(), ClerkUserId = subject, FirstName = firstName, LastName = lastName, Email = email, CreatedAt = now };
            db.Students.Add(student); db.TrainerStudents.Add(new TrainerStudent { Id = Guid.NewGuid(), TrainerId = invite.TrainerId, StudentId = student.Id, StartedAt = now }); invite.AcceptedAt = now;
            await db.SaveChangesAsync(ct); return Results.Ok(new StudentBootstrapResponse(true, student.Id));
        });
    }
    private static string? Text(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
