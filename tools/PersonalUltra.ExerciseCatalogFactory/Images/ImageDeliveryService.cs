using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Normalization;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;
using SkiaSharp;

namespace PersonalUltra.ExerciseCatalogFactory.Images;

internal sealed class ImageDeliveryService(
    FactorySettings settings,
    IObjectStore? objectStore = null,
    string? workspaceRoot = null,
    Func<string, Task>? progress = null)
{
    private const int Edge = 640;
    private const int Quality = 78;
    private readonly string _workspaceRoot = workspaceRoot ?? settings.WorkspaceRoot;
    private readonly Func<string, Task> _progress = progress ?? (_ => Task.CompletedTask);
    private readonly string _root = Path.Combine(workspaceRoot ?? settings.WorkspaceRoot, "images", "delivery-v1");
    private string FilesRoot => Path.Combine(_root, "files");
    private string ManifestPath => Path.Combine(_root, "manifest.v1.json");

    internal async Task<DeliveryResult> RunAsync(bool execute, CancellationToken cancellationToken)
    {
        var sources = await LoadSourcesAsync(cancellationToken);
        var manifest = await LoadManifestAsync(cancellationToken) ?? new DeliveryManifest(
            1, Edge, Quality,
            sources.Select(source => new DeliveryItem(
                source.Slug, source.PromptVersion, source.Sha256,
                $"files/{source.Slug}.webp", null, false)).ToArray());
        ValidateManifest(manifest, sources);
        if (!execute)
            return new DeliveryResult(sources.Count, manifest.Items.Count(item => item.Uploaded), 0, 0,
                manifest.Items.Where(item => item.DeliverySha256 is not null).Sum(item => new FileInfo(Path.Combine(_root, item.LocalFile)).Length));

        Directory.CreateDirectory(FilesRoot);
        var actualStore = objectStore ?? new S3ObjectStore(settings.GetBucketOptions(), settings.GetBucketCredentials());
        var ownsStore = objectStore is null;
        var converted = 0;
        var uploaded = 0;
        try
        {
            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                var item = manifest.Items.Single(value => value.Slug == source.Slug);
                var targetPath = Path.Combine(_root, item.LocalFile);
                byte[] deliveryBytes;
                if (item.DeliverySha256 is not null)
                {
                    deliveryBytes = await File.ReadAllBytesAsync(targetPath, cancellationToken);
                    if (Sha256(deliveryBytes) != item.DeliverySha256)
                        throw new InvalidDataException($"Derivado local alterado: {item.LocalFile}.");
                }
                else
                {
                    await _progress($"[{index + 1}/{sources.Count}] Otimizando: {source.Slug}...");
                    deliveryBytes = CreateWebp(await File.ReadAllBytesAsync(source.Path, cancellationToken));
                    await File.WriteAllBytesAsync(targetPath, deliveryBytes, cancellationToken);
                    item = item with { DeliverySha256 = Sha256(deliveryBytes) };
                    manifest = Replace(manifest, item);
                    await SaveManifestAsync(manifest, cancellationToken);
                    converted++;
                }

                if (item.Uploaded) continue;
                var key = ObjectKey.CreateCatalogDeliveryV1(item.Slug);
                var existing = await actualStore.HeadAsync(key, cancellationToken);
                if (existing is not null)
                {
                    if (existing.Length != deliveryBytes.Length || existing.Sha256 != item.DeliverySha256)
                        throw new InvalidDataException($"Já existe outro objeto na chave de entrega: {key.Value}");
                }
                else
                {
                    await actualStore.PutAsync(key, deliveryBytes, "image/webp", item.DeliverySha256!, cancellationToken);
                    var verified = await actualStore.HeadAsync(key, cancellationToken);
                    if (verified is null || verified.Length != deliveryBytes.Length || verified.Sha256 != item.DeliverySha256)
                        throw new InvalidDataException($"Derivado publicado não pôde ser verificado: {item.Slug}");
                    uploaded++;
                }

                item = item with { Uploaded = true };
                manifest = Replace(manifest, item);
                await SaveManifestAsync(manifest, cancellationToken);
                await _progress($"[{index + 1}/{sources.Count}] Entrega pronta: {source.Slug} ({deliveryBytes.Length / 1024:N0} KB).");
            }
        }
        finally
        {
            if (ownsStore) await actualStore.DisposeAsync();
        }

        var totalBytes = manifest.Items.Sum(item => new FileInfo(Path.Combine(_root, item.LocalFile)).Length);
        return new DeliveryResult(sources.Count, manifest.Items.Count(item => item.Uploaded), converted, uploaded, totalBytes);
    }

    private async Task<IReadOnlyList<DeliverySource>> LoadSourcesAsync(CancellationToken cancellationToken)
    {
        var v2 = await new ImagePilotStore(_workspaceRoot, "personal-ultra-exercise-image-v2").LoadAsync(cancellationToken)
            ?? throw new InvalidOperationException("Manifesto v2 ausente.");
        var v3 = await new ImagePilotStore(_workspaceRoot, "personal-ultra-exercise-image-v3").LoadAsync(cancellationToken)
            ?? throw new InvalidOperationException("Manifesto v3 ausente.");
        RequirePublished(v2);
        RequirePublished(v3);
        var legacy = LegacyCatalog.Identities.Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        var sources = v2.Items.Where(item => !legacy.Contains(item.Slug)).Select(item => Source("v2", v2.PromptVersion, item))
            .Concat(v3.Items.Select(item => Source("v3", v3.PromptVersion, item)))
            .OrderBy(item => item.Slug, StringComparer.Ordinal)
            .ToArray();
        if (sources.Length != 231 || sources.Select(item => item.Slug).Distinct(StringComparer.Ordinal).Count() != 231)
            throw new InvalidDataException($"Catálogo de entrega deveria conter 231 slugs únicos; encontrado={sources.Length}.");
        return sources;
    }

    private DeliverySource Source(string folder, string promptVersion, ImagePilotItem item)
    {
        var path = Path.Combine(_workspaceRoot, "images", folder, item.LocalFile.Replace('/', Path.DirectorySeparatorChar));
        if (item.Sha256 is null || !File.Exists(path)) throw new InvalidDataException($"Master ausente: {item.Slug}.");
        return new DeliverySource(item.Slug, promptVersion, item.Sha256, path);
    }

    private static void RequirePublished(ImagePilotManifest manifest)
    {
        var incomplete = manifest.Items.FirstOrDefault(item => !item.Approved || !item.Uploaded || item.Sha256 is null);
        if (incomplete is not null)
            throw new InvalidOperationException($"Master ainda não aprovado/publicado: {incomplete.Slug}.");
    }

    internal static byte[] CreateWebp(byte[] source)
    {
        using var bitmap = SKBitmap.Decode(source) ?? throw new InvalidDataException("Master não pôde ser decodificado.");
        using var resized = new SKBitmap(new SKImageInfo(Edge, Edge, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (!bitmap.ScalePixels(resized, new SKSamplingOptions(SKCubicResampler.Mitchell)))
            throw new InvalidDataException("Master não pôde ser redimensionado.");
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, Quality)
            ?? throw new InvalidDataException("Derivado WebP não pôde ser codificado.");
        return encoded.ToArray();
    }

    private async Task<DeliveryManifest?> LoadManifestAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ManifestPath)) return null;
        await using var stream = File.OpenRead(ManifestPath);
        return await JsonSerializer.DeserializeAsync<DeliveryManifest>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("Manifesto de entrega vazio.");
    }

    private async Task SaveManifestAsync(DeliveryManifest manifest, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        var temporary = ManifestPath + ".tmp";
        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        File.Move(temporary, ManifestPath, true);
    }

    private static void ValidateManifest(DeliveryManifest manifest, IReadOnlyList<DeliverySource> sources)
    {
        if (manifest.Version != 1 || manifest.Edge != Edge || manifest.Quality != Quality || manifest.Items.Count != sources.Count)
            throw new InvalidDataException("Manifesto de entrega incompatível.");
        foreach (var source in sources)
        {
            var item = manifest.Items.SingleOrDefault(value => value.Slug == source.Slug)
                ?? throw new InvalidDataException($"Slug ausente no manifesto de entrega: {source.Slug}.");
            if (item.SourceSha256 != source.Sha256 || item.SourcePromptVersion != source.PromptVersion ||
                item.LocalFile != $"files/{source.Slug}.webp")
                throw new InvalidDataException($"Fonte mudou para o derivado: {source.Slug}.");
        }
    }

    private static DeliveryManifest Replace(DeliveryManifest manifest, DeliveryItem item) =>
        manifest with { Items = manifest.Items.Select(value => value.Slug == item.Slug ? item : value).ToArray() };

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private sealed record DeliverySource(string Slug, string PromptVersion, string Sha256, string Path);
}

internal sealed record DeliveryManifest(int Version, int Edge, int Quality, IReadOnlyList<DeliveryItem> Items);
internal sealed record DeliveryItem(string Slug, string SourcePromptVersion, string SourceSha256, string LocalFile, string? DeliverySha256, bool Uploaded);
internal sealed record DeliveryResult(int Total, int Ready, int Converted, int Uploaded, long TotalBytes);
