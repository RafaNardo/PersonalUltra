using PersonalUltra.ExerciseCatalogFactory.Normalization;
using System.Text.RegularExpressions;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class LegacyCatalogTests
{
    private static readonly string[] ExpectedSlugs =
    [
        "supino-reto-com-barra", "afundo-com-halteres", "ponte-de-gluteo-unilateral", "remada-baixa",
        "puxada-dorsal-na-maquina", "pull-through-no-cabo", "desenvolvimento-com-halteres",
        "elevacao-lateral-com-halteres", "triceps-na-polia-com-corda", "rosca-direta-com-barra",
        "agachamento-livre", "agachamento-goblet", "agachamento-sumo", "cadeira-extensora",
        "cadeira-flexora", "leg-press-45", "passada-com-halteres", "step-up-com-halteres",
        "stiff-com-barra", "levantamento-terra-romeno", "abducao-com-elastico",
        "abducao-de-quadril-na-maquina", "coice-com-caneleira", "coice-no-cabo",
        "elevacao-pelvica-com-barra", "elevacao-pelvica-unilateral-com-barra",
        "ponte-de-gluteos", "frog-pump"
    ];

    [Fact]
    public void All_28_legacy_slugs_and_guids_are_preserved_exactly()
    {
        Assert.Equal(ExpectedSlugs, LegacyCatalog.Identities.Select(identity => identity.Slug));
        Assert.Equal(28, LegacyCatalog.Identities.Count);
        for (var index = 0; index < LegacyCatalog.Identities.Count; index++)
            Assert.Equal(Guid.Parse($"10000000-0000-0000-0000-{index + 1:000000000000}"), LegacyCatalog.Identities[index].Id);
    }

    [Fact]
    public void Exact_legacy_slug_reuses_id_but_does_not_mutate_registry()
    {
        var source = LegacyCatalog.Identities.Single(identity => identity.Slug == "supino-reto-com-barra");
        var catalog = new CatalogNormalizer().Normalize(
            [new(new(null, "Supino reto com barra", null, "Peito", "Barra", null, null, null), 1)],
            "input.json", new string('a', 64));
        var item = Assert.Single(catalog.Items);

        Assert.True(item.PreservesLegacyIdentity);
        Assert.Equal(source.Id, item.TargetId);
        Assert.Equal(source.Slug, item.Slug);
        Assert.Equal(28, catalog.PreservedLegacyIdentities.Count);
        Assert.Single(catalog.MatchedLegacyIdentities);
        Assert.Equal(27, catalog.UnresolvedLegacyIdentities.Count);
    }

    [Fact]
    public void Frozen_profile_matches_the_current_target_seed_exactly()
    {
        var seedPath = FindRepositoryFile("apps", "backend", "PersonalUltra.Infrastructure", "Infrastructure", "ExerciseCatalogSeed.cs");
        var matches = Regex.Matches(File.ReadAllText(seedPath),
            "new\\(\"(?<id>10000000-[^\"]+)\", \"(?<name>[^\"]+)\", \"(?<slug>[^\"]+)\",");

        Assert.Equal(28, matches.Count);
        Assert.Equal(
            LegacyCatalog.Identities.Select(identity => (identity.Id, identity.Name, identity.Slug)),
            matches.Select(match =>
                (Guid.Parse(match.Groups["id"].Value), match.Groups["name"].Value, match.Groups["slug"].Value)));
    }

    private static string FindRepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = parts.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(string.Join('/', parts));
    }
}
