namespace PersonalUltra.ExerciseCatalogFactory.Domain;

public static class ManifestLifecycle
{
    public static ManifestItem ReconcileHashes(ManifestItem item, ItemStageHashes current)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(current);

        if (!Same(item.Hashes.Source, current.Source))
            return Reset(item, current with { NormalizationInput = null, MetadataInput = null, ImageInput = null, ExportInput = null }, "imported", [], []);
        if (!Same(item.Hashes.NormalizationInput, current.NormalizationInput))
            return Reset(item, current with { MetadataInput = null, ImageInput = null, ExportInput = null }, "imported", KeepBefore(item, "normalization"), []);
        if (!Same(item.Hashes.MetadataInput, current.MetadataInput))
            return Reset(item, current with { ImageInput = null, ExportInput = null }, "metadata_pending", KeepBefore(item, "metadata"), []);
        if (!Same(item.Hashes.ImageInput, current.ImageInput))
            return Reset(item, current with { ExportInput = null }, "image_pending", KeepBefore(item, "image"),
                (item.Reviews ?? []).Where(review => review.Stage == "metadata").ToArray());
        if (!Same(item.Hashes.ExportInput, current.ExportInput))
            return Reset(item, current, "approved", KeepBefore(item, "export"), item.Reviews ?? []);

        return item;
    }

    private static ManifestItem Reset(ManifestItem item, ItemStageHashes hashes, string state,
        IReadOnlyList<ArtifactReference> artifacts, IReadOnlyList<ReviewDecision> reviews) =>
        item with { State = state, Hashes = hashes, Artifacts = artifacts, Reviews = reviews };

    private static IReadOnlyList<ArtifactReference> KeepBefore(ManifestItem item, string invalidStage)
    {
        var order = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["source"] = 0, ["normalization"] = 1, ["metadata"] = 2, ["image"] = 3, ["export"] = 4
        };
        var threshold = order[invalidStage];
        return (item.Artifacts ?? []).Where(artifact =>
            order.TryGetValue(artifact.Stage, out var position) && position < threshold).ToArray();
    }

    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.Ordinal);
}
