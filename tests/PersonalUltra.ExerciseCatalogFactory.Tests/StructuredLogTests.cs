using PersonalUltra.ExerciseCatalogFactory.Logging;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class StructuredLogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"personal-ultra-log-{Guid.NewGuid():N}");

    [Fact]
    public async Task Log_redacts_sensitive_keys_and_credential_patterns()
    {
        var logger = new StructuredLog(_root);

        await logger.WriteAsync(
            "information",
            "redaction.test",
            "Authorization Bearer token-value and sk-openai-value",
            new Dictionary<string, object?>
            {
                ["apiKey"] = "visible-no",
                ["safe"] = "tid_access-value"
            });
        var content = await File.ReadAllTextAsync(Assert.Single(Directory.EnumerateFiles(_root, "*.jsonl")));

        Assert.DoesNotContain("token-value", content);
        Assert.DoesNotContain("openai-value", content);
        Assert.DoesNotContain("visible-no", content);
        Assert.DoesNotContain("access-value", content);
        Assert.Contains("[REDACTED]", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
