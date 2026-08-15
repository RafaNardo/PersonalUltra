using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Persistence;

public sealed partial class RunStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly Func<string, CancellationToken, Task>? _beforeManifestReplace;

    public RunStore(string workspaceRoot, Func<string, CancellationToken, Task>? beforeManifestReplace = null)
    {
        WorkspaceRoot = Path.GetFullPath(workspaceRoot);
        _beforeManifestReplace = beforeManifestReplace;
    }

    public string WorkspaceRoot { get; }
    public string RunsRoot => Path.Combine(WorkspaceRoot, "runs");
    public string LogsRoot => Path.Combine(WorkspaceRoot, "logs");

    public string GetStoredSourcePath(FactoryRun run)
    {
        ValidateRun(run);
        return ResolveDescendant(GetRunDirectory(run.RunId), run.Source.StoredRelativePath);
    }

    public async Task<ArtifactReference> SaveArtifactAsync(
        string runId,
        string stage,
        string relativePath,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        ValidateRunId(runId);
        if (stage is not ("normalization" or "metadata" or "image" or "export"))
            throw new ArgumentException("Estágio de artefato inválido.", nameof(stage));
        var runDirectory = GetRunDirectory(runId);
        var destination = ResolveDescendant(runDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!, $".{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: true);
            var (sha256, length) = await ComputeIntegrityAsync(destination, cancellationToken);
            return new ArtifactReference(stage, relativePath.Replace(Path.DirectorySeparatorChar, '/'), sha256, length);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task<byte[]> ReadVerifiedArtifactAsync(
        string runId,
        ArtifactReference artifact,
        CancellationToken cancellationToken = default)
    {
        ValidateRunId(runId);
        var path = ResolveDescendant(GetRunDirectory(runId), artifact.RelativePath);
        if (!File.Exists(path)) throw new InvalidDataException($"Artefato ausente: {artifact.RelativePath}");
        var (sha256, length) = await ComputeIntegrityAsync(path, cancellationToken);
        if (sha256 != artifact.Sha256 || length != artifact.Length)
            throw new InvalidDataException($"Artefato falhou na verificação de integridade: {artifact.RelativePath}");
        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public void Initialize()
    {
        Directory.CreateDirectory(RunsRoot);
        Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "cache"));
        Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "inputs"));
        Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "outputs"));
        Directory.CreateDirectory(LogsRoot);
    }

    public void VerifyWritable()
    {
        Initialize();
        var probe = Path.Combine(WorkspaceRoot, $".write-probe-{Guid.NewGuid():N}");
        try
        {
            using (File.Create(probe)) { }
        }
        finally
        {
            if (File.Exists(probe)) File.Delete(probe);
        }
    }

    public async Task<SourceArtifact> CopySourceAsync(
        string runId,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        ValidateRunId(runId);
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var sourceInfo = new FileInfo(absoluteSourcePath);
        if (!sourceInfo.Exists) throw new FileNotFoundException("Arquivo de entrada não encontrado.", absoluteSourcePath);

        var safeName = Path.GetFileName(absoluteSourcePath);
        EnsureSafeText(safeName, "nome do source");
        EnsureSafeText(absoluteSourcePath, "path original do source");

        var sourceDirectory = Path.Combine(GetRunDirectory(runId), "source");
        Directory.CreateDirectory(sourceDirectory);
        var destination = Path.Combine(sourceDirectory, safeName);
        if (File.Exists(destination)) throw new IOException("O source imutável deste run já existe.");

        var temporary = Path.Combine(sourceDirectory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var input = new FileStream(
                absoluteSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var output = new FileStream(
                temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }

            File.Move(temporary, destination);
            var (sha256, length) = await ComputeIntegrityAsync(destination, cancellationToken);
            var relativePath = Path.GetRelativePath(GetRunDirectory(runId), destination)
                .Replace(Path.DirectorySeparatorChar, '/');
            return new SourceArtifact(safeName, absoluteSourcePath, relativePath, sha256, length);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task VerifySourceAsync(FactoryRun run, CancellationToken cancellationToken = default)
    {
        ValidateRun(run);
        var runDirectory = GetRunDirectory(run.RunId);
        var sourcePath = ResolveDescendant(runDirectory, run.Source.StoredRelativePath);
        if (!File.Exists(sourcePath)) throw new InvalidDataException("O source imutável do run não foi encontrado.");

        var (sha256, length) = await ComputeIntegrityAsync(sourcePath, cancellationToken);
        if (length != run.Source.Length || !string.Equals(sha256, run.Source.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("O source imutável do run falhou na verificação de integridade.");
        }
    }

    public async Task SaveAsync(FactoryRun run, CancellationToken cancellationToken = default)
    {
        ValidateRun(run);
        Initialize();
        var runDirectory = GetRunDirectory(run.RunId);
        Directory.CreateDirectory(runDirectory);
        var destination = Path.Combine(runDirectory, "manifest.json");
        var temporary = Path.Combine(runDirectory, $"manifest.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, run, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            // Validate the complete checkpoint before it can replace the last known-good manifest.
            _ = await DeserializeAsync(temporary, cancellationToken);
            if (_beforeManifestReplace is not null) await _beforeManifestReplace(temporary, cancellationToken);
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task<FactoryRun?> LoadAsync(string runId, CancellationToken cancellationToken = default)
    {
        ValidateRunId(runId);
        var path = Path.Combine(GetRunDirectory(runId), "manifest.json");
        if (!File.Exists(path)) return null;

        var run = await DeserializeAsync(path, cancellationToken);
        if (!string.Equals(run.RunId, runId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("O runId do manifesto não corresponde ao diretório.");
        }

        return run;
    }

    public async Task<IReadOnlyList<FactoryRun>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(RunsRoot)) return [];

        var runs = new List<FactoryRun>();
        foreach (var runDirectory in Directory.EnumerateDirectories(RunsRoot))
        {
            var runId = Path.GetFileName(runDirectory);
            ValidateRunId(runId);
            var manifest = Path.Combine(runDirectory, "manifest.json");
            if (!File.Exists(manifest)) continue;
            var run = await DeserializeAsync(manifest, cancellationToken);
            if (!string.Equals(run.RunId, runId, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Manifesto inconsistente no run {runId}.");
            }

            runs.Add(run);
        }

        return runs.OrderByDescending(run => run.CreatedAt).ToArray();
    }

    public static void ValidateRunId(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId) || !RunIdPattern().IsMatch(runId) || runId is "." or "..")
        {
            throw new ArgumentException("runId inválido.", nameof(runId));
        }
    }

    private async Task<FactoryRun> DeserializeAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var run = await JsonSerializer.DeserializeAsync<FactoryRun>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException("Manifesto vazio.");
            ValidateRun(run);
            return run;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Manifesto inválido: {Path.GetFileName(Path.GetDirectoryName(path))}", exception);
        }
    }

    private string GetRunDirectory(string runId)
    {
        ValidateRunId(runId);
        return ResolveDescendant(RunsRoot, runId);
    }

    private static string ResolveDescendant(string root, string relativePath)
    {
        if (Path.IsPathFullyQualified(relativePath)) throw new InvalidDataException("Path absoluto não permitido no run.");
        var absoluteRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(absoluteRoot, relativePath));
        if (!candidate.StartsWith(absoluteRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Path fora do diretório permitido.");
        }

        return candidate;
    }

    private static void ValidateRun(FactoryRun run)
    {
        if (run is null) throw new InvalidDataException("Manifesto nulo.");
        ValidateRunId(run.RunId);
        EnsureSafeText(run.RunId, "runId");
        EnsureSafeText(run.SchemaVersion, "schemaVersion");
        EnsureSafeText(run.Status, "status");
        if (run.SchemaVersion != "1") throw new InvalidDataException("schemaVersion desconhecido.");
        if (!ManifestStates.Run.Contains(run.Status)) throw new InvalidDataException("status de run inválido.");
        if (run.CreatedAt == default || run.UpdatedAt < run.CreatedAt) throw new InvalidDataException("Datas do run inválidas.");
        if (run.ResumeCount < 0) throw new InvalidDataException("resumeCount inválido.");
        if (run.Source is null) throw new InvalidDataException("Source do run ausente.");
        if (string.IsNullOrWhiteSpace(run.Source.FileName) || Path.GetFileName(run.Source.FileName) != run.Source.FileName)
        {
            throw new InvalidDataException("Nome de source inválido.");
        }

        EnsureSafeText(run.Source.FileName, "nome do source");
        EnsureSafeText(run.Source.OriginalAbsolutePath, "path original do source");
        ValidateRelativePath(run.Source.StoredRelativePath, "source armazenado");
        if (run.Source.Length < 0) throw new InvalidDataException("Tamanho do source inválido.");
        ValidateSha256(run.Source.Sha256, "SHA-256 do source");

        foreach (var hash in run.StageHashes ?? new Dictionary<string, string>())
        {
            EnsureSafeText(hash.Key, "nome do hash de estágio");
            if (hash.Key is not ("source" or "normalization" or "metadata" or "image" or "export"))
                throw new InvalidDataException("Nome de hash de estágio inválido.");
            if (hash.Value is null) throw new InvalidDataException("Hash de estágio ausente.");
            ValidateSha256(hash.Value, "hash do estágio");
        }
        foreach (var output in run.Outputs ?? [])
        {
            if (output is null) throw new InvalidDataException("Referência de output nula.");
            ValidateArtifact(output);
        }
        foreach (var item in run.Items ?? [])
        {
            if (item is null) throw new InvalidDataException("Item nulo no manifesto.");
            ValidateItem(item);
        }
        var duplicateKey = (run.Items ?? []).GroupBy(item => item.ExternalKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateKey is not null) throw new InvalidDataException("externalKey duplicada no manifesto.");
        foreach (var attempt in run.Attempts ?? [])
        {
            if (attempt is null) throw new InvalidDataException("Tentativa nula no manifesto.");
            ValidateAttempt(attempt);
        }
        if (run.Usage is { EstimatedCost: < 0 } or { ObservedCost: < 0 } or { Attempts: < 0 })
            throw new InvalidDataException("Resumo de usage inválido.");
        if (run.Usage is { } usage) EnsureSafeText(usage.Currency, "moeda do usage");
        if (run.Versions is { } versions)
        {
            var versionValues = new[]
            {
                versions.PipelineVersion, versions.TaxonomyVersion, versions.MetadataPromptVersion,
                versions.ImagePromptVersion, versions.StyleVersion, versions.TargetProfileVersion
            };
            if (versionValues.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("Versões do pipeline não podem ficar vazias.");
            foreach (var version in versionValues) EnsureSafeText(version, "versão do pipeline");
        }
    }

    private static void ValidateItem(ManifestItem item)
    {
        EnsureSafeText(item.ExternalKey, "externalKey");
        EnsureSafeText(item.State, "estado do item");
        if (!ManifestStates.Item.Contains(item.State)) throw new InvalidDataException("estado de item inválido.");
        if (item.Source is null) throw new InvalidDataException("Source do item ausente.");
        if (item.Hashes is null) throw new InvalidDataException("Hashes do item ausentes.");
        if (item.Source.Row < 1) throw new InvalidDataException("Linha de origem inválida.");
        ValidateRelativePath(item.Source.File, "source do item");
        ValidateSha256(item.Source.SourceHash, "sourceHash do item");
        ValidateSha256(item.Hashes.Source, "hash source do item");
        ValidateOptionalSha256(item.Hashes.NormalizationInput, "normalizationInputHash");
        ValidateOptionalSha256(item.Hashes.MetadataInput, "metadataInputHash");
        ValidateOptionalSha256(item.Hashes.ImageInput, "imageInputHash");
        ValidateOptionalSha256(item.Hashes.ExportInput, "exportInputHash");
        foreach (var artifact in item.Artifacts ?? [])
        {
            if (artifact is null) throw new InvalidDataException("Artefato nulo no item.");
            ValidateArtifact(artifact);
        }
        foreach (var review in item.Reviews ?? [])
        {
            if (review is null) throw new InvalidDataException("Review nulo no item.");
            EnsureSafeText(review.ItemKey, "itemKey da revisão");
            EnsureSafeText(review.Stage, "estágio da revisão");
            EnsureSafeText(review.Decision, "decisão da revisão");
            EnsureSafeText(review.ReasonCode, "reasonCode da revisão");
            EnsureSafeText(review.Reviewer, "reviewer da revisão");
            if (!string.Equals(review.ItemKey, item.ExternalKey, StringComparison.Ordinal))
                throw new InvalidDataException("itemKey da revisão não corresponde ao item.");
            if (review.Stage is not ("metadata" or "visual" or "biomechanics"))
                throw new InvalidDataException("Estágio de revisão inválido.");
            if (review.ReviewedAt == default) throw new InvalidDataException("Data da revisão inválida.");
            ValidateSha256(review.ArtifactHash, "artifactHash da revisão");
            EnsureSafeOptionalText(review.Notes, "notes da revisão");
            if (review.Decision is not ("approved" or "rejected")) throw new InvalidDataException("Decisão de revisão inválida.");
        }
    }

    private static void ValidateArtifact(ArtifactReference artifact)
    {
        EnsureSafeText(artifact.Stage, "estágio do artefato");
        if (artifact.Stage is not ("source" or "normalization" or "metadata" or "image" or "export"))
            throw new InvalidDataException("Estágio de artefato inválido.");
        ValidateRelativePath(artifact.RelativePath, "path do artefato");
        ValidateSha256(artifact.Sha256, "SHA-256 do artefato");
        if (artifact.Length < 0) throw new InvalidDataException("Tamanho de artefato inválido.");
    }

    private static void ValidateAttempt(ProviderAttempt attempt)
    {
        if (attempt.Stage is not ("metadata" or "image")) throw new InvalidDataException("Estágio de tentativa inválido.");
        if (attempt.Status is not ("started" or "succeeded" or "failed_retryable" or "failed_terminal"))
            throw new InvalidDataException("Status de tentativa inválido.");
        if (attempt.Attempt < 1) throw new InvalidDataException("Número de tentativa inválido.");
        if (attempt.FinishedAt < attempt.StartedAt) throw new InvalidDataException("Duração de tentativa inválida.");
        ValidateSha256(attempt.InputHash, "inputHash da tentativa");
        EnsureSafeText(attempt.Stage, "estágio da tentativa");
        EnsureSafeText(attempt.Provider, "provider");
        EnsureSafeText(attempt.Model, "model");
        EnsureSafeOptionalText(attempt.RequestId, "requestId");
        EnsureSafeOptionalText(attempt.PromptVersion, "promptVersion");
        EnsureSafeText(attempt.Status, "status da tentativa");
        if (attempt.Cost is null) throw new InvalidDataException("Custo da tentativa ausente.");
        if (attempt.Cost.Estimated < 0 || attempt.Cost.Observed < 0) throw new InvalidDataException("Custo inválido.");
        EnsureSafeText(attempt.Cost.Currency, "moeda do custo");
        if (attempt.Usage is { InputTokens: < 0 } or { OutputTokens: < 0 } or { Images: < 0 })
            throw new InvalidDataException("Usage inválido.");
    }

    private static void EnsureSafeText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException($"{field} ausente.");
        if (CredentialPattern().IsMatch(value))
            throw new InvalidDataException($"Conteúdo sensível não pode ser persistido em {field}.");
    }

    private static void EnsureSafeOptionalText(string? value, string field)
    {
        if (value is not null) EnsureSafeText(value, field);
    }

    private static void ValidateRelativePath(string? value, string field)
    {
        EnsureSafeText(value, field);
        var path = value!;
        if (Path.IsPathFullyQualified(path) || path.Split('/', '\\').Any(segment => segment is ".." or "." or ""))
            throw new InvalidDataException($"{field} deve ser relativo e não pode conter traversal.");
    }

    private static void ValidateOptionalSha256(string? hash, string field)
    {
        if (hash is not null) ValidateSha256(hash, field);
    }

    private static void ValidateSha256(string? hash, string field)
    {
        if (string.IsNullOrWhiteSpace(hash) || !Sha256Pattern().IsMatch(hash))
            throw new InvalidDataException($"{field} inválido.");
    }

    private static async Task<(string Sha256, long Length)> ComputeIntegrityAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var sha256 = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();
        return (sha256, stream.Length);
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex RunIdPattern();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();

    [GeneratedRegex("(?i)(bearer\\s+|sk-|tsec_|tid_)[A-Za-z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialPattern();
}
