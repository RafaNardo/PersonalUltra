using System.Text.RegularExpressions;
using PersonalUltra.ExerciseCatalogFactory.Intake;
using PersonalUltra.ExerciseCatalogFactory.Normalization;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ExerciseSeedExporterTests
{
    [Fact]
    public async Task Generated_seed_contains_every_new_normalized_item_with_stable_identity_and_media_ref()
    {
        var catalogPath = FindRepositoryFile("tools", "PersonalUltra.ExerciseCatalogFactory", "Inputs", "v1", "exercise-inventory-v1.csv");
        var seedPath = FindRepositoryFile("apps", "backend", "PersonalUltra.Infrastructure", "Infrastructure", "ExerciseCatalogSeed.Generated.cs");
        var rows = await CatalogInputReader.ReadAsync(catalogPath);
        var catalog = new CatalogNormalizer().Normalize(rows, Path.GetFileName(catalogPath), new string('a', 64));
        var legacy = LegacyCatalog.Identities.Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        var expected = catalog.Items.Where(item => item.State == "normalized" && !legacy.Contains(item.Slug))
            .OrderBy(item => item.Slug, StringComparer.Ordinal).ToArray();
        var matches = Regex.Matches(File.ReadAllText(seedPath),
            "new\\(\\\"(?<id>[0-9a-f-]+)\\\", \\\"(?<name>[^\\\"]+)\\\", \\\"(?<slug>[^\\\"]+)\\\", \\\"(?<group>[^\\\"]+)\\\", null, \\\"(?<image>media://exercise-catalog/v2/[^\\\"]+\\.png)\\\", null\\),");

        Assert.Equal(203, expected.Length);
        Assert.Equal(expected.Length, matches.Count);
        for (var index = 0; index < expected.Length; index++)
        {
            var source = expected[index];
            var generated = matches[index];
            Assert.Equal(source.TargetId, Guid.Parse(generated.Groups["id"].Value));
            Assert.Equal(source.CanonicalName, generated.Groups["name"].Value);
            Assert.Equal(source.Slug, generated.Groups["slug"].Value);
            Assert.Equal(source.PrimaryMuscleGroup, generated.Groups["group"].Value);
            Assert.Equal($"media://exercise-catalog/v2/{source.Slug}.png", generated.Groups["image"].Value);
        }
    }

    [Fact]
    public void Generated_seed_is_stably_sorted_and_never_duplicates_a_legacy_slug()
    {
        var seedPath = FindRepositoryFile("apps", "backend", "PersonalUltra.Infrastructure", "Infrastructure", "ExerciseCatalogSeed.Generated.cs");
        var slugs = Regex.Matches(File.ReadAllText(seedPath), "media://exercise-catalog/v2/(?<slug>[^\\\"]+)\\.png")
            .Select(match => match.Groups["slug"].Value).ToArray();

        Assert.Equal(slugs.Order(StringComparer.Ordinal), slugs);
        Assert.Empty(slugs.Intersect(LegacyCatalog.Identities.Select(item => item.Slug), StringComparer.Ordinal));
        Assert.Equal(slugs.Length, slugs.Distinct(StringComparer.Ordinal).Count());
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
