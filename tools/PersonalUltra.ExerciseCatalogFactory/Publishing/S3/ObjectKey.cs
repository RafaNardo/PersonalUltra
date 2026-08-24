using System.Text.RegularExpressions;

namespace PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

internal sealed partial class ObjectKey
{
    private ObjectKey(string value) => Value = value;

    internal string Value { get; }

    internal static ObjectKey CreateSmoke(string runId, Guid objectId)
    {
        if (!RunIdPattern().IsMatch(runId))
        {
            throw new ArgumentException(
                "Run ID de smoke inválido; use 8–64 caracteres minúsculos, números ou hífen.",
                nameof(runId));
        }

        return new ObjectKey($"smoke/{runId}/{objectId:N}.txt");
    }

    internal static ObjectKey ParseSmoke(string value)
    {
        if (!SmokeKeyPattern().IsMatch(value))
        {
            throw new ArgumentException("Object key fora do escopo estrito de smoke.", nameof(value));
        }

        return new ObjectKey(value);
    }

    internal static ObjectKey CreateCatalogImage(string slug)
    {
        if (!SlugPattern().IsMatch(slug))
            throw new ArgumentException("Slug inválido para imagem do catálogo.", nameof(slug));
        return new ObjectKey($"exercise-catalog/v1/{slug}.png");
    }

    internal static ObjectKey CreateCatalogImageV2(string slug)
    {
        if (!SlugPattern().IsMatch(slug))
            throw new ArgumentException("Slug inválido para imagem do catálogo.", nameof(slug));
        return new ObjectKey($"exercise-catalog/v2/{slug}.png");
    }

    internal static ObjectKey CreateCatalogImageV3(string slug)
    {
        if (!SlugPattern().IsMatch(slug))
            throw new ArgumentException("Slug inválido para imagem do catálogo.", nameof(slug));
        return new ObjectKey($"exercise-catalog/v3/{slug}.png");
    }

    internal static ObjectKey CreateCatalogDeliveryV1(string slug)
    {
        if (!SlugPattern().IsMatch(slug))
            throw new ArgumentException("Slug inválido para imagem do catálogo.", nameof(slug));
        return new ObjectKey($"exercise-catalog/delivery/v1/{slug}.webp");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{7,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex RunIdPattern();

    [GeneratedRegex("^smoke/[a-z0-9][a-z0-9-]{7,63}/[0-9a-f]{32}\\.txt$", RegexOptions.CultureInvariant)]
    private static partial Regex SmokeKeyPattern();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
