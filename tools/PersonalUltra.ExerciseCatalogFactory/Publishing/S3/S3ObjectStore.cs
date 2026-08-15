using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using PersonalUltra.ExerciseCatalogFactory.Configuration;

namespace PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

internal sealed class S3ObjectStore : IObjectStore
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    internal S3ObjectStore(BucketOptions options, BucketCredentials credentials)
    {
        var clientConfig = CreateClientConfiguration(options);
        _bucketName = credentials.BucketName;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(credentials.AccessKeyId, credentials.SecretAccessKey),
            clientConfig);
    }

    internal static AmazonS3Config CreateClientConfiguration(BucketOptions options) => new()
    {
        ServiceURL = options.Endpoint.AbsoluteUri.TrimEnd('/'),
        AuthenticationRegion = options.Region,
        ForcePathStyle = options.ForcePathStyle
    };

    internal S3ObjectStore(IAmazonS3 client, string bucketName)
    {
        _client = client;
        _bucketName = bucketName;
    }

    public async Task<ObjectStoreResult> ProbeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.HeadBucketAsync(
                new HeadBucketRequest { BucketName = _bucketName }, cancellationToken);
            return new ObjectStoreResult(response.ResponseMetadata?.RequestId);
        }
        catch (AmazonS3Exception exception)
        {
            throw Wrap("HEAD bucket", exception);
        }
    }

    public async Task<ObjectStoreResult> PutAsync(
        ObjectKey key,
        ReadOnlyMemory<byte> content,
        string contentType,
        string sha256,
        CancellationToken cancellationToken)
    {
        RequireContentType(contentType);
        RequireSha256(sha256);
        try
        {
            await using var stream = new MemoryStream(content.ToArray(), writable: false);
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key.Value,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false
            };
            request.Metadata["sha256"] = sha256;
            var response = await _client.PutObjectAsync(request, cancellationToken);
            return new ObjectStoreResult(response.ResponseMetadata?.RequestId);
        }
        catch (AmazonS3Exception exception)
        {
            throw Wrap("PUT object", exception);
        }
    }

    public async Task<ObjectMetadata?> HeadAsync(ObjectKey key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = _bucketName, Key = key.Value },
                cancellationToken);
            return new ObjectMetadata(
                response.ContentLength,
                response.Headers.ContentType,
                response.Metadata["x-amz-meta-sha256"],
                response.ResponseMetadata?.RequestId);
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
            return null;
        }
        catch (AmazonS3Exception exception)
        {
            throw Wrap("HEAD object", exception);
        }
    }

    public async Task<ObjectContent> GetAsync(ObjectKey key, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetObjectAsync(_bucketName, key.Value, cancellationToken);
            await using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return new ObjectContent(
                buffer.ToArray(),
                response.Headers.ContentType,
                response.ResponseMetadata?.RequestId);
        }
        catch (AmazonS3Exception exception)
        {
            throw Wrap("GET object", exception);
        }
    }

    public Uri CreatePresignedGetUri(ObjectKey key, DateTimeOffset expiresAt)
    {
        try
        {
            var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key.Value,
                Verb = HttpVerb.GET,
                Expires = expiresAt.UtcDateTime,
                Protocol = Protocol.HTTPS
            });
            return new Uri(url, UriKind.Absolute);
        }
        catch (AmazonS3Exception exception)
        {
            throw Wrap("presign GET", exception);
        }
    }

    public async Task<ObjectStoreResult> DeleteAsync(ObjectKey key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.DeleteObjectAsync(_bucketName, key.Value, cancellationToken);
            return new ObjectStoreResult(response.ResponseMetadata?.RequestId);
        }
        catch (AmazonS3Exception exception)
        {
            throw Wrap("DELETE object", exception);
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private static bool IsNotFound(AmazonS3Exception exception) =>
        exception.StatusCode == HttpStatusCode.NotFound ||
        exception.ErrorCode is "NoSuchKey" or "NotFound";

    private static BucketOperationException Wrap(string operation, AmazonS3Exception exception) =>
        new(
            operation,
            exception.StatusCode,
            exception.RequestId,
            SafeErrorCode(exception.ErrorCode),
            SafeProviderMessage(exception),
            exception);

    private static string? SafeErrorCode(string? errorCode) =>
        string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 64 ||
        errorCode.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_')
            ? null
            : errorCode;

    private static string SafeProviderMessage(AmazonS3Exception exception)
    {
        return exception.ErrorCode switch
        {
            "InvalidAccessKeyId" => "Access key ID não reconhecido pelo provider.",
            "SignatureDoesNotMatch" => "Assinatura da requisição rejeitada pelo provider.",
            "AccessDenied" or "Forbidden" => "Acesso negado pelo provider.",
            "NoSuchKey" or "NotFound" => "Objeto não encontrado pelo provider.",
            _ => "Detalhes do provider omitidos por segurança."
        };
    }

    private static void RequireContentType(string contentType)
    {
        if (!string.Equals(contentType, "text/plain; charset=utf-8", StringComparison.Ordinal))
        {
            throw new ArgumentException("MIME não permitido para o smoke.", nameof(contentType));
        }
    }

    private static void RequireSha256(string sha256)
    {
        if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("SHA-256 inválido.", nameof(sha256));
        }
    }
}
