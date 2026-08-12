using PersonalUltra.TrainerApi.Contracts;

namespace PersonalUltra.TrainerApi.Endpoints;

internal static class ApiErrorExtensions
{
    public static IResult ApiError(this HttpContext context, string code, string message, int statusCode) =>
        Results.Json(new ErrorResponse(code, message, null, context.TraceIdentifier), statusCode: statusCode);
}
