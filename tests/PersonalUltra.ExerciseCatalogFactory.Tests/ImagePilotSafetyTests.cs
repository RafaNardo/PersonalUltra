using System.Net;
using System.Text.Json;
using Amazon.S3;
using PersonalUltra.ExerciseCatalogFactory.Images;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ImagePilotSafetyTests : IDisposable
{
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), $"personal-ultra-image-safety-{Guid.NewGuid():N}");

    [Fact]
    public async Task Existing_valid_manifest_is_accepted_without_touching_images()
    {
        var store = new ImagePilotStore(_workspace);
        var manifest = ValidManifest();
        await store.SaveAsync(manifest, default);

        var loaded = await store.LoadAsync(default);

        Assert.NotNull(loaded);
        Assert.Equal(manifest.Items[0].Slug, loaded.Items[0].Slug);
        Assert.False(Directory.Exists(store.FilesRoot));
    }

    [Fact]
    public async Task Manifest_rejects_unknown_version_before_artifact_access()
    {
        var store = new ImagePilotStore(_workspace);
        await WriteRawManifestAsync(store, ValidManifest() with { Version = 2 });

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(default));

        Assert.Contains("Versão", exception.Message);
        Assert.False(Directory.Exists(store.FilesRoot));
    }

    [Fact]
    public async Task Manifest_rejects_duplicate_or_malformed_slugs()
    {
        var store = new ImagePilotStore(_workspace);
        var item = ValidManifest().Items[0];
        await WriteRawManifestAsync(store, ValidManifest() with { Items = [item, item] });
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(default));

        await WriteRawManifestAsync(store, ValidManifest() with
        {
            Items = [item with { Slug = "../escape", LocalFile = "files/../escape.png" }]
        });
        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(default));
        Assert.False(Directory.Exists(store.FilesRoot));
    }

    [Theory]
    [InlineData("../agachamento-frontal-com-barra.png")]
    [InlineData("files/../agachamento-frontal-com-barra.png")]
    [InlineData("files\\agachamento-frontal-com-barra.png")]
    [InlineData("files/outro.png")]
    public async Task Manifest_requires_exact_safe_local_file(string localFile)
    {
        var store = new ImagePilotStore(_workspace);
        var item = ValidManifest().Items[0] with { LocalFile = localFile };
        await WriteRawManifestAsync(store, ValidManifest() with { Items = [item] });

        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(default));

        Assert.False(Directory.Exists(store.FilesRoot));
    }

    [Fact]
    public void S3_image_put_is_conditional_and_412_is_a_safe_collision()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var request = S3ObjectStore.CreatePutRequest(
            "bucket", ObjectKey.CreateCatalogImage("cadeira-extensora"), stream,
            "image/png", new string('a', 64));

        Assert.Equal("*", request.IfNoneMatch);
        Assert.Equal("exercise-catalog/v1/cadeira-extensora.png", request.Key);

        var providerFailure = new AmazonS3Exception("provider details must not escape")
        {
            StatusCode = HttpStatusCode.PreconditionFailed
        };
        var translated = S3ObjectStore.TranslatePutFailure(providerFailure);

        var collision = Assert.IsType<BucketObjectCollisionException>(translated);
        Assert.Contains("nenhuma sobrescrita", collision.Message);
        Assert.DoesNotContain("provider details", collision.Message);
        Assert.DoesNotContain("cadeira-extensora", collision.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace)) Directory.Delete(_workspace, recursive: true);
    }

    private static ImagePilotManifest ValidManifest()
    {
        var item = new ImagePilotItem(
            "Agachamento frontal com barra",
            "agachamento-frontal-com-barra",
            "prompt ULTRA",
            "files/agachamento-frontal-com-barra.png");
        return new ImagePilotManifest(1, "gpt-image-2", "1024x1024", "low",
            "personal-ultra-exercise-image-v2", 0.02m, [item]);
    }

    private static async Task WriteRawManifestAsync(ImagePilotStore store, ImagePilotManifest manifest)
    {
        Directory.CreateDirectory(store.Root);
        await File.WriteAllTextAsync(store.ManifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
