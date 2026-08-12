namespace PersonalUltra.TrainerApi.Contracts;

public sealed record ErrorResponse(string Code, string Message, object? Details, string TraceId);
