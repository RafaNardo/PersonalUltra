namespace PersonalUltra.ExerciseCatalogFactory.Images;

internal sealed record ImagePilotManifest(
    int Version,
    string Model,
    string Size,
    string Quality,
    string PromptVersion,
    decimal EstimatedCostPerImageUsd,
    IReadOnlyList<ImagePilotItem> Items);

internal sealed record ImagePilotItem(
    string Name,
    string Slug,
    string Prompt,
    string LocalFile,
    string? Sha256 = null,
    bool Approved = false,
    bool Uploaded = false,
    string? ObjectKey = null);

public sealed record GeneratedImage(byte[] Bytes, string? RequestId);

public interface IImageProvider
{
    Task<GeneratedImage> GenerateAsync(
        string model,
        string prompt,
        string size,
        string quality,
        CancellationToken cancellationToken);
}

internal sealed class ImageProviderException(string safeMessage, bool retryable, Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    internal bool Retryable { get; } = retryable;
}
