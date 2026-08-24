using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PersonalUltra.Infrastructure;
using Xunit;

namespace PersonalUltra.Api.IntegrationTests;

public sealed class ExerciseMediaConfigurationTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("10081")]
    public void Signed_url_expiry_outside_safe_range_is_rejected(string lifetimeMinutes)
    {
        var configuration = ValidConfiguration();
        configuration["RailwayBucket:SignedUrlLifetimeMinutes"] = lifetimeMinutes;

        AssertInvalid(configuration);
    }

    [Fact]
    public void Missing_bucket_secret_is_rejected_without_falling_back_to_mobile_credentials()
    {
        var configuration = ValidConfiguration();
        configuration.Remove("RailwayBucket:SecretAccessKey");

        AssertInvalid(configuration);
    }

    private static void AssertInvalid(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddExerciseMediaResolver(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ExerciseMediaStorageOptions>>().Value);
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        ["RailwayBucket:EndpointUrl"] = "https://test.storageapi.dev",
        ["RailwayBucket:Region"] = "auto",
        ["RailwayBucket:ForcePathStyle"] = "false",
        ["RailwayBucket:SignedUrlLifetimeMinutes"] = "15",
        ["RailwayBucket:BucketName"] = "personal-ultra-tests",
        ["RailwayBucket:AccessKeyId"] = "test-access-key",
        ["RailwayBucket:SecretAccessKey"] = "test-secret-key",
    };
}
