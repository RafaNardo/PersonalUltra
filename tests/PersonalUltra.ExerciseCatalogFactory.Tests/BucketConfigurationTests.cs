using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class BucketConfigurationTests
{
    [Fact]
    public void Bucket_options_reject_endpoint_paths_userinfo_queries_and_invalid_regions()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new BucketOptions("https://user@example.invalid/path?secret=x", "auto", false, TimeSpan.FromMinutes(10)));
        Assert.Throws<InvalidOperationException>(() =>
            new BucketOptions("https://example.invalid", "INVALID_REGION", false, TimeSpan.FromMinutes(10)));
        Assert.Throws<InvalidOperationException>(() =>
            new BucketOptions("https://example.invalid", "auto", false, TimeSpan.FromSeconds(59)));
    }

    [Fact]
    public void Object_key_policy_accepts_only_the_exact_smoke_shape()
    {
        var key = ObjectKey.CreateSmoke("20260815010101-abcdef12", Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.Equal(
            "smoke/20260815010101-abcdef12/11111111111111111111111111111111.txt",
            key.Value);
        Assert.Equal(key.Value, ObjectKey.ParseSmoke(key.Value).Value);
        Assert.Throws<ArgumentException>(() => ObjectKey.ParseSmoke("smoke/../../other.txt"));
        Assert.Throws<ArgumentException>(() => ObjectKey.ParseSmoke("exercise-catalog/all.txt"));
        Assert.Throws<ArgumentException>(() => ObjectKey.CreateSmoke("unsafe/path", Guid.NewGuid()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForcePathStyle_is_propagated_to_the_exact_AmazonS3Config_used_by_the_client(bool forcePathStyle)
    {
        var options = new BucketOptions(
            "https://t3.storageapi.dev",
            "auto",
            forcePathStyle,
            TimeSpan.FromMinutes(5));

        var clientConfiguration = S3ObjectStore.CreateClientConfiguration(options);

        Assert.Equal(forcePathStyle, clientConfiguration.ForcePathStyle);
        Assert.Equal("https://t3.storageapi.dev/", clientConfiguration.ServiceURL);
        Assert.Equal("auto", clientConfiguration.AuthenticationRegion);
    }

    [Theory]
    [InlineData(null, "n/a")]
    [InlineData("short", "[REDACTED]")]
    [InlineData("request-id-sensitive", "reques…[REDACTED]")]
    public void Request_ids_are_always_redacted(string? value, string expected) =>
        Assert.Equal(expected, Cli.BucketCommands.RedactRequestId(value));

    [Fact]
    public void Composite_failure_description_never_exposes_exception_payloads()
    {
        var inner = new InvalidOperationException(
            "smoke/private/key https://example.invalid/file?X-Amz-Signature=secret access-secret");

        var description = Cli.BucketCommands.DescribeFailure(inner);

        Assert.DoesNotContain("smoke/private", description, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", description, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", description, StringComparison.Ordinal);
    }
}
