namespace PersonalUltra.TrainerApi.Contracts;

public sealed record DashboardResponse(
    string TrainerName,
    int ActiveStudents,
    int PendingAnamneses,
    int CompletedAnamneses,
    IReadOnlyList<DashboardStudentSummary> RecentStudents);

public sealed record DashboardStudentSummary(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? Email,
    string AnamnesisStatus,
    DateTimeOffset StartedAt);
