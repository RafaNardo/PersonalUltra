using System.Text.Json;
using System.Text.RegularExpressions;
using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Persistence;

public sealed partial class RunStore(string workspaceRoot)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string WorkspaceRoot { get; } = Path.GetFullPath(workspaceRoot);
    public string RunsRoot => Path.Combine(WorkspaceRoot, "runs");
    public string LogsRoot => Path.Combine(WorkspaceRoot, "logs");

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

        var sourceDirectory = Path.Combine(GetRunDirectory(runId), "source");
        Directory.CreateDirectory(sourceDirectory);
        var safeName = Path.GetFileName(absoluteSourcePath);
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
        ValidateRunId(run.RunId);
        if (string.IsNullOrWhiteSpace(run.SchemaVersion)) throw new InvalidDataException("schemaVersion ausente.");
        if (string.IsNullOrWhiteSpace(run.Status)) throw new InvalidDataException("status ausente.");
        if (run.ResumeCount < 0) throw new InvalidDataException("resumeCount inválido.");
        if (string.IsNullOrWhiteSpace(run.Source.FileName) || Path.GetFileName(run.Source.FileName) != run.Source.FileName)
        {
            throw new InvalidDataException("Nome de source inválido.");
        }

        if (string.IsNullOrWhiteSpace(run.Source.StoredRelativePath)) throw new InvalidDataException("Source armazenado ausente.");
        if (run.Source.Length < 0) throw new InvalidDataException("Tamanho do source inválido.");
        if (!Sha256Pattern().IsMatch(run.Source.Sha256)) throw new InvalidDataException("SHA-256 do source inválido.");
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
}
