namespace PersonalUltra.TrainerApi.Contracts;

public sealed record CreateStudentInviteRequest;

public sealed record StudentInviteResponse(
    Guid Id,
    string Token,
    string InviteCode,
    string InviteUrl,
    DateTimeOffset ExpiresAt);
