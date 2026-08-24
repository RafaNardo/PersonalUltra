using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PersonalUltra.Application.Training;

namespace PersonalUltra.Infrastructure;

internal sealed class S3ExerciseMediaResolver : IExerciseMediaResolver, IDisposable
{
    private readonly AmazonS3Client client;
    private readonly ExerciseMediaStorageOptions options;
    private readonly TimeProvider clock;

    public S3ExerciseMediaResolver(IOptions<ExerciseMediaStorageOptions> options, TimeProvider clock)
    {
        this.options = options.Value;
        this.clock = clock;
        client = new AmazonS3Client(
            new BasicAWSCredentials(this.options.AccessKeyId, this.options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = this.options.EndpointUrl.TrimEnd('/'),
                AuthenticationRegion = this.options.Region,
                ForcePathStyle = this.options.ForcePathStyle,
            });
    }

    public string? ResolveUrl(string? imageRef)
    {
        if (string.IsNullOrWhiteSpace(imageRef) || !ExerciseMediaReference.IsMediaReference(imageRef))
            return null;

        var reference = ExerciseMediaReference.Parse(imageRef);
        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = reference.ObjectKey,
            Verb = HttpVerb.GET,
            Expires = clock.GetUtcNow().AddMinutes(options.SignedUrlLifetimeMinutes).UtcDateTime,
            Protocol = Protocol.HTTPS,
        });

        if (!Uri.TryCreate(url, UriKind.Absolute, out var signedUri) || signedUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The exercise media provider did not generate a valid HTTPS URL.");

        return signedUri.AbsoluteUri;
    }

    public void Dispose() => client.Dispose();
}
