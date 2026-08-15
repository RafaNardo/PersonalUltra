namespace PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

internal interface IObjectStore : IAsyncDisposable
{
    Task<ObjectStoreResult> ProbeAsync(CancellationToken cancellationToken);
    Task<ObjectStoreResult> PutAsync(
        ObjectKey key,
        ReadOnlyMemory<byte> content,
        string contentType,
        string sha256,
        CancellationToken cancellationToken);
    Task<ObjectMetadata?> HeadAsync(ObjectKey key, CancellationToken cancellationToken);
    Task<ObjectContent> GetAsync(ObjectKey key, CancellationToken cancellationToken);
    Uri CreatePresignedGetUri(ObjectKey key, DateTimeOffset expiresAt);
    Task<ObjectStoreResult> DeleteAsync(ObjectKey key, CancellationToken cancellationToken);
}

internal sealed record ObjectStoreResult(string? RequestId);

internal sealed record ObjectMetadata(
    long Length,
    string? ContentType,
    string? Sha256,
    string? RequestId);

internal sealed record ObjectContent(
    byte[] Bytes,
    string? ContentType,
    string? RequestId);
