using PersonalUltra.ExerciseCatalogFactory.Configuration;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class FactorySettingsTests
{
    [Fact]
    public void MissingSecretNames_returns_names_only()
    {
        var settings = CreateSettings();

        var missing = settings.MissingSecretNames();

        Assert.Equal([
            "ai-api-key",
            "RailwayBucket:BucketName",
            "RailwayBucket:AccessKeyId",
            "RailwayBucket:SecretAccessKey"
        ], missing);
    }

    [Fact]
    public void Settings_and_credentials_use_type_name_instead_of_secret_values()
    {
        var settings = CreateSettings(
            openAiApiKey: "openai-test-value",
            bucketName: "bucket-private-value",
            bucketAccessKeyId: "access-test-value",
            bucketSecretAccessKey: "secret-test-value");

        var description = settings.ToString()!;
        var credentialsDescription = settings.GetBucketCredentials().ToString()!;

        Assert.DoesNotContain("openai-test-value", description);
        Assert.DoesNotContain("bucket-private-value", description);
        Assert.DoesNotContain("access-test-value", description);
        Assert.DoesNotContain("secret-test-value", description);
        Assert.DoesNotContain("bucket-private-value", credentialsDescription);
        Assert.DoesNotContain("access-test-value", credentialsDescription);
        Assert.DoesNotContain("secret-test-value", credentialsDescription);
        Assert.Equal(typeof(FactorySettings).FullName, description);
    }

    [Fact]
    public void ValidateLocalConfiguration_reports_invalid_non_secret_values()
    {
        var settings = new FactorySettings(
            "workspace", "future", null, null, "", "http://insecure.invalid", "", false, 0,
            null, null, null);

        var errors = settings.ValidateLocalConfiguration();

        Assert.Contains(errors, error => error.Contains("SchemaVersion", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("ImageModel", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("EndpointUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveWorkspaceRoot_uses_solution_root_instead_of_current_directory()
    {
        var repositoryRoot = FindRepositoryRoot();
        var nestedDirectory = Path.Combine(repositoryRoot, "tools", "PersonalUltra.ExerciseCatalogFactory", "bin");

        var resolved = FactorySettings.ResolveWorkspaceRoot(
            "tools/PersonalUltra.ExerciseCatalogFactory/workspace",
            nestedDirectory,
            nestedDirectory);

        Assert.Equal(
            Path.Combine(repositoryRoot, "tools", "PersonalUltra.ExerciseCatalogFactory", "workspace"),
            resolved);
    }

    internal static FactorySettings CreateSettings(
        string? workspace = null,
        string? openAiApiKey = null,
        string? bucketName = null,
        string? bucketAccessKeyId = null,
        string? bucketSecretAccessKey = null) =>
        new(
            workspace ?? Path.Combine(Path.GetTempPath(), $"personal-ultra-settings-{Guid.NewGuid():N}"),
            "1",
            openAiApiKey,
            "gpt-5.6-luna",
            "gpt-image-2",
            "https://example.invalid",
            "auto",
            false,
            360,
            bucketName,
            bucketAccessKeyId,
            bucketSecretAccessKey);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PersonalUltra.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("PersonalUltra.sln não encontrada durante o teste.");
    }
}
