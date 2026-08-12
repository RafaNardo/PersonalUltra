namespace PersonalUltra.TrainerApi.Contracts;

public sealed record CreateStudentInviteRequest(string? Email);

public sealed record StudentInviteResponse(
    Guid Id,
    string Token,
    string InviteUrl,
    string? Email,
    DateTimeOffset ExpiresAt);
