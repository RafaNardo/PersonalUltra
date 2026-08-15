namespace PersonalUltra.ExerciseCatalogFactory.Domain;

public sealed record FactoryRun(
    string SchemaVersion,
    string RunId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Status,
    bool DryRun,
    int ResumeCount,
    SourceArtifact Source);

public sealed record SourceArtifact(
    string FileName,
    string OriginalAbsolutePath,
    string StoredRelativePath,
    string Sha256,
    long Length);
