using System.Security.Cryptography;
using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Intake;
using PersonalUltra.ExerciseCatalogFactory.Normalization;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

namespace PersonalUltra.ExerciseCatalogFactory.Images;

internal sealed class ImagePilotService(
    FactorySettings settings,
    IImageProvider? provider = null,
    IObjectStore? objectStore = null,
    string? workspaceRoot = null,
    Func<string, Task>? progress = null)
{
    private readonly string _workspaceRoot = workspaceRoot ?? settings.WorkspaceRoot;
    private readonly Func<string, Task> _progress = progress ?? (_ => Task.CompletedTask);
    internal static readonly IReadOnlyDictionary<string, string> ExpectedPilotItems =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["agachamento-frontal-com-barra"] = "Agachamento frontal com barra",
        ["cadeira-extensora"] = "Cadeira extensora",
        ["mesa-flexora"] = "Mesa flexora",
        ["elevacao-pelvica-na-maquina"] = "Elevação pélvica na máquina",
        ["supino-reto-com-halteres"] = "Supino reto com halteres",
        ["flexao-de-bracos"] = "Flexão de braços",
        ["remada-baixa-no-cabo-com-barra-pronada"] = "Remada baixa no cabo com barra pronada",
        ["desenvolvimento-na-maquina"] = "Desenvolvimento na máquina",
        ["elevacao-lateral-unilateral-no-cabo"] = "Elevação lateral unilateral no cabo",
        ["prancha-frontal"] = "Prancha frontal"
    };

    internal async Task<ImagePilotManifest> PlanAsync(int maxItems, decimal maxCost, CancellationToken cancellationToken)
    {
        ValidateLimits(maxItems, maxCost);
        if (!string.Equals(settings.ImageModel, "gpt-image-2", StringComparison.Ordinal))
            throw new InvalidOperationException("O piloto foi validado somente para OpenAI:ImageModel=gpt-image-2.");
        RequireCurrentStyle();
        var store = CreateStore();
        var candidates = await ReadNormalizedCandidatesAsync(cancellationToken);
        var targetCount = Math.Min(maxItems, candidates.Count);
        var existing = await store.LoadAsync(cancellationToken);
        if (existing is not null)
        {
            ValidateCompatible(existing);
            ValidateCatalogItems(existing, candidates);

            if (existing.Items.Count < targetCount)
            {
                var additions = candidates.Skip(existing.Items.Count).Take(targetCount - existing.Items.Count)
                    .Select(item => new ImagePilotItem(
                        item.CanonicalName, item.Slug, BuildPrompt(item.CanonicalName, item.PrimaryMuscleGroup),
                        $"files/{item.Slug}.png"));
                existing = existing with { Items = existing.Items.Concat(additions).ToArray() };
            }

            EnsurePendingCost(existing.Items.Take(targetCount), maxCost);
            await store.SaveAsync(existing, cancellationToken);
            return existing;
        }

        var selected = candidates.Take(targetCount)
            .Select(item => new ImagePilotItem(
                item.CanonicalName,
                item.Slug,
                BuildPrompt(item.CanonicalName, item.PrimaryMuscleGroup),
                $"files/{item.Slug}.png"))
            .ToArray();
        EnsurePendingCost(selected, maxCost);

        var manifest = new ImagePilotManifest(1, settings.ImageModel, settings.ImageSize,
            settings.ImageQuality, settings.ImagePromptVersion, settings.ImageEstimatedCostUsd, selected);
        await store.SaveAsync(manifest, cancellationToken);
        return manifest;
    }

    internal async Task<(ImagePilotManifest Manifest, int Generated, int Skipped)> GenerateAsync(
        int maxItems, decimal maxCost, bool execute, CancellationToken cancellationToken)
    {
        var manifest = await PlanAsync(maxItems, maxCost, cancellationToken);
        if (!execute) return (manifest, 0, manifest.Items.Count(item => item.Sha256 is not null));
        var actualProvider = provider ?? new OpenAiImageProvider(
            new HttpClient { Timeout = TimeSpan.FromMinutes(10) }, settings.GetOpenAiCredentials().ApiKey);
        var store = CreateStore();
        Directory.CreateDirectory(store.FilesRoot);
        var generated = 0;
        var skipped = 0;

        var selectedItems = manifest.Items.Take(Math.Min(maxItems, manifest.Items.Count)).ToArray();
        for (var index = 0; index < selectedItems.Length; index++)
        {
            var item = selectedItems[index];
            var position = index + 1;
            var path = Path.Combine(store.Root, item.LocalFile);
            var pendingPath = path + ".pending";
            if (item.Sha256 is not null)
            {
                if (!File.Exists(path) || Sha256(await File.ReadAllBytesAsync(path, cancellationToken)) != item.Sha256)
                    throw new InvalidDataException($"Arquivo gerado ausente ou alterado: {item.LocalFile}. Corrija-o manualmente; regeneração silenciosa foi bloqueada.");
                if (File.Exists(pendingPath)) File.Delete(pendingPath);
                skipped++;
                await _progress($"[{position}/{selectedItems.Length}] Preservada: {item.Name} (já gerada).");
                continue;
            }
            if (File.Exists(path))
                throw new InvalidDataException($"Arquivo sem checkpoint já existe: {item.LocalFile}. Remova-o conscientemente ou reconcilie o manifesto.");

            if (File.Exists(pendingPath))
                throw new InvalidOperationException($"Geração anterior de {item.Slug} ficou incerta. Verifique a cobrança antes de remover {Path.GetFileName(pendingPath)} e tentar novamente.");
            await _progress($"[{position}/{selectedItems.Length}] Gerando: {item.Name}...");
            await File.WriteAllTextAsync(pendingPath, "generation-started", cancellationToken);

            var result = await GenerateWithRetryAsync(actualProvider, manifest, item, cancellationToken);
            RequirePng(result.Bytes, item.Slug);
            await File.WriteAllBytesAsync(path, result.Bytes, cancellationToken);
            var updated = item with { Sha256 = Sha256(result.Bytes) };
            manifest = manifest with { Items = manifest.Items.Select(current => current.Slug == item.Slug ? updated : current).ToArray() };
            await store.SaveAsync(manifest, cancellationToken);
            File.Delete(pendingPath);
            generated++;
            await _progress($"[{position}/{selectedItems.Length}] Concluída: {item.Name} ({result.Bytes.Length / 1024:N0} KB).");
        }
        return (manifest, generated, skipped);
    }

    internal async Task<ImagePilotManifest> ApproveAsync(string approvalsPath, CancellationToken cancellationToken)
    {
        RequireCurrentStyle();
        if (!File.Exists(approvalsPath)) throw new FileNotFoundException("Arquivo de aprovações não encontrado.", approvalsPath);
        var store = CreateStore();
        var manifest = await store.LoadAsync(cancellationToken) ?? throw new InvalidOperationException("Execute 'images plan' primeiro.");
        ValidateCompatible(manifest);
        await ValidateManifestCatalogAsync(manifest, cancellationToken);
        var approved = (await File.ReadAllLinesAsync(approvalsPath, cancellationToken))
            .Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')).ToHashSet(StringComparer.Ordinal);
        var known = manifest.Items.Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        var unknown = approved.Except(known, StringComparer.Ordinal).FirstOrDefault();
        if (unknown is not null) throw new InvalidDataException($"Slug desconhecido no arquivo de aprovações: {unknown}");
        foreach (var slug in approved)
            if (manifest.Items.Single(item => item.Slug == slug).Sha256 is null)
                throw new InvalidOperationException($"Não é possível aprovar imagem ainda não gerada: {slug}");
        manifest = manifest with { Items = manifest.Items.Select(item => item with { Approved = approved.Contains(item.Slug) }).ToArray() };
        await store.SaveAsync(manifest, cancellationToken);
        return manifest;
    }

    internal async Task<ImagePilotItem> RegenerateAsync(
        string slug,
        decimal maxCost,
        bool execute,
        CancellationToken cancellationToken)
    {
        RequireCurrentStyle();
        if (maxCost < settings.ImageEstimatedCostUsd)
            throw new ArgumentException($"O teto informado não cobre uma imagem: estimativa USD {settings.ImageEstimatedCostUsd:F2}; teto USD {maxCost:F2}.");
        var store = CreateStore();
        var manifest = await store.LoadAsync(cancellationToken) ?? throw new InvalidOperationException("Execute 'images plan' primeiro.");
        ValidateCompatible(manifest);
        await ValidateManifestCatalogAsync(manifest, cancellationToken);
        var item = manifest.Items.SingleOrDefault(value => value.Slug == slug)
            ?? throw new ArgumentException($"Slug não encontrado no manifesto: {slug}.");
        if (item.Uploaded) throw new InvalidOperationException("Imagem já enviada não pode ser regenerada silenciosamente; crie uma nova versão.");
        var path = Path.Combine(store.Root, item.LocalFile);
        if (item.Sha256 is null || !File.Exists(path)) throw new InvalidOperationException("A imagem original precisa existir antes da regeneração.");
        if (!execute) return item;

        var pendingPath = path + ".regenerate.pending";
        if (File.Exists(pendingPath))
            throw new InvalidOperationException($"Regeneração anterior de {slug} ficou incerta. Verifique a cobrança antes de tentar novamente.");
        await _progress($"[1/1] Regenerando: {item.Name}...");
        await File.WriteAllTextAsync(pendingPath, "regeneration-started", cancellationToken);

        var prompt = await BuildCurrentPromptAsync(item.Name, cancellationToken);
        var actualProvider = provider ?? new OpenAiImageProvider(
            new HttpClient { Timeout = TimeSpan.FromMinutes(10) }, settings.GetOpenAiCredentials().ApiKey);
        var result = await actualProvider.GenerateAsync(manifest.Model, prompt, manifest.Size, manifest.Quality, cancellationToken);
        RequirePng(result.Bytes, item.Slug);

        var rejectedRoot = Path.Combine(store.Root, "rejected");
        Directory.CreateDirectory(rejectedRoot);
        var archivePath = Path.Combine(rejectedRoot, $"{slug}.{item.Sha256[..12]}.png");
        if (!File.Exists(archivePath)) File.Copy(path, archivePath);
        await File.WriteAllBytesAsync(path, result.Bytes, cancellationToken);
        var updated = item with
        {
            Prompt = prompt,
            Sha256 = Sha256(result.Bytes),
            Approved = false,
            Uploaded = false,
            ObjectKey = null
        };
        manifest = manifest with { Items = manifest.Items.Select(value => value.Slug == slug ? updated : value).ToArray() };
        await store.SaveAsync(manifest, cancellationToken);
        File.Delete(pendingPath);
        await _progress($"[1/1] Concluída: {item.Name} ({result.Bytes.Length / 1024:N0} KB).");
        return updated;
    }

    internal async Task<(ImagePilotManifest Manifest, int Uploaded, int Skipped)> UploadAsync(bool execute, CancellationToken cancellationToken)
    {
        RequireCurrentStyle();
        var store = CreateStore();
        var manifest = await store.LoadAsync(cancellationToken) ?? throw new InvalidOperationException("Execute 'images plan' primeiro.");
        ValidateCompatible(manifest);
        await ValidateManifestCatalogAsync(manifest, cancellationToken);
        if (!execute) return (manifest, 0, manifest.Items.Count(item => item.Approved && item.Uploaded));
        var actualStore = objectStore ?? new S3ObjectStore(settings.GetBucketOptions(), settings.GetBucketCredentials());
        var ownsStore = objectStore is null;
        var uploaded = 0;
        var skipped = 0;
        try
        {
            var approvedItems = manifest.Items.Where(item => item.Approved).ToArray();
            for (var index = 0; index < approvedItems.Length; index++)
            {
                var item = approvedItems[index];
                var position = index + 1;
                if (item.Uploaded) { skipped++; continue; }
                await _progress($"[{position}/{approvedItems.Length}] Enviando: {item.Name}...");
                var path = Path.Combine(store.Root, item.LocalFile);
                if (item.Sha256 is null || !File.Exists(path)) throw new InvalidDataException($"Imagem aprovada ausente: {item.Slug}");
                var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                RequirePng(bytes, item.Slug);
                if (Sha256(bytes) != item.Sha256) throw new InvalidDataException($"Hash divergente para imagem aprovada: {item.Slug}");
                // Keep already checkpointed object keys untouched; all new v2 uploads use the v2 prefix.
                var key = ObjectKey.CreateCatalogImageV2(item.Slug);
                var existing = await actualStore.HeadAsync(key, cancellationToken);
                if (existing is not null)
                {
                    if (existing.Length != bytes.Length || existing.Sha256 != item.Sha256)
                        throw new InvalidDataException($"Já existe outro objeto na chave de destino: {key.Value}");
                    var resumed = item with { Uploaded = true, ObjectKey = key.Value };
                    manifest = manifest with { Items = manifest.Items.Select(current => current.Slug == item.Slug ? resumed : current).ToArray() };
                    await store.SaveAsync(manifest, cancellationToken);
                    skipped++;
                    continue;
                }
                await actualStore.PutAsync(key, bytes, "image/png", item.Sha256, cancellationToken);
                var metadata = await actualStore.HeadAsync(key, cancellationToken);
                if (metadata is null || metadata.Length != bytes.Length || metadata.Sha256 != item.Sha256)
                    throw new InvalidDataException($"Upload não pôde ser verificado: {item.Slug}");
                var updated = item with { Uploaded = true, ObjectKey = key.Value };
                manifest = manifest with { Items = manifest.Items.Select(current => current.Slug == item.Slug ? updated : current).ToArray() };
                await store.SaveAsync(manifest, cancellationToken);
                uploaded++;
                await _progress($"[{position}/{approvedItems.Length}] Upload verificado: {item.Name}.");
            }
        }
        finally
        {
            if (ownsStore) await actualStore.DisposeAsync();
        }
        return (manifest, uploaded, skipped);
    }

    private void ValidateCompatible(ImagePilotManifest manifest)
    {
        if (manifest.Model != settings.ImageModel || manifest.Size != settings.ImageSize ||
            manifest.Quality != settings.ImageQuality || manifest.PromptVersion != settings.ImagePromptVersion)
            throw new InvalidOperationException("Configuração visual mudou após o plano. Use outro workspace para iniciar um piloto novo.");
    }

    private static void ValidateLimits(int maxItems, decimal maxCost)
    {
        if (maxItems < 1) throw new ArgumentOutOfRangeException(nameof(maxItems), "--max-items deve ser maior que zero.");
        if (maxCost <= 0) throw new ArgumentOutOfRangeException(nameof(maxCost), "--max-cost deve ser maior que zero.");
    }

    private void EnsurePendingCost(IEnumerable<ImagePilotItem> items, decimal maxCost)
    {
        var pending = items.Count(item => item.Sha256 is null);
        var estimated = pending * settings.ImageEstimatedCostUsd;
        if (estimated > maxCost)
            throw new ArgumentException($"O teto informado não cobre as {pending} imagens pendentes: estimativa USD {estimated:F2}; teto USD {maxCost:F2}.");
    }

    private async Task<IReadOnlyList<NormalizedExercise>> ReadNormalizedCandidatesAsync(CancellationToken cancellationToken)
    {
        var rows = await CatalogInputReader.ReadAsync(settings.ImageCatalogPath, cancellationToken);
        var bytes = await File.ReadAllBytesAsync(settings.ImageCatalogPath, cancellationToken);
        var catalog = new CatalogNormalizer().Normalize(rows, Path.GetFileName(settings.ImageCatalogPath), Sha256(bytes));
        var normalized = catalog.Items.Where(item => item.State == "normalized").ToArray();
        var byName = normalized.ToDictionary(item => item.CanonicalName, StringComparer.Ordinal);
        var pilot = ExpectedPilotItems.Values.Select(name => byName.TryGetValue(name, out var item)
                ? item
                : throw new InvalidDataException($"Exercício inicial não encontrado entre os itens normalizados: {name}"))
            .ToArray();
        var pilotSlugs = pilot.Select(item => item.Slug).ToHashSet(StringComparer.Ordinal);
        return pilot.Concat(normalized.Where(item => !pilotSlugs.Contains(item.Slug))).ToArray();
    }

    private static void ValidateCatalogItems(ImagePilotManifest manifest, IReadOnlyList<NormalizedExercise> candidates)
    {
        if (manifest.Items.Count > candidates.Count)
            throw new InvalidDataException("O manifesto possui mais itens que o catálogo normalizado atual.");
        for (var index = 0; index < manifest.Items.Count; index++)
        {
            var item = manifest.Items[index];
            var expected = candidates[index];
            if (item.Slug != expected.Slug || item.Name != expected.CanonicalName)
                throw new InvalidDataException($"O manifesto diverge do catálogo normalizado na posição {index + 1}.");
        }
    }

    private async Task ValidateManifestCatalogAsync(ImagePilotManifest manifest, CancellationToken cancellationToken) =>
        ValidateCatalogItems(manifest, await ReadNormalizedCandidatesAsync(cancellationToken));

    private async Task<GeneratedImage> GenerateWithRetryAsync(
        IImageProvider actualProvider, ImagePilotManifest manifest, ImagePilotItem item, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await actualProvider.GenerateAsync(manifest.Model, item.Prompt, manifest.Size, manifest.Quality, cancellationToken);
            }
            catch (ImageProviderException exception) when (exception.Retryable && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(attempt);
                await _progress($"  Resposta temporária para {item.Name}; nova tentativa {attempt + 1}/{maxAttempts} em {delay.TotalSeconds:N0}s.");
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private string BuildPrompt(string name, string? muscleGroup)
    {
        var athlete = name.Sum(character => character) % 2 == 0
            ? "Uma atleta adulta usando top esportivo e legging"
            : "Um atleta adulto usando camiseta esportiva e shorts";
        return $"""
        {settings.ImagePromptVersion}. Crie uma única ilustração digital fitness premium para o exercício "{name}".
        Grupo principal: {muscleGroup ?? "não informado"}. Mostre uma posição estável, tecnicamente reconhecível e segura do movimento, com corpo inteiro e equipamento necessário visíveis.
        {athlete}, usando exclusivamente a paleta Personal Ultra: preto #080808, grafite/titânio #151515 e #222220, com detalhes laranja #FF6A13 e pequeno wordmark legível "ULTRA" no peito; nenhuma outra marca ou cor de destaque.
        Anatomia humana natural e proporcional, sem músculos expostos, overlay anatômico, cutaway, transparência da pele, fibras musculares pintadas ou destaque artificial de grupos musculares.
        Rosto natural e humano, expressão relaxada, textura de pele real e pequenas imperfeições sutis; sem aparência plástica, CGI, maquiagem excessiva ou estética de modelo fitness hiperproduzida.
        Composição central, contornos nítidos, iluminação cinematográfica suave e fundo de academia escuro e discreto.
        Fundo de academia levemente mais claro e legível, ainda premium e escuro, com equipamentos visíveis e preenchimento neutro suave.
        Preserve a ação principal nos 60% centrais para crop em cards. Sem colagem, setas, interface, legenda, watermark, membros extras, anatomia deformada ou equipamento incorreto.
        """;
    }

    private async Task<string> BuildCurrentPromptAsync(string name, CancellationToken cancellationToken)
    {
        var rows = await CatalogInputReader.ReadAsync(settings.ImageCatalogPath, cancellationToken);
        var bytes = await File.ReadAllBytesAsync(settings.ImageCatalogPath, cancellationToken);
        var catalog = new CatalogNormalizer().Normalize(rows, Path.GetFileName(settings.ImageCatalogPath), Sha256(bytes));
        var item = catalog.Items.Single(value => value.CanonicalName == name);
        return BuildPrompt(item.CanonicalName, item.PrimaryMuscleGroup);
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private ImagePilotStore CreateStore() => new(_workspaceRoot, settings.ImagePromptVersion);

    private void RequireCurrentStyle()
    {
        if (!string.Equals(settings.ImagePromptVersion, "personal-ultra-exercise-image-v2", StringComparison.Ordinal))
            throw new InvalidOperationException("Apenas o style v2 pode gerar, aprovar ou publicar; o v1 permanece arquivado localmente.");
    }

    private static void RequirePng(byte[] bytes, string slug)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < signature.Length || !bytes.AsSpan(0, signature.Length).SequenceEqual(signature))
            throw new InvalidDataException($"OpenAI não retornou um PNG válido para {slug}.");
    }
}
