using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Logging;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Cli;

public sealed class FactoryApplication(FactorySettings settings, TextWriter output, TextWriter error)
{
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0 || args.Any(arg => arg is "-h" or "--help") || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            WriteHelp();
            return 0;
        }

        try
        {
            var command = CommandLine.Parse(args);
            var workspace = command.Option("--workspace") is { } configuredWorkspace
                ? FactorySettings.ResolveWorkspaceRoot(configuredWorkspace)
                : settings.WorkspaceRoot;
            var store = new RunStore(workspace);

            return command.Name switch
            {
                "init" => await InitializeAsync(command, store, cancellationToken),
                "import" => await ImportAsync(command, store, cancellationToken),
                "status" => await StatusAsync(command, store, cancellationToken),
                "doctor" => await DoctorAsync(command, store, cancellationToken),
                _ => UnknownCommand(command.Name)
            };
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync("Operação cancelada. O último checkpoint íntegro pode ser retomado.");
            return 130;
        }
        catch (Exception exception) when (exception is
            IOException or UnauthorizedAccessException or InvalidDataException or
            ArgumentException or InvalidOperationException)
        {
            await error.WriteLineAsync($"Falha local: {exception.Message}");
            return 2;
        }
    }

    private async Task<int> InitializeAsync(
        ParsedCommand command,
        RunStore store,
        CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace");
        store.VerifyWritable();
        var logger = new StructuredLog(store.LogsRoot);
        await logger.WriteAsync("information", "workspace.initialized", "Workspace inicializado.", cancellationToken: cancellationToken);
        output.WriteLine($"Workspace inicializado: {store.WorkspaceRoot}");
        output.WriteLine("Modo padrão: dry-run. Nenhuma integração externa está habilitada nesta baseline.");
        return 0;
    }

    private async Task<int> ImportAsync(
        ParsedCommand command,
        RunStore store,
        CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--file", "--resume");
        var sourcePath = command.Option("--file");
        var resumeRunId = command.Option("--resume");
        if ((sourcePath is null) == (resumeRunId is null))
        {
            await error.WriteLineAsync("Use exatamente uma opção: --file <caminho> para novo run ou --resume <runId>.");
            return 2;
        }

        store.VerifyWritable();
        var logger = new StructuredLog(store.LogsRoot);
        if (resumeRunId is not null)
        {
            var existing = await store.LoadAsync(resumeRunId, cancellationToken);
            if (existing is null)
            {
                await error.WriteLineAsync($"Run não encontrado: {resumeRunId}");
                return 2;
            }

            if (!string.Equals(existing.SchemaVersion, settings.SchemaVersion, StringComparison.Ordinal))
            {
                await error.WriteLineAsync(
                    $"O run usa schema {existing.SchemaVersion}, mas a Factory atual usa {settings.SchemaVersion}.");
                return 2;
            }

            await store.VerifySourceAsync(existing, cancellationToken);
            var resumed = existing with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                ResumeCount = existing.ResumeCount + 1
            };
            await store.SaveAsync(resumed, cancellationToken);
            await logger.WriteAsync(
                "information", "run.resumed", "Run retomado após verificação de integridade.",
                new Dictionary<string, object?> { ["runId"] = resumed.RunId, ["resumeCount"] = resumed.ResumeCount },
                cancellationToken);
            await output.WriteLineAsync($"Run retomado: {resumed.RunId}");
            await output.WriteLineAsync($"Source imutável verificado. Retomadas: {resumed.ResumeCount}");
            await output.WriteLineAsync("Nenhuma chamada externa ou alteração no produto foi executada.");
            return 0;
        }

        var absolutePath = Path.GetFullPath(sourcePath!);
        if (!File.Exists(absolutePath))
        {
            await error.WriteLineAsync($"Arquivo não encontrado: {absolutePath}");
            return 2;
        }

        var now = DateTimeOffset.UtcNow;
        var runId = $"{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..27];
        var source = await store.CopySourceAsync(runId, absolutePath, cancellationToken);
        var run = new FactoryRun(
            settings.SchemaVersion,
            runId,
            now,
            now,
            "imported",
            DryRun: true,
            ResumeCount: 0,
            source);

        await store.SaveAsync(run, cancellationToken);
        await logger.WriteAsync(
            "information", "run.imported", "Source importado em modo dry-run.",
            new Dictionary<string, object?>
            {
                ["runId"] = run.RunId,
                ["fileName"] = run.Source.FileName,
                ["length"] = run.Source.Length,
                ["sha256"] = run.Source.Sha256
            },
            cancellationToken);
        await output.WriteLineAsync($"Run criado: {run.RunId}");
        await output.WriteLineAsync($"Fonte copiada para o run: {run.Source.FileName} ({run.Source.Length} bytes)");
        await output.WriteLineAsync($"SHA-256: {run.Source.Sha256}");
        await output.WriteLineAsync("Nenhuma chamada externa ou alteração no produto foi executada.");
        return 0;
    }

    private async Task<int> StatusAsync(
        ParsedCommand command,
        RunStore store,
        CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--run");
        var runId = command.Option("--run");
        if (runId is not null)
        {
            var run = await store.LoadAsync(runId, cancellationToken);
            if (run is null)
            {
                await error.WriteLineAsync($"Run não encontrado: {runId}");
                return 2;
            }

            WriteRun(run);
            return 0;
        }

        var runs = await store.LoadAllAsync(cancellationToken);
        if (runs.Count == 0)
        {
            await output.WriteLineAsync("Nenhum run local encontrado.");
            return 0;
        }

        foreach (var run in runs) WriteRun(run);
        return 0;
    }

    private async Task<int> DoctorAsync(
        ParsedCommand command,
        RunStore store,
        CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace");
        output.WriteLine("Exercise Catalog Factory — diagnóstico da baseline local");
        output.WriteLine($"Workspace: {store.WorkspaceRoot}");

        var localErrors = settings.ValidateLocalConfiguration().ToList();
        try
        {
            store.VerifyWritable();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            localErrors.Add($"Workspace não gravável: {exception.Message}");
        }

        if (localErrors.Count == 0)
        {
            output.WriteLine("Readiness local: READY");
        }
        else
        {
            output.WriteLine("Readiness local: BLOCKED");
            foreach (var issue in localErrors) output.WriteLine($"- {issue}");
        }

        var missing = settings.MissingSecretNames();
        output.WriteLine("Integrações externas: PENDING (fora do escopo de PU-ECF-001)");
        if (missing.Count > 0)
        {
            output.WriteLine("Nomes de secrets ainda ausentes:");
            foreach (var name in missing) output.WriteLine($"- {name}");
        }
        else
        {
            output.WriteLine("Secrets esperados: configurados, valores não exibidos.");
        }

        output.WriteLine("Adapters OpenAI/S3: não implementados nesta baseline; nenhuma conexão foi tentada.");
        output.WriteLine("Target profile: validação planejada para PU-ECF-009/010.");

        if (localErrors.Count == 0)
        {
            var logger = new StructuredLog(store.LogsRoot);
            await logger.WriteAsync(
                "information", "doctor.completed", "Diagnóstico local concluído.",
                new Dictionary<string, object?>
                {
                    ["localReady"] = true,
                    ["externalIntegrations"] = "pending",
                    ["missingSecretCount"] = missing.Count
                },
                cancellationToken);
        }

        return localErrors.Count == 0 ? 0 : 2;
    }

    private void WriteRun(FactoryRun run) =>
        output.WriteLine(
            $"{run.RunId} | {run.Status} | dry-run={run.DryRun} | resumes={run.ResumeCount} | " +
            $"{run.Source.FileName} | {run.CreatedAt:O}");

    private int UnknownCommand(string command)
    {
        error.WriteLine($"Comando desconhecido: {command}");
        WriteHelp();
        return 2;
    }

    private void WriteHelp()
    {
        output.WriteLine("Personal Ultra Exercise Catalog Factory");
        output.WriteLine();
        output.WriteLine("Comandos:");
        output.WriteLine("  init [--workspace <pasta>]");
        output.WriteLine("  import --file <csv-ou-json> [--workspace <pasta>]");
        output.WriteLine("  import --resume <runId> [--workspace <pasta>]");
        output.WriteLine("  status [--run <id>] [--workspace <pasta>]");
        output.WriteLine("  doctor [--workspace <pasta>]");
        output.WriteLine();
        output.WriteLine("Esta baseline opera somente localmente e sempre em dry-run.");
    }
}

internal sealed class ParsedCommand(string name, IReadOnlyDictionary<string, string> options)
{
    public string Name { get; } = name;

    public string? Option(string name) => options.GetValueOrDefault(name);

    public void EnsureOnly(params string[] allowed)
    {
        var allowedSet = allowed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unexpected = options.Keys.FirstOrDefault(key => !allowedSet.Contains(key));
        if (unexpected is not null) throw new ArgumentException($"Opção não suportada para {Name}: {unexpected}");
    }
}

internal static class CommandLine
{
    public static ParsedCommand Parse(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith("-", StringComparison.Ordinal))
        {
            throw new ArgumentException("O primeiro argumento deve ser um comando.");
        }

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index += 2)
        {
            var name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal) || name.Length == 2)
            {
                throw new ArgumentException($"Argumento posicional inesperado: {name}");
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"A opção {name} exige um valor.");
            }

            if (!options.TryAdd(name, args[index + 1]))
            {
                throw new ArgumentException($"Opção duplicada: {name}");
            }
        }

        return new ParsedCommand(args[0].ToLowerInvariant(), options);
    }
}
