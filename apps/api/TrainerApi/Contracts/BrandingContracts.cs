namespace PersonalUltra.TrainerApi.Contracts;
public sealed record BrandingResponse(string DisplayName, string PrimaryColor, string? LogoUrl);
public sealed record BrandingRequest(string DisplayName, string PrimaryColor, string? LogoUrl);
