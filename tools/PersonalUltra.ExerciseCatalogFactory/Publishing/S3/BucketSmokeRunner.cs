using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.ExceptionServices;
using PersonalUltra.ExerciseCatalogFactory.Configuration;

namespace PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

internal sealed class BucketSmokeRunner(IObjectStore store, HttpClient httpClient, BucketOptions options)
{
    internal const string ContentType = "text/plain; charset=utf-8";

    internal async Task<BucketSmokeReport> RunAsync(string runId, CancellationToken cancellationToken)
    {
        var key = ObjectKey.CreateSmoke(runId, Guid.NewGuid());
        var bytes = Encoding.UTF8.GetBytes($"personal-ultra-bucket-smoke:{runId}:{Guid.NewGuid():N}");
        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var steps = new List<BucketSmokeStep>();
        Exception? primaryFailure = null;
        BucketCleanupException? cleanupFailure = null;

        try
        {
            var put = await store.PutAsync(key, bytes, ContentType, expectedHash, cancellationToken);
            steps.Add(new BucketSmokeStep("PUT", put.RequestId));

            var head = await store.HeadAsync(key, cancellationToken)
                ?? throw new InvalidDataException("HEAD não encontrou o objeto criado pelo smoke.");
            ValidateMetadata(head, bytes.Length, expectedHash);
            steps.Add(new BucketSmokeStep("HEAD", head.RequestId));

            var get = await store.GetAsync(key, cancellationToken);
            ValidateContent(get.Bytes, get.ContentType, bytes, expectedHash, "GET autenticado");
            steps.Add(new BucketSmokeStep("GET", get.RequestId));

            var signedUri = store.CreatePresignedGetUri(key, DateTimeOffset.UtcNow + options.SignedUrlLifetime);
            using var response = await httpClient.GetAsync(
                signedUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidDataException(
                    $"GET assinado retornou HTTP {(int)response.StatusCode}.");
            }

            var signedBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            ValidateContent(
                signedBytes,
                response.Content.Headers.ContentType?.ToString(),
                bytes,
                expectedHash,
                "GET assinado");
            steps.Add(new BucketSmokeStep("PRESIGNED GET", null));
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }
        finally
        {
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            ObjectStoreResult? deleted = null;
            try
            {
                deleted = await store.DeleteAsync(key, cleanupTimeout.Token);
                steps.Add(new BucketSmokeStep("DELETE", deleted.RequestId));
            }
            catch (Exception exception)
            {
                cleanupFailure = new BucketCleanupException("DELETE", exception);
            }

            if (deleted is not null)
            {
                try
                {
                var remaining = await store.HeadAsync(key, cleanupTimeout.Token);
                if (remaining is not null)
                {
                    throw new InvalidDataException("O objeto delimitado ainda existe após DELETE.");
                }

                steps.Add(new BucketSmokeStep("CONFIRM NOT FOUND", null));
                }
                catch (Exception exception)
                {
                    cleanupFailure = new BucketCleanupException("CONFIRM NOT FOUND", exception);
                }
            }
        }

        if (primaryFailure is not null && cleanupFailure is not null)
        {
            throw new BucketSmokeException(primaryFailure, cleanupFailure);
        }

        if (cleanupFailure is not null) throw cleanupFailure;
        if (primaryFailure is not null) ExceptionDispatchInfo.Capture(primaryFailure).Throw();
        return new BucketSmokeReport(options.AddressingStyle, expectedHash, ContentType, steps);
    }

    private static void ValidateMetadata(ObjectMetadata metadata, int expectedLength, string expectedHash)
    {
        if (metadata.Length != expectedLength)
        {
            throw new InvalidDataException("HEAD retornou tamanho diferente do conteúdo enviado.");
        }

        if (!string.Equals(metadata.ContentType, ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("HEAD retornou MIME diferente do conteúdo enviado.");
        }

        if (!string.Equals(metadata.Sha256, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("HEAD retornou SHA-256 diferente do conteúdo enviado.");
        }
    }

    private static void ValidateContent(
        byte[] actual,
        string? actualContentType,
        byte[] expected,
        string expectedHash,
        string operation)
    {
        if (!actual.AsSpan().SequenceEqual(expected) ||
            !string.Equals(Convert.ToHexStringLower(SHA256.HashData(actual)), expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{operation} retornou bytes ou SHA-256 divergentes.");
        }

        if (!string.Equals(actualContentType, ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"{operation} retornou MIME divergente.");
        }
    }
}

internal sealed record BucketSmokeStep(string Operation, string? RequestId);

internal sealed record BucketSmokeReport(
    string AddressingStyle,
    string Sha256,
    string ContentType,
    IReadOnlyList<BucketSmokeStep> Steps);
