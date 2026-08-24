using System.Security.Cryptography;
using System.Text;
using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Intake;
using PersonalUltra.ExerciseCatalogFactory.Normalization;

namespace PersonalUltra.ExerciseCatalogFactory.Images;

internal sealed class ExerciseSeedExporter(
    FactorySettings settings,
    string workspaceRoot,
    string? outputPath = null)
{
    internal async Task<SeedExportResult> ExportAsync(bool execute, CancellationToken cancellationToken)
    {
        var manifest = await new ImagePilotStore(workspaceRoot, settings.ImagePromptVersion).LoadAsync(cancellationToken)
            ?? throw new InvalidOperationException("Manifesto de imagens não encontrado.");
        var rows = await CatalogInputReader.ReadAsync(settings.ImageCatalogPath, cancellationToken);
        var sourceBytes = await File.ReadAllBytesAsync(settings.ImageCatalogPath, cancellationToken);
        var catalog = new CatalogNormalizer().Normalize(
            rows, Path.GetFileName(settings.ImageCatalogPath), Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant());
        var normalized = catalog.Items.Where(item => item.State == "normalized").ToArray();

        ValidateManifest(manifest, normalized);
        await ValidateFilesAsync(manifest, cancellationToken);

        var legacySlugs = LegacyCatalog.Identities.Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        var manifestBySlug = manifest.Items.ToDictionary(item => item.Slug, StringComparer.Ordinal);
        var generated = normalized
            .Where(item => !legacySlugs.Contains(item.Slug))
            .OrderBy(item => item.Slug, StringComparer.Ordinal)
            .Select(item => new GeneratedSeed(
                item.TargetId,
                item.CanonicalName,
                item.Slug,
                item.PrimaryMuscleGroup ?? throw new InvalidDataException($"Grupo ausente no item normalizado: {item.Slug}"),
                item.Equipment,
                $"media://{manifestBySlug[item.Slug].ObjectKey}",
                null))
            .ToArray();
        var target = outputPath ?? FindOutputPath(settings.ImageCatalogPath);
        var source = Render(generated);
        if (execute) await WriteAtomicallyAsync(target, source, cancellationToken);
        return new SeedExportResult(normalized.Length, LegacyCatalog.Identities.Count, generated.Length, target);
    }

    private static void ValidateManifest(ImagePilotManifest manifest, IReadOnlyList<NormalizedExercise> normalized)
    {
        if (manifest.Items.Count != normalized.Count)
            throw new InvalidDataException($"Manifesto incompleto: esperado={normalized.Count}; encontrado={manifest.Items.Count}.");
        var normalizedBySlug = normalized.ToDictionary(item => item.Slug, StringComparer.Ordinal);
        foreach (var item in manifest.Items)
        {
            if (!normalizedBySlug.TryGetValue(item.Slug, out var source) || source.CanonicalName != item.Name)
                throw new InvalidDataException($"Slug/nome do manifesto diverge do catálogo: {item.Slug}.");
            if (!item.Approved || !item.Uploaded)
                throw new InvalidDataException($"Imagem ainda não aprovada e publicada: {item.Slug}.");
            if (item.Sha256 is null || item.Sha256.Length != 64 || item.Sha256.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException($"SHA-256 ausente ou inválido: {item.Slug}.");
            var expectedKey = $"exercise-catalog/v2/{item.Slug}.png";
            if (!string.Equals(item.ObjectKey, expectedKey, StringComparison.Ordinal))
                throw new InvalidDataException($"Object key divergente para {item.Slug}.");
        }
    }

    private async Task ValidateFilesAsync(ImagePilotManifest manifest, CancellationToken cancellationToken)
    {
        foreach (var item in manifest.Items)
        {
            var path = Path.Combine(workspaceRoot, "images", "v2", item.LocalFile.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) throw new InvalidDataException($"Imagem local ausente: {item.Slug}.");
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!string.Equals(hash, item.Sha256, StringComparison.Ordinal))
                throw new InvalidDataException($"Hash local diverge do manifesto: {item.Slug}.");
        }
    }

    private static string FindOutputPath(string catalogPath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(catalogPath))!);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "apps", "backend", "PersonalUltra.Infrastructure", "Infrastructure", "ExerciseCatalogSeed.Generated.cs");
            if (Directory.Exists(Path.GetDirectoryName(candidate))) return candidate;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Raiz do repositório não encontrada para gerar ExerciseCatalogSeed.Generated.cs.");
    }

    private static async Task WriteAtomicallyAsync(string path, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, content, new UTF8Encoding(false), cancellationToken);
        File.Move(temporary, path, true);
    }

    private static string Render(IEnumerable<GeneratedSeed> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// Generated by: images seed --execute. Do not edit manually.");
        builder.AppendLine("namespace PersonalUltra.Infrastructure;");
        builder.AppendLine();
        builder.AppendLine("internal static class ExerciseCatalogSeedGenerated");
        builder.AppendLine("{");
        builder.AppendLine("    internal static readonly IReadOnlyList<ExerciseCatalogSeed.ExerciseSeed> Exercises =");
        builder.AppendLine("    [");
        foreach (var item in items)
            builder.AppendLine($"        new(\"{item.Id:D}\", \"{Escape(item.Name)}\", \"{item.Slug}\", \"{Escape(item.Group)}\", {Literal(item.Equipment)}, \"{item.ImageRef}\", {Literal(item.Instructions)}),");
        builder.AppendLine("    ];");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string Literal(string? value) => value is null ? "null" : $"\"{Escape(value)}\"";
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

    private sealed record GeneratedSeed(Guid Id, string Name, string Slug, string Group, string? Equipment, string ImageRef, string? Instructions);
}

internal sealed record SeedExportResult(int NormalizedCount, int LegacyCount, int GeneratedCount, string OutputPath);
