using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ManifestLifecycleTests
{
    private const string A = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string B = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string C = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string D = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string E = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

    [Fact]
    public void Same_hashes_are_an_idempotent_cache_hit()
    {
        var item = CreateItem();

        var result = ManifestLifecycle.ReconcileHashes(item, item.Hashes);

        Assert.Same(item, result);
    }

    [Fact]
    public void Metadata_hash_change_invalidates_only_metadata_and_downstream()
    {
        var item = CreateItem();
        var next = item.Hashes with { MetadataInput = A };

        var result = ManifestLifecycle.ReconcileHashes(item, next);

        Assert.Equal("metadata_pending", result.State);
        Assert.Equal(A, result.Hashes.MetadataInput);
        Assert.Null(result.Hashes.ImageInput);
        Assert.Null(result.Hashes.ExportInput);
        Assert.Equal(["source", "normalization"], result.Artifacts!.Select(x => x.Stage));
        Assert.Empty(result.Reviews!);
    }

    [Fact]
    public void Image_hash_change_preserves_metadata_approval_only()
    {
        var item = CreateItem();
        var result = ManifestLifecycle.ReconcileHashes(item, item.Hashes with { ImageInput = A });

        Assert.Equal("image_pending", result.State);
        Assert.Equal(["metadata"], result.Reviews!.Select(review => review.Stage));
        Assert.Equal(["source", "normalization", "metadata"], result.Artifacts!.Select(x => x.Stage));
    }

    private static ManifestItem CreateItem()
    {
        var artifacts = new[] { "source", "normalization", "metadata", "image", "export" }
            .Select(stage => new ArtifactReference(stage, $"artifacts/{stage}.json", A, 1)).ToArray();
        var reviews = new[] { "metadata", "visual", "biomechanics" }
            .Select(stage => new ReviewDecision("bench", stage, "approved", "ok", null, "reviewer",
                DateTimeOffset.Parse("2026-08-14T12:00:00Z"), A)).ToArray();
        return new ManifestItem("bench", "exported", new ItemSource("source/input.json", 2, A),
            new ItemStageHashes(A, B, C, D, E), artifacts, reviews);
    }
}
