namespace PersonalUltra.TrainerApi.Contracts;

public sealed record CreateTrainerMessageRequest(string Message, DateTimeOffset? StartsAt, DateTimeOffset? ExpiresAt);

public sealed record TrainerMessageResponse(
    Guid Id,
    Guid StudentId,
    string Message,
    DateTimeOffset StartsAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
