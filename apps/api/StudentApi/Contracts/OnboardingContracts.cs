namespace PersonalUltra.StudentApi.Contracts;

public sealed record StudentBootstrapResponse(bool IsOnboarded, Guid? StudentId);
public sealed record InvitePreviewResponse(string TrainerName);
public sealed record ClaimStudentInviteRequest(string? Code, string? FirstName, string? LastName);
