using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalUltra.ExerciseCatalogFactory.Normalization;

namespace PersonalUltra.ExerciseCatalogFactory.Images;

internal sealed class ImagePilotStore(string workspaceRoot, string promptVersion = "personal-ultra-exercise-image-v2")
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    internal string Root { get; } = promptVersion == "personal-ultra-exercise-image-v2"
        ? Path.Combine(workspaceRoot, "images", "v2")
        : Path.Combine(workspaceRoot, "images", "unsupported", CatalogNormalizer.Slugify(promptVersion));
    internal string FilesRoot => Path.Combine(Root, "files");
    internal string ManifestPath => Path.Combine(Root, "manifest.v1.json");

    internal async Task<ImagePilotManifest?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ManifestPath)) return null;
        await using var stream = File.OpenRead(ManifestPath);
        ImagePilotManifest manifest;
        try
        {
            manifest = await JsonSerializer.DeserializeAsync<ImagePilotManifest>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException("Manifesto de imagens vazio.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Manifesto de imagens inválido.", exception);
        }
        Validate(manifest);
        return manifest;
    }

    internal async Task SaveAsync(ImagePilotManifest manifest, CancellationToken cancellationToken)
    {
        Validate(manifest);
        Directory.CreateDirectory(Root);
        var temporary = ManifestPath + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        File.Move(temporary, ManifestPath, overwrite: true);
    }

    internal void Validate(ImagePilotManifest manifest)
    {
        if (manifest.Version != 1) throw new InvalidDataException($"Versão de manifesto não suportada: {manifest.Version}.");
        if (!string.Equals(manifest.PromptVersion, promptVersion, StringComparison.Ordinal))
            throw new InvalidDataException("O manifesto pertence a outra versão de estilo.");
        if (manifest.Items is null || manifest.Items.Count < 1)
            throw new InvalidDataException("O manifesto deve conter ao menos um item.");

        var slugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in manifest.Items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Slug))
                throw new InvalidDataException("Item ou slug vazio no manifesto.");
            if (!slugs.Add(item.Slug)) throw new InvalidDataException($"Slug duplicado no manifesto: {item.Slug}");
            if (CatalogNormalizer.Slugify(item.Slug) != item.Slug)
                throw new InvalidDataException($"Slug inesperado no manifesto: {item.Slug}");
            if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 200 || item.Name.Any(char.IsControl))
                throw new InvalidDataException($"Nome inválido no manifesto: {item.Slug}");

            var expectedRelative = $"files/{item.Slug}.png";
            if (!string.Equals(item.LocalFile, expectedRelative, StringComparison.Ordinal))
                throw new InvalidDataException($"LocalFile inválido para {item.Slug}.");
            var resolved = Path.GetFullPath(Path.Combine(Root, item.LocalFile.Replace('/', Path.DirectorySeparatorChar)));
            var expectedResolved = Path.GetFullPath(Path.Combine(FilesRoot, $"{item.Slug}.png"));
            if (!string.Equals(resolved, expectedResolved, StringComparison.OrdinalIgnoreCase) ||
                !resolved.StartsWith(Path.GetFullPath(FilesRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"LocalFile escaparia da pasta de imagens para {item.Slug}.");
        }
    }
}
