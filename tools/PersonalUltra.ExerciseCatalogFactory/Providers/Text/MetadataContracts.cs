using System.Text.Json.Serialization;

namespace PersonalUltra.ExerciseCatalogFactory.Providers.Text;

public sealed record MetadataProposal(
    string CanonicalName,
    IReadOnlyList<string> Aliases,
    string PrimaryMuscleGroup,
    string Equipment,
    string Instructions,
    string VisualDescription,
    IReadOnlyList<string> Ambiguities,
    MetadataConfidence Confidence);

public sealed record MetadataConfidence(string PrimaryMuscleGroup, string Equipment);

public sealed record MetadataRequest(
    string ExternalKey,
    string Name,
    IReadOnlyList<string> Aliases,
    string? PrimaryMuscleGroup,
    string? Equipment,
    string? InstructionsHint,
    string? VisualHint,
    IReadOnlySet<string> LockedFields,
    string Model,
    string PromptVersion,
    string PromptText,
    decimal Temperature);

public sealed record MetadataProviderResult(
    MetadataProposal Proposal,
    string? RequestId,
    int? InputTokens,
    int? OutputTokens,
    decimal? ObservedCostUsd);

public sealed record ProviderCallContext(string IdempotencyKey, int Attempt);

public interface IMetadataProvider
{
    Task<MetadataProviderResult> GenerateAsync(
        MetadataRequest request,
        ProviderCallContext call,
        CancellationToken cancellationToken);
}

public sealed class MetadataProviderException(string safeMessage, bool retryable, Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    public bool Retryable { get; } = retryable;
}

internal sealed record OpenAiResponse(
    string? Id,
    string? Status,
    IReadOnlyList<OpenAiOutput>? Output,
    OpenAiUsage? Usage,
    OpenAiError? Error,
    [property: JsonPropertyName("incomplete_details")] OpenAiIncompleteDetails? IncompleteDetails);

internal sealed record OpenAiOutput(IReadOnlyList<OpenAiContent>? Content);
internal sealed record OpenAiContent(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("refusal")] string? Refusal);
internal sealed record OpenAiUsage([property: JsonPropertyName("input_tokens")] int? InputTokens, [property: JsonPropertyName("output_tokens")] int? OutputTokens);
internal sealed record OpenAiError(string? Message, string? Type, string? Code);
internal sealed record OpenAiIncompleteDetails(string? Reason);
