using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Normalization;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class CatalogNormalizationTests
{
    [Theory]
    [InlineData("  Elevação   pélvica com BARRA ", "elevacao-pelvica-com-barra")]
    [InlineData("Leg press 45°", "leg-press-45")]
    [InlineData("Tríceps francês", "triceps-frances")]
    public void Slug_is_accentless_kebab_case_and_deterministic(string name, string expected) =>
        Assert.Equal(expected, CatalogNormalizer.Slugify(name));

    [Theory]
    [InlineData("Halter", "Halteres")]
    [InlineData("  HALTERES ", "Halteres")]
    [InlineData("Polia", "Cabo")]
    [InlineData("Máquina", "Máquina")]
    public void Equipment_aliases_are_normalized_deterministically(string input, string expected) =>
        Assert.Equal(expected, CatalogNormalizer.NormalizeEquipment(input));

    [Fact]
    public void Reordering_input_does_not_change_external_key_slug_or_target_id()
    {
        var first = Row("Supino inclinado com halteres", 1);
        var second = Row("Pallof press em pé", 2);
        var normalizer = new CatalogNormalizer();

        var catalogA = normalizer.Normalize([first, second], "a.json", Hash('a'));
        var catalogB = normalizer.Normalize([second with { Row = 1 }, first with { Row = 2 }], "a.json", Hash('a'));

        Assert.Equal(
            catalogA.Items.Select(IdentityTuple),
            catalogB.Items.Select(IdentityTuple));
    }

    [Fact]
    public void Degree_symbol_collision_is_blocking_and_never_gets_random_suffix()
    {
        var catalog = new CatalogNormalizer().Normalize(
            [Row("Leg press 45", 1), Row("Leg press 45°", 2)], "collision.json", Hash('b'));

        Assert.All(catalog.Items, item => Assert.Equal("leg-press-45", item.Slug));
        Assert.Contains(catalog.Issues, issue => issue.Code == "slug-collision" && issue.ExternalKeys.Count == 2);
        Assert.All(catalog.Items, item => Assert.Equal("needs_review", item.State));
    }

    [Fact]
    public void New_target_id_is_uuid_v5_and_stable()
    {
        var item = Assert.Single(new CatalogNormalizer().Normalize(
            [Row("Pallof press em pé", 1)], "input.json", Hash('c')).Items);

        Assert.Equal(5, item.TargetId.Version);
        Assert.Equal(UuidV5.Create(CatalogNormalizer.PersonalUltraExerciseNamespace, "pallof-press-em-pe"), item.TargetId);
        Assert.False(item.PreservesLegacyIdentity);
    }

    [Fact]
    public void Known_ambiguity_is_reported_without_merging_or_reusing_legacy_id()
    {
        var item = Assert.Single(new CatalogNormalizer().Normalize(
            [Row("Levantamento terra romeno com barra", 1)], "input.json", Hash('d')).Items);

        Assert.Equal("needs_review", item.State);
        Assert.False(item.PreservesLegacyIdentity);
        Assert.NotEqual(LegacyCatalog.Identities.Single(x => x.Slug == "stiff-com-barra").Id, item.TargetId);
    }

    private static CatalogInputRow Row(string name, int row) => new(
        new CatalogInputItem(null, name, null, "Peito", "Halter", null, null, null), row);

    private static (string, string, Guid) IdentityTuple(NormalizedExercise item) =>
        (item.ExternalKey, item.Slug, item.TargetId);

    private static string Hash(char value) => new(value, 64);
}
