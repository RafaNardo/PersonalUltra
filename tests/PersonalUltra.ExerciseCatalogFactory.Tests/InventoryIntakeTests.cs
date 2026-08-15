using PersonalUltra.ExerciseCatalogFactory.Intake;
using PersonalUltra.ExerciseCatalogFactory.Normalization;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class InventoryIntakeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"personal-ultra-intake-{Guid.NewGuid():N}");

    [Fact]
    public async Task Versioned_inventory_contains_exactly_232_candidates()
    {
        var path = FindInventory();
        var rows = await CatalogInputReader.ReadAsync(path);

        Assert.Equal(232, rows.Count);
        Assert.Equal(232, rows.Select(row => row.Item.Name).Distinct(StringComparer.Ordinal).Count());

        var catalog = new CatalogNormalizer().Normalize(rows, Path.GetFileName(path), new string('a', 64));
        Assert.Equal(28, catalog.PreservedLegacyIdentities.Count);
        Assert.Equal(17, catalog.MatchedLegacyIdentities.Count);
        Assert.Equal(11, catalog.UnresolvedLegacyIdentities.Count);
        Assert.All(catalog.UnresolvedLegacyIdentities, legacy =>
            Assert.Contains(catalog.Issues, issue =>
                issue.Code == "known-legacy-ambiguity" && issue.Message.Contains(legacy.Slug, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Intake_reopen_is_verified_cache_hit_and_idempotent()
    {
        Directory.CreateDirectory(_root);
        var workspace = Path.Combine(_root, "workspace");
        var store = new RunStore(workspace);
        var now = DateTimeOffset.UtcNow;
        var source = await store.CopySourceAsync("intake-cache", FindInventory());
        var run = new PersonalUltra.ExerciseCatalogFactory.Domain.FactoryRun(
            "1", "intake-cache", now, now, "imported", true, 0, source,
            new("factory-v1", "pending", "pending", "pending", "pending", "pending"), [], [],
            new("USD", 0, 0, 0), new Dictionary<string, string> { ["source"] = source.Sha256 }, []);
        await store.SaveAsync(run);
        var processor = new IntakeProcessor(store);

        var first = await processor.ExecuteAsync(run);
        var reopenedRun = await store.LoadAsync(run.RunId);
        var second = await processor.ExecuteAsync(reopenedRun!);

        Assert.False(first.CacheHit);
        Assert.True(second.CacheHit);
        Assert.Equal(232, second.Catalog.Items.Count);
        Assert.Equal(first.Run.StageHashes!["normalization"], second.Run.StageHashes!["normalization"]);
        Assert.Equal(first.Run.Outputs, second.Run.Outputs);
        Assert.Equal(0, second.Run.Usage!.ObservedCost);
        Assert.Empty(second.Run.Attempts!);
    }

    [Fact]
    public async Task Missing_or_corrupt_report_is_regenerated_and_never_returns_false_cache_hit()
    {
        Directory.CreateDirectory(_root);
        var workspace = Path.Combine(_root, "workspace");
        var store = new RunStore(workspace);
        var now = DateTimeOffset.UtcNow;
        var source = await store.CopySourceAsync("intake-report-integrity", FindInventory());
        var run = new PersonalUltra.ExerciseCatalogFactory.Domain.FactoryRun(
            "1", "intake-report-integrity", now, now, "imported", true, 0, source,
            new("factory-v1", "pending", "pending", "pending", "pending", "pending"), [], [],
            new("USD", 0, 0, 0), new Dictionary<string, string> { ["source"] = source.Sha256 }, []);
        await store.SaveAsync(run);
        var processor = new IntakeProcessor(store);
        var first = await processor.ExecuteAsync(run);
        var reportPath = Path.Combine(workspace, "runs", run.RunId, "normalization", "intake-report.v1.md");

        File.Delete(reportPath);
        var afterMissing = await processor.ExecuteAsync((await store.LoadAsync(run.RunId))!);
        Assert.False(afterMissing.CacheHit);
        Assert.True(File.Exists(reportPath));
        Assert.Contains("intencionalmente diferido", await File.ReadAllTextAsync(reportPath));

        await File.WriteAllTextAsync(reportPath, "corrompido");
        var afterCorruption = await processor.ExecuteAsync((await store.LoadAsync(run.RunId))!);
        Assert.False(afterCorruption.CacheHit);
        Assert.Contains("Identidades legadas vinculadas por slug exato: 17", await File.ReadAllTextAsync(reportPath));

        var verified = await processor.ExecuteAsync((await store.LoadAsync(run.RunId))!);
        Assert.True(verified.CacheHit);
        Assert.Equal(first.Catalog.Items.Count, verified.Catalog.Items.Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static string FindInventory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "PersonalUltra.ExerciseCatalogFactory", "Inputs", "v1", "exercise-inventory-v1.csv");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Inventário canônico não encontrado.");
    }
}
