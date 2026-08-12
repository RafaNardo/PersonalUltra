namespace PersonalUltra.TrainerApi.Contracts;

public sealed record StudentListResponse(IReadOnlyList<TrainerStudentSummary> Students);

public sealed record TrainerStudentSummary(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? Email,
    string AnamnesisStatus,
    DateTimeOffset StartedAt);

public sealed record StudentDetailResponse(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? Email,
    string AnamnesisStatus,
    DateTimeOffset StartedAt);
