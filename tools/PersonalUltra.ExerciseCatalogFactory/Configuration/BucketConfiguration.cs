using System.Text.RegularExpressions;

namespace PersonalUltra.ExerciseCatalogFactory.Configuration;

internal sealed class BucketCredentials
{
    internal BucketCredentials(string bucketName, string accessKeyId, string secretAccessKey)
    {
        BucketName = RequireSecret(bucketName, nameof(bucketName));
        AccessKeyId = RequireSecret(accessKeyId, nameof(accessKeyId));
        SecretAccessKey = RequireSecret(secretAccessKey, nameof(secretAccessKey));
    }

    internal string BucketName { get; }
    internal string AccessKeyId { get; }
    internal string SecretAccessKey { get; }

    private static string RequireSecret(string value, string name) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Credencial obrigatória ausente.", name);
}

internal sealed class BucketOptions
{
    private static readonly Regex RegionPattern = new("^[a-z0-9][a-z0-9-]{0,31}$", RegexOptions.CultureInvariant);

    internal BucketOptions(string endpointUrl, string region, bool forcePathStyle, TimeSpan signedUrlLifetime)
    {
        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment) ||
            endpoint.AbsolutePath != "/")
        {
            throw new InvalidOperationException(
                "RailwayBucket:EndpointUrl deve ser uma origem HTTPS absoluta, sem credenciais, path, query ou fragmento.");
        }

        if (!RegionPattern.IsMatch(region))
        {
            throw new InvalidOperationException("RailwayBucket:Region possui formato inválido.");
        }

        if (signedUrlLifetime < TimeSpan.FromMinutes(1) || signedUrlLifetime > TimeSpan.FromDays(7))
        {
            throw new InvalidOperationException(
                "RailwayBucket:SignedUrlLifetimeMinutes deve ficar entre 1 e 10080.");
        }

        Endpoint = endpoint;
        Region = region;
        ForcePathStyle = forcePathStyle;
        SignedUrlLifetime = signedUrlLifetime;
    }

    internal Uri Endpoint { get; }
    internal string Region { get; }
    internal bool ForcePathStyle { get; }
    internal TimeSpan SignedUrlLifetime { get; }
    internal string AddressingStyle => ForcePathStyle ? "path-style" : "virtual-hosted-style";
}
