using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace PersonalUltra.Infrastructure;

internal sealed partial class ExerciseMediaStorageOptionsValidator : IValidateOptions<ExerciseMediaStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, ExerciseMediaStorageOptions options)
    {
        var failures = new List<string>();
        if (!Uri.TryCreate(options.EndpointUrl, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment) ||
            endpoint.AbsolutePath != "/")
        {
            failures.Add("RailwayBucket:EndpointUrl must be an HTTPS origin without credentials, path, query, or fragment.");
        }

        if (!RegionPattern().IsMatch(options.Region ?? string.Empty))
            failures.Add("RailwayBucket:Region is invalid.");
        if (options.SignedUrlLifetimeMinutes is < 1 or > 10080)
            failures.Add("RailwayBucket:SignedUrlLifetimeMinutes must be between 1 and 10080.");
        if (string.IsNullOrWhiteSpace(options.BucketName))
            failures.Add("RailwayBucket:BucketName is required.");
        if (string.IsNullOrWhiteSpace(options.AccessKeyId))
            failures.Add("RailwayBucket:AccessKeyId is required.");
        if (string.IsNullOrWhiteSpace(options.SecretAccessKey))
            failures.Add("RailwayBucket:SecretAccessKey is required.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex RegionPattern();
}
