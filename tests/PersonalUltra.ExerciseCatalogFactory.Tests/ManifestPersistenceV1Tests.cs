using System.Security.Cryptography;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ManifestPersistenceV1Tests : IDisposable
{
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"personal-ultra-manifest-{Guid.NewGuid():N}");

    [Fact]
    public async Task Reopen_preserves_attempt_usage_cost_and_redacted_references()
    {
        var store = new RunStore(_root);
        var run = CreateRun("reopen") with
        {
            Items = [ValidItem()],
            Attempts = [new ProviderAttempt("metadata", "bench", "openai", "configured-model", "idem-reopen", "response_123", 2,
                DateTimeOffset.Parse("2026-08-14T12:00:00Z"), DateTimeOffset.Parse("2026-08-14T12:00:01Z"),
                Hash, "metadata-v1", "succeeded", new ProviderCost("USD", 0.01m, 0.009m),
                new ProviderUsage(100, 20, null))],
            Usage = new UsageSummary("USD", 0.01m, 0.009m, 2)
        };

        await store.SaveAsync(run);
        var reopened = await new RunStore(_root).LoadAsync("reopen");

        Assert.NotNull(reopened);
        Assert.Equal(run.SchemaVersion, reopened.SchemaVersion);
        Assert.Equal(run.RunId, reopened.RunId);
        Assert.Equal(run.Versions, reopened.Versions);
        Assert.Single(reopened.Items!);
        Assert.Empty(reopened.Outputs!);
        Assert.Equal(0.009m, reopened!.Usage!.ObservedCost);
        Assert.Equal("response_123", Assert.Single(reopened.Attempts!).RequestId);
    }

    [Fact]
    public async Task Interrupted_replace_keeps_last_known_good_manifest_and_cleans_temp_file()
    {
        var initial = CreateRun("interrupted");
        await new RunStore(_root).SaveAsync(initial);
        var interruptedStore = new RunStore(_root, (_, _) => throw new OperationCanceledException("simulated"));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            interruptedStore.SaveAsync(initial with { Status = "processing", ResumeCount = 1 }));
        var reopened = await new RunStore(_root).LoadAsync("interrupted");

        Assert.Equal("imported", reopened!.Status);
        Assert.Equal(0, reopened.ResumeCount);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(_root, "runs", "interrupted"), "*.tmp"));
    }

    [Theory]
    [InlineData("sk-super-secret")]
    [InlineData("Bearer abc.def.ghi")]
    [InlineData("tsec_sensitive")]
    public async Task Manifest_rejects_sensitive_provider_references(string sensitive)
    {
        var run = CreateRun("redaction") with
        {
            Items = [ValidItem()],
            Attempts = [new ProviderAttempt("image", "bench", "openai", "gpt-image", "idem-redaction", sensitive, 1,
                DateTimeOffset.UtcNow, null, Hash, "image-v1", "started", new ProviderCost("USD", null, null))]
        };

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new RunStore(_root).SaveAsync(run));

        Assert.Contains("sensível", exception.Message);
        Assert.DoesNotContain(sensitive, exception.Message);
    }

    [Fact]
    public async Task Unknown_manifest_schema_and_corrupt_json_fail_explicitly_without_payload()
    {
        var store = new RunStore(_root);
        var run = CreateRun("unknown") with { SchemaVersion = "999" };
        var unknown = await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(run));
        Assert.Contains("schemaVersion desconhecido", unknown.Message);

        var directory = Path.Combine(_root, "runs", "corrupt");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "manifest.json"), "{ sk-never-echo");
        var corrupt = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync("corrupt"));
        Assert.DoesNotContain("sk-never-echo", corrupt.Message);
    }

    [Fact]
    public async Task Manifest_rejects_unknown_raw_provider_payload_without_echoing_it()
    {
        var store = new RunStore(_root);
        await store.SaveAsync(CreateRun("payload"));
        var path = Path.Combine(_root, "runs", "payload", "manifest.json");
        var json = await File.ReadAllTextAsync(path);
        json = json.Replace("\"outputs\": []", "\"outputs\": [], \"providerPayload\": \"sk-never-persist\"", StringComparison.Ordinal);
        await File.WriteAllTextAsync(path, json);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync("payload"));

        Assert.Contains("Manifesto inválido", exception.Message);
        Assert.DoesNotContain("sk-never-persist", exception.Message);
    }

    [Fact]
    public async Task Manifest_rejects_duplicate_external_keys()
    {
        var item = new ManifestItem("same", "imported", new ItemSource("source/input.json", 2, Hash),
            new ItemStageHashes(Hash));
        var run = CreateRun("duplicates") with { Items = [item, item] };

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new RunStore(_root).SaveAsync(run));

        Assert.Contains("externalKey duplicada", exception.Message);
    }

    [Fact]
    public async Task Manifest_rejects_malformed_nested_contracts()
    {
        var item = ValidItem();
        var cases = new FactoryRun[]
        {
            CreateRun("bad-null-source") with { Source = null! },
            CreateRun("bad-source-path") with { Source = CreateRun("x").Source with { StoredRelativePath = "../escape.json" } },
            CreateRun("bad-null-item") with { Items = [null!] },
            CreateRun("bad-null-hashes") with { Items = [item with { Hashes = null! }] },
            CreateRun("bad-item-source") with { Items = [item with { Source = item.Source with { File = "source/../escape.json" } }] },
            CreateRun("bad-null-artifact") with { Outputs = [null!] },
            CreateRun("bad-empty-artifact") with { Outputs = [new ArtifactReference("source", "", Hash, 1)] },
            CreateRun("bad-null-review") with { Items = [item with { Reviews = [null!] }] },
            CreateRun("bad-review-stage") with { Items = [item with { Reviews = [ValidReview() with { Stage = "unknown" }] }] },
            CreateRun("bad-review-reason") with { Items = [item with { Reviews = [ValidReview() with { ReasonCode = "" }] }] },
            CreateRun("bad-reviewer") with { Items = [item with { Reviews = [ValidReview() with { Reviewer = "" }] }] },
            CreateRun("bad-review-key") with { Items = [item with { Reviews = [ValidReview() with { ItemKey = "other" }] }] },
            CreateRun("bad-null-cost") with { Attempts = [ValidAttempt() with { Cost = null! }] }
        };

        foreach (var run in cases)
            await Assert.ThrowsAsync<InvalidDataException>(() => new RunStore(_root).SaveAsync(run));
    }

    [Fact]
    public async Task Manifest_rejects_credential_like_content_in_every_free_text_area_without_echo()
    {
        const string secret = "sk-never-write-this";
        var item = ValidItem();
        var cases = new FactoryRun[]
        {
            CreateRun("secret-source-name") with { Source = CreateRun("x").Source with { FileName = secret + ".json" } },
            CreateRun("secret-source-path") with { Source = CreateRun("x").Source with { OriginalAbsolutePath = "C:/" + secret + "/input.json" } },
            CreateRun("secret-version") with { Versions = CreateRun("x").Versions! with { StyleVersion = secret } },
            CreateRun("secret-stage-key") with { StageHashes = new Dictionary<string, string> { [secret] = Hash } },
            CreateRun("secret-item-key") with { Items = [item with { ExternalKey = secret }] },
            CreateRun("secret-item-file") with { Items = [item with { Source = item.Source with { File = "source/" + secret + ".json" } }] },
            CreateRun("secret-artifact") with { Outputs = [new ArtifactReference("source", "artifacts/" + secret + ".json", Hash, 1)] },
            CreateRun("secret-reviewer") with { Items = [item with { Reviews = [ValidReview() with { Reviewer = secret }] }] },
            CreateRun("secret-reason") with { Items = [item with { Reviews = [ValidReview() with { ReasonCode = secret }] }] }
        };

        foreach (var run in cases)
        {
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new RunStore(_root).SaveAsync(run));
            Assert.DoesNotContain(secret, exception.Message);
        }
    }

    [Fact]
    public async Task Serialized_manifest_matches_v1_golden_file()
    {
        var store = new RunStore(_root);
        await store.SaveAsync(CreateRun("golden"));
        var actual = Normalize(await File.ReadAllTextAsync(Path.Combine(_root, "runs", "golden", "manifest.json")));
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v1.golden.json");

        Assert.Equal(Normalize(await File.ReadAllTextAsync(fixture)), actual);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static FactoryRun CreateRun(string runId)
    {
        var instant = DateTimeOffset.Parse("2026-08-14T12:00:00Z");
        return new FactoryRun("1", runId, instant, instant, "imported", true, 0,
            new SourceArtifact("input.json", "C:/safe/input.json", "source/input.json", Hash, 42),
            new PipelineVersions("factory-v1", "taxonomy-v1", "metadata-v1", "image-v1", "style-v1", "target-v1"),
            [], [], new UsageSummary("USD", 0, 0, 0), new Dictionary<string, string>(), []);
    }

    private static ManifestItem ValidItem() =>
        new("bench", "metadata_approved", new ItemSource("source/input.json", 2, Hash),
            new ItemStageHashes(Hash, Hash, Hash), [], [ValidReview()]);

    private static ReviewDecision ValidReview() =>
        new("bench", "metadata", "approved", "ok", null, "reviewer",
            DateTimeOffset.Parse("2026-08-14T12:00:00Z"), Hash);

    private static ProviderAttempt ValidAttempt() =>
        new("metadata", "bench", "openai", "model", "idem-valid", null, 1, DateTimeOffset.Parse("2026-08-14T12:00:00Z"),
            null, Hash, "metadata-v1", "started", new ProviderCost("USD", null, null));

    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
