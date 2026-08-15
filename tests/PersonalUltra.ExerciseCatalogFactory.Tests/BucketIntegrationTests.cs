using PersonalUltra.ExerciseCatalogFactory.Cli;
using PersonalUltra.ExerciseCatalogFactory.Configuration;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class BucketIntegrationTests
{
    [Fact]
    [Trait("Category", "External")]
    public async Task Railway_bucket_smoke_is_explicitly_opt_in()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("PERSONAL_ULTRA_FACTORY_RUN_BUCKET_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var output = new StringWriter();
        var error = new StringWriter();
        var command = new BucketCommands(FactorySettings.Load(), output, error);

        var exit = await command.RunAsync(["smoke", "--execute"], CancellationToken.None);

        Assert.True(exit == 0, $"Smoke externo falhou sem expor detalhes sensíveis: {error}");
        Assert.Contains("PASSED", output.ToString());
        Assert.DoesNotContain("X-Amz-", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
