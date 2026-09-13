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

        if (app.Environment.IsEnvironment("Testing"))
        {
            app.MapPost("/api/v1/auth/student-login", () => Results.Ok(new { accessToken = IntegrationTestAuthenticationHandler.StudentToken }));
        }

    }

    internal static IResult ApiError(string code, string message, int status) => Results.Json(new ErrorResponse(code, message, null, TraceId()), statusCode: status);
    internal static IResult ApiError(HttpContext context, string code, string message, int status) => Results.Json(new ErrorResponse(code, message, null, context.TraceIdentifier), statusCode: status);
    private static string TraceId() => System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
}
