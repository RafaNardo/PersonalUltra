namespace PersonalUltra.TrainerApi.Contracts;

public sealed record PrescriptionSettingsResponse(
    int Sets,
    int RepetitionsMin,
    int RepetitionsMax,
    int RestSeconds,
    bool IsCustomized);

public sealed record UpdatePrescriptionSettingsRequest(
    int Sets,
    int RepetitionsMin,
    int RepetitionsMax,
    int RestSeconds);
