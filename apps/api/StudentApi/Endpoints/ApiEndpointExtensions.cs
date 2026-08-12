using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PersonalUltra.Infrastructure;
using PersonalUltra.StudentApi.Contracts;

namespace PersonalUltra.StudentApi.Endpoints;

public static class ApiEndpointExtensions
{
    public static void MapPersonalUltraApi(this WebApplication app)
    {
        app.MapGet("/health", async (PersonalUltraDbContext db, HttpContext context, CancellationToken cancellationToken) =>
            await db.Database.CanConnectAsync(cancellationToken)
                ? Results.Ok(new { status = "Healthy" })
                : ApiError(context, "SERVICE_UNAVAILABLE", "Database is unavailable.", StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();

        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("DemoAuth:Enabled"))
        {
            app.MapPost("/api/v1/auth/student-login", async (StudentEmailLoginRequest request, PersonalUltraDbContext db, DemoSessionTokenService sessions, HttpContext context, CancellationToken cancellationToken) =>
            {
                var email = NormalizeEmail(request.Email);
                if (email is null) return ApiError(context, "VALIDATION_ERROR", "Informe um e-mail válido.", StatusCodes.Status400BadRequest);
                var student = await db.Students.AsNoTracking().Include(item => item.Trainers).SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
                if (student is null) return ApiError(context, "STUDENT_NOT_FOUND", "Não encontramos um aluno com este e-mail. Use o código enviado pelo seu personal.", StatusCodes.Status404NotFound);
                var trainerId = student.Trainers.SingleOrDefault(link => link.EndedAt is null)?.TrainerId;
                return trainerId is null
                    ? ApiError(context, "STUDENT_NOT_FOUND", "Não encontramos um acompanhamento ativo para este aluno.", StatusCodes.Status404NotFound)
                    : Results.Ok(new StudentEmailLoginResponse(sessions.CreateStudent(student.Id), "Bearer", student.Id, student.FirstName, student.LastName, student.Email!, student.Phone, trainerId.Value));
            }).AllowAnonymous();
        }
    }

    internal static IResult ApiError(string code, string message, int status) => Results.Json(new ErrorResponse(code, message, null, TraceId()), statusCode: status);
    internal static IResult ApiError(HttpContext context, string code, string message, int status) => Results.Json(new ErrorResponse(code, message, null, context.TraceIdentifier), statusCode: status);
    private static string TraceId() => System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    private static string? NormalizeEmail(string? input)
    {
        var email = input?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320) return null;
        try { var parsed = new MailAddress(email); return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) ? email : null; }
        catch (FormatException) { return null; }
    }
}
