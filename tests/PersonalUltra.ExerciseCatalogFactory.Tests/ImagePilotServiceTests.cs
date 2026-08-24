using PersonalUltra.ExerciseCatalogFactory.Images;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ImagePilotServiceTests : IDisposable
{
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), $"personal-ultra-images-{Guid.NewGuid():N}");
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3];

    [Fact]
    public async Task Plan_is_deterministic_and_makes_no_provider_call()
    {
        var provider = new FakeProvider();
        var service = new ImagePilotService(FactorySettingsTests.CreateSettings(_workspace), provider);

        var first = await service.PlanAsync(10, 1m, default);
        var second = await service.PlanAsync(10, 1m, default);

        Assert.Equal(10, first.Items.Count);
        Assert.Equal(first.Items.Select(item => item.Slug), second.Items.Select(item => item.Slug));
        Assert.Equal("agachamento-frontal-com-barra", first.Items[0].Slug);
        Assert.Equal("personal-ultra-exercise-image-v2", first.PromptVersion);
        Assert.All(first.Items, item =>
        {
            Assert.Contains("ULTRA", item.Prompt);
            Assert.Contains("#080808", item.Prompt);
            Assert.Contains("#151515", item.Prompt);
            Assert.Contains("#222220", item.Prompt);
            Assert.Contains("#FF6A13", item.Prompt);
            Assert.Contains("levemente mais claro e legível", item.Prompt);
            Assert.Contains("equipamentos visíveis", item.Prompt);
            Assert.Contains("preenchimento neutro suave", item.Prompt);
            Assert.DoesNotContain("vinho", item.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("vermelho", item.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sem músculos expostos", item.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Rosto natural e humano", item.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sem aparência plástica", item.Prompt, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Regenerate_replaces_only_requested_image_and_archives_previous_file()
    {
        var provider = new FakeProvider();
        var service = new ImagePilotService(FactorySettingsTests.CreateSettings(_workspace), provider);
        var generated = await service.GenerateAsync(2, 1m, true, default);
        var target = generated.Manifest.Items[0];

        var regenerated = await service.RegenerateAsync(target.Slug, 1m, true, default);

        Assert.Equal(3, provider.Calls);
        Assert.False(regenerated.Approved);
        Assert.False(regenerated.Uploaded);
        Assert.Contains("sem músculos expostos", regenerated.Prompt, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(
            _workspace,
            "images",
            "v2",
            "rejected",
            $"{target.Slug}.{target.Sha256![..12]}.png")));
    }

    [Fact]
    public async Task Generate_checkpoints_each_file_and_resume_skips_it()
    {
        var provider = new FakeProvider();
        var progress = new List<string>();
        var service = new ImagePilotService(
            FactorySettingsTests.CreateSettings(_workspace),
            provider,
            progress: message =>
            {
                progress.Add(message);
                return Task.CompletedTask;
            });

        var generated = await service.GenerateAsync(2, 1m, true, default);
        var resumed = await service.GenerateAsync(2, 1m, true, default);

        Assert.Equal(2, generated.Generated);
        Assert.Equal(2, resumed.Skipped);
        Assert.Equal(2, provider.Calls);
        Assert.Contains(progress, message => message.StartsWith("[1/2] Gerando:", StringComparison.Ordinal));
        Assert.Contains(progress, message => message.StartsWith("[2/2] Concluída:", StringComparison.Ordinal));
        Assert.Contains(progress, message => message.Contains("Preservada:", StringComparison.Ordinal));
        Assert.All(resumed.Manifest.Items, item => Assert.NotNull(item.Sha256));
        Assert.All(resumed.Manifest.Items, item => Assert.True(File.Exists(Path.Combine(_workspace, "images", "v2", item.LocalFile))));
    }

    [Fact]
    public async Task Approve_and_upload_only_selected_generated_images()
    {
        var provider = new FakeProvider();
        var store = new FakeObjectStore();
        var settings = FactorySettingsTests.CreateSettings(_workspace);
        var service = new ImagePilotService(settings, provider, store);
        var generated = await service.GenerateAsync(2, 1m, true, default);
        var approvals = Path.Combine(_workspace, "approved.txt");
        Directory.CreateDirectory(_workspace);
        await File.WriteAllTextAsync(approvals, generated.Manifest.Items[1].Slug);

        var approved = await service.ApproveAsync(approvals, default);
        var uploaded = await service.UploadAsync(true, default);

        Assert.Single(approved.Items, item => item.Approved);
        Assert.Equal(1, uploaded.Uploaded);
        Assert.Single(store.Keys);
        Assert.Equal($"exercise-catalog/v2/{generated.Manifest.Items[1].Slug}.png", store.Keys[0]);
    }

    [Fact]
    public async Task Full_batch_expands_pilot_preserving_checkpoints_and_excludes_review_items()
    {
        var provider = new FakeProvider();
        var service = new ImagePilotService(FactorySettingsTests.CreateSettings(_workspace), provider);
        var pilot = await service.GenerateAsync(10, 1m, true, default);
        var checkpoint = pilot.Manifest.Items[0];

        var full = await service.PlanAsync(int.MaxValue, 5m, default);

        Assert.Equal(220, full.Items.Count);
        Assert.Equal(10, full.Items.Count(item => item.Sha256 is not null));
        Assert.Equal(checkpoint, full.Items[0]);
        Assert.DoesNotContain(full.Items, item => item.Slug == "afundo-com-halteres");
        Assert.Equal(full.Items.Count, full.Items.Select(item => item.Slug).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(10, provider.Calls);
    }

    [Fact]
    public async Task Full_batch_budget_counts_only_pending_images()
    {
        var provider = new FakeProvider();
        var service = new ImagePilotService(FactorySettingsTests.CreateSettings(_workspace), provider);
        await service.GenerateAsync(10, 1m, true, default);

        var manifest = await service.PlanAsync(int.MaxValue, 4.20m, default);

        Assert.Equal(220, manifest.Items.Count);
        await Assert.ThrowsAsync<ArgumentException>(() => service.PlanAsync(int.MaxValue, 4.19m, default));
    }

    [Fact]
    public async Task Dry_runs_never_call_external_providers()
    {
        var provider = new FakeProvider();
        var store = new FakeObjectStore();
        var service = new ImagePilotService(FactorySettingsTests.CreateSettings(_workspace), provider, store);

        await service.GenerateAsync(2, 1m, false, default);
        await service.UploadAsync(false, default);

        Assert.Equal(0, provider.Calls);
        Assert.Empty(store.Keys);
    }

    [Fact]
    public async Task V2_plan_preserves_v1_archive_and_v1_cannot_be_approved_or_uploaded()
    {
        var archiveRoot = Path.Combine(_workspace, "images");
        var archiveFiles = Path.Combine(archiveRoot, "files");
        Directory.CreateDirectory(archiveFiles);
        var archiveManifest = Path.Combine(archiveRoot, "manifest.v1.json");
        var archiveImage = Path.Combine(archiveFiles, "reference.png");
        await File.WriteAllTextAsync(archiveManifest, "v1-reference-manifest");
        await File.WriteAllBytesAsync(archiveImage, Png);
        var manifestBefore = await File.ReadAllBytesAsync(archiveManifest);
        var imageBefore = await File.ReadAllBytesAsync(archiveImage);

        var v2Service = new ImagePilotService(FactorySettingsTests.CreateSettings(_workspace));
        await v2Service.PlanAsync(2, 1m, default);

        Assert.Equal(manifestBefore, await File.ReadAllBytesAsync(archiveManifest));
        Assert.Equal(imageBefore, await File.ReadAllBytesAsync(archiveImage));
        Assert.True(File.Exists(Path.Combine(_workspace, "images", "v2", "manifest.v1.json")));

        var objectStore = new FakeObjectStore();
        var v1Service = new ImagePilotService(
            FactorySettingsTests.CreateSettings(_workspace, imagePromptVersion: "personal-ultra-exercise-image-v1"),
            objectStore: objectStore);
        await Assert.ThrowsAsync<InvalidOperationException>(() => v1Service.ApproveAsync("unused.txt", default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => v1Service.UploadAsync(true, default));
        Assert.Empty(objectStore.Keys);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace)) Directory.Delete(_workspace, recursive: true);
    }

    private sealed class FakeProvider : IImageProvider
    {
        internal int Calls { get; private set; }
        public Task<GeneratedImage> GenerateAsync(string model, string prompt, string size, string quality, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new GeneratedImage(Png, "req_fake"));
        }
    }

    private sealed class FakeObjectStore : IObjectStore
    {
        internal List<string> Keys { get; } = [];
        private readonly Dictionary<string, ObjectMetadata> _metadata = [];
        public Task<ObjectStoreResult> PutAsync(ObjectKey key, ReadOnlyMemory<byte> content, string contentType, string sha256, CancellationToken cancellationToken)
        {
            Keys.Add(key.Value);
            _metadata[key.Value] = new ObjectMetadata(content.Length, contentType, sha256, null);
            return Task.FromResult(new ObjectStoreResult(null));
        }
        public Task<ObjectMetadata?> HeadAsync(ObjectKey key, CancellationToken cancellationToken) => Task.FromResult(_metadata.GetValueOrDefault(key.Value));
        public Task<ObjectStoreResult> ProbeAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ObjectContent> GetAsync(ObjectKey key, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Uri CreatePresignedGetUri(ObjectKey key, DateTimeOffset expiresAt) => throw new NotSupportedException();
        public Task<ObjectStoreResult> DeleteAsync(ObjectKey key, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
