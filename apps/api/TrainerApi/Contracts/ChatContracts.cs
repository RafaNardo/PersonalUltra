namespace PersonalUltra.TrainerApi.Contracts;

public sealed record TrainerChatMessage(Guid Id, Guid StudentId, string Sender, string Content, DateTimeOffset CreatedAt);
public sealed record SendChatMessageRequest(string? Content);
