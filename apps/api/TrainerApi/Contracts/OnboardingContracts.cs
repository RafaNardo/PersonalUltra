namespace PersonalUltra.TrainerApi.Contracts;

public sealed record CompleteTrainerOnboardingRequest(string? Name, string? DisplayName);
public sealed record TrainerBootstrapResponse(bool IsOnboarded, Guid? TrainerId, string? Name, string? DisplayName);
