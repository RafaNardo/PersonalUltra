namespace PersonalUltra.StudentApi.Contracts;

public sealed record StudentChatResponse(string? TrainerPhone, IReadOnlyList<StudentChatMessage> Messages);
public sealed record StudentChatMessage(Guid Id, string Sender, string Content, DateTimeOffset CreatedAt);
public sealed record SendChatMessageRequest(string? Content);
