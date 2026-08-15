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
    public void ToString_never_exposes_secret_values()
    {
        var settings = CreateSettings(
            openAiApiKey: "sk-test-openai-value",
            bucketName: "bucket-private-value",
            bucketAccessKeyId: "tid_test-access-value",
            bucketSecretAccessKey: "tsec_test-secret-value");

        var description = settings.ToString();

        Assert.DoesNotContain("sk-test-openai-value", description);
        Assert.DoesNotContain("bucket-private-value", description);
        Assert.DoesNotContain("tid_test-access-value", description);
        Assert.DoesNotContain("tsec_test-secret-value", description);
        Assert.Contains("ExternalSecretsConfigured = True", description);
    }

    [Fact]
    public void ValidateLocalConfiguration_reports_invalid_non_secret_values()
    {
        var settings = new FactorySettings(
            "workspace", "future", null, null, "", "http://insecure.invalid", "", false, 0,
            null, null, null);

        var errors = settings.ValidateLocalConfiguration();

        Assert.Equal(5, errors.Count);
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
