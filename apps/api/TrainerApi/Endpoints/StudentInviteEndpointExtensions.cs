using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using PersonalUltra.Domain;
using PersonalUltra.Infrastructure;
using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

public static class StudentInviteEndpointExtensions
{
    public static void MapStudentInviteApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/student-invites").RequireAuthorization();

        api.MapPost("/", async (CreateStudentInviteRequest request, PersonalUltraDbContext db, ClaimsPrincipal user, TimeProvider clock, IConfiguration configuration, HttpContext context, CancellationToken cancellationToken) =>
        {
            var email = NormalizeEmail(request.Email);
            if (request.Email is not null && email is null)
                return context.ApiError("VALIDATION_ERROR", "Informe um e-mail válido ou deixe o campo em branco.", StatusCodes.Status400BadRequest);

            var now = clock.GetUtcNow();
            var invite = new StudentInvite
            {
                Id = Guid.NewGuid(),
                TrainerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
                Email = email,
                CreatedAt = now,
                ExpiresAt = now.AddDays(7),
            };
            db.StudentInvites.Add(invite);
            await db.SaveChangesAsync(cancellationToken);

            var linkBase = (configuration["StudentInvite:LinkBaseUrl"] ?? "personalultra://invite").TrimEnd('/');
            return Results.Created($"/api/v1/student-invites/{invite.Id}", new StudentInviteResponse(
                invite.Id, invite.Token, $"{linkBase}/{invite.Token}", invite.Email, invite.ExpiresAt));
        });
    }

    private static string? NormalizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input is null ? null : "";
        var email = input.Trim().ToLowerInvariant();
        if (email.Length > 320) return null;
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ? email : null;
        }
        catch (FormatException) { return null; }
    }
}
