namespace PersonalUltra.StudentApi.Contracts;

public sealed record ErrorResponse(string Code, string Message, object? Details, string TraceId);
public sealed record InviteResolutionResponse(string TrainerName, string? Email, DateTimeOffset ExpiresAt);
public sealed record StudentEmailLoginRequest(string? Email);
public sealed record StudentEmailLoginResponse(string AccessToken, string TokenType, Guid StudentId, string FirstName, string LastName, string Email, string? Phone, Guid TrainerId);
public sealed record AcceptInviteRequest(string FirstName, string? LastName, string? Email, string? Phone);
public sealed record StudentProfileResponse(string FirstName, string LastName, string? Email, string? Phone, string? PreferredName);
public sealed record UpdateStudentProfileRequest(string? PreferredName);
public sealed record InviteAcceptanceResponse(string AccessToken, string TokenType, Guid StudentId, string FirstName, string LastName, string Email, string Phone, Guid TrainerId);
public sealed record ActiveTrainerMessageResponse(Guid Id, string Message, DateTimeOffset StartsAt, DateTimeOffset? ExpiresAt);
public sealed record SaveAnamnesisRequest(string Goal, string ExperienceLevel, int TrainingDaysPerWeek, int SessionDurationMinutes, string TrainingLocation, string EquipmentNotes, decimal HeightCm, decimal WeightKg, string HealthConditions, string MovementRestrictions, string CurrentPainDescription, string NutritionPreferences, string NutritionRestrictions);
public sealed record AnamnesisResponse(string Goal, string ExperienceLevel, int TrainingDaysPerWeek, int SessionDurationMinutes, string TrainingLocation, string EquipmentNotes, decimal HeightCm, decimal WeightKg, string HealthConditions, string MovementRestrictions, string CurrentPainDescription, string NutritionPreferences, string NutritionRestrictions, bool IsCompleted);
