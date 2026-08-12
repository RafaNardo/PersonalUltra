namespace PersonalUltra.TrainerApi.Contracts;

public sealed record DashboardResponse(
    string TrainerName,
    int ActiveStudents,
    int PendingAnamneses,
    int CompletedAnamneses,
    IReadOnlyList<DashboardStudentSummary> RecentStudents,
    IReadOnlyList<DashboardActivity> RecentActivities);

public sealed record DashboardStudentSummary(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? Email,
    string AnamnesisStatus,
    DateTimeOffset StartedAt);

public sealed record DashboardActivity(
    Guid StudentId,
    string StudentName,
    string Type,
    DateTimeOffset OccurredAt);
