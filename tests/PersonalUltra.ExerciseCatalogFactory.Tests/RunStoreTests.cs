using System.Security.Cryptography;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class RunStoreTests : IDisposable
{
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), $"personal-ultra-factory-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndLoad_preserves_manifest_and_leaves_no_temporary_file()
    {
        var store = new RunStore(_workspace);
        var now = DateTimeOffset.UtcNow;
        var original = CreateRun("run-1", now);

        await store.SaveAsync(original);
        var updated = original with { Status = "needs_review", UpdatedAt = now.AddMinutes(1) };
        await store.SaveAsync(updated);

        var loaded = await store.LoadAsync("run-1");

        Assert.NotNull(loaded);
        Assert.Equal("needs_review", loaded.Status);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(store.RunsRoot, "run-1"), "*.tmp"));
    }

    [Fact]
    public async Task CopySource_preserves_immutable_copy_and_integrity_after_original_is_deleted()
    {
        var sourcePath = Path.Combine(_workspace, "outside", "input.json");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        await File.WriteAllTextAsync(sourcePath, "{\"items\":[]}");
        var store = new RunStore(Path.Combine(_workspace, "factory"));

        var source = await store.CopySourceAsync("run-copy", sourcePath);
        var run = CreateRun("run-copy", DateTimeOffset.UtcNow) with { Source = source };
        await store.SaveAsync(run);
        File.Delete(sourcePath);

        await store.VerifySourceAsync(run);
        Assert.True(File.Exists(Path.Combine(store.RunsRoot, "run-copy", "source", "input.json")));
    }

    [Fact]
    public async Task VerifySource_rejects_tampered_copy()
    {
        var sourcePath = Path.Combine(_workspace, "input.json");
        Directory.CreateDirectory(_workspace);
        await File.WriteAllTextAsync(sourcePath, "original");
        var store = new RunStore(Path.Combine(_workspace, "factory"));
        var source = await store.CopySourceAsync("run-tamper", sourcePath);
        var run = CreateRun("run-tamper", DateTimeOffset.UtcNow) with { Source = source };
        await store.SaveAsync(run);
        await File.WriteAllTextAsync(Path.Combine(store.RunsRoot, "run-tamper", "source", "input.json"), "changed");

        await Assert.ThrowsAsync<InvalidDataException>(() => store.VerifySourceAsync(run));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\escape")]
    [InlineData("run id")]
    [InlineData("")]
    public async Task Load_rejects_unsafe_run_id(string runId)
    {
        var store = new RunStore(_workspace);

        await Assert.ThrowsAsync<ArgumentException>(() => store.LoadAsync(runId));
    }

    [Fact]
    public async Task Load_reports_corrupt_manifest_without_exposing_json_contents()
    {
        var manifestDirectory = Path.Combine(_workspace, "runs", "corrupt-run");
        Directory.CreateDirectory(manifestDirectory);
        await File.WriteAllTextAsync(Path.Combine(manifestDirectory, "manifest.json"), "{ secret payload");
        var store = new RunStore(_workspace);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync("corrupt-run"));

        Assert.Contains("Manifesto inválido", exception.Message);
        Assert.DoesNotContain("secret payload", exception.Message);
    }

    [Fact]
    public async Task LoadAll_orders_newest_run_first()
    {
        var store = new RunStore(_workspace);
        var old = CreateRun("old", DateTimeOffset.UtcNow.AddHours(-1));
        var recent = CreateRun("recent", DateTimeOffset.UtcNow);
        await store.SaveAsync(old);
        await store.SaveAsync(recent);

        var runs = await store.LoadAllAsync();

        Assert.Equal(["recent", "old"], runs.Select(run => run.RunId));
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace)) Directory.Delete(_workspace, recursive: true);
    }

    private static FactoryRun CreateRun(string id, DateTimeOffset createdAt) =>
        new(
            "1",
            id,
            createdAt,
            createdAt,
            "imported",
            true,
            0,
            new SourceArtifact(
                "input.json",
                "C:/input.json",
                "source/input.json",
                Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id))).ToLowerInvariant(),
                10));
}
