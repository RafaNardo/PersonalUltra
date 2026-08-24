namespace PersonalUltra.ExerciseCatalogFactory.Domain;

public sealed record FactoryRun(
    string SchemaVersion,
    string RunId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Status,
    bool DryRun,
    int ResumeCount,
    SourceArtifact Source,
    PipelineVersions? Versions = null,
    IReadOnlyList<ManifestItem>? Items = null,
    IReadOnlyList<ProviderAttempt>? Attempts = null,
    UsageSummary? Usage = null,
    IReadOnlyDictionary<string, string>? StageHashes = null,
    IReadOnlyList<ArtifactReference>? Outputs = null);

public sealed record SourceArtifact(
    string FileName,
    string OriginalAbsolutePath,
    string StoredRelativePath,
    string Sha256,
    long Length);

public sealed record PipelineVersions(
    string PipelineVersion,
    string TaxonomyVersion,
    string MetadataPromptVersion,
    string ImagePromptVersion,
    string StyleVersion,
    string TargetProfileVersion);

public sealed record ManifestItem(
    string ExternalKey,
    string State,
    ItemSource Source,
    ItemStageHashes Hashes,
    IReadOnlyList<ArtifactReference>? Artifacts = null,
    IReadOnlyList<ReviewDecision>? Reviews = null);

public sealed record ItemSource(string File, int Row, string SourceHash);

public sealed record ItemStageHashes(
    string Source,
    string? NormalizationInput = null,
    string? MetadataInput = null,
    string? ImageInput = null,
    string? ExportInput = null);

public sealed record ArtifactReference(string Stage, string RelativePath, string Sha256, long Length);

public sealed record ReviewDecision(
    string ItemKey,
    string Stage,
    string Decision,
    string ReasonCode,
    string? Notes,
    string Reviewer,
    DateTimeOffset ReviewedAt,
    string ArtifactHash);

public sealed record ProviderAttempt(
    string Stage,
    string ItemKey,
    string Provider,
    string Model,
    string IdempotencyKey,
    string? RequestId,
    int Attempt,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string InputHash,
    string? PromptVersion,
    string Status,
    ProviderCost Cost,
    ProviderUsage? Usage = null);

public sealed record ProviderCost(string Currency, decimal? Estimated, decimal? Observed);
public sealed record ProviderUsage(int? InputTokens, int? OutputTokens, int? Images);
public sealed record UsageSummary(string Currency, decimal EstimatedCost, decimal ObservedCost, int Attempts);

public static class ManifestStates
{
    public static readonly IReadOnlySet<string> Run = new HashSet<string>(StringComparer.Ordinal)
    {
        "imported", "processing", "needs_review", "ready", "partial", "blocked", "failed", "completed"
    };

    public static readonly IReadOnlyList<string> ItemPipeline =
    [
        "imported", "normalized", "metadata_pending", "metadata_generated", "metadata_review",
        "metadata_approved", "image_pending", "image_generated", "image_review", "approved", "exported"
    ];

    public static readonly IReadOnlySet<string> Item = new HashSet<string>(
        ItemPipeline.Concat(["needs_review", "rejected", "failed_retryable", "failed_terminal", "deprecated"]),
        StringComparer.Ordinal);
}
