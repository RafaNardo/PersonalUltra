namespace PersonalUltra.ExerciseCatalogFactory.Domain;

public sealed record CatalogInputDocument(int SchemaVersion, string Source, IReadOnlyList<CatalogInputItem?> Items);

public sealed record CatalogInputItem(
    string? ExternalKey,
    string Name,
    IReadOnlyList<string>? Aliases,
    string? PrimaryMuscleGroup,
    string? Equipment,
    string? InstructionsHint,
    string? VisualHint,
    IReadOnlyList<string>? LockedFields);

public sealed record ContractDiagnostic(string File, int? Line, string Field, string Message);

public sealed record OutputPackageContract(
    int SchemaVersion,
    string RunId,
    string Status,
    IReadOnlyList<ArtifactReference> Artifacts,
    string ManifestHash);
