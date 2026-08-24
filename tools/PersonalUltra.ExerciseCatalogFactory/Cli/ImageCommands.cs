using System.Globalization;
using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Images;

namespace PersonalUltra.ExerciseCatalogFactory.Cli;

internal sealed class ImageCommands(
    FactorySettings settings,
    TextWriter output,
    IImageProvider? provider = null)
{
    internal async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var command = CommandLine.Parse(args);
        var workspace = command.Option("--workspace") is { } configured
            ? FactorySettings.ResolveWorkspaceRoot(configured)
            : settings.WorkspaceRoot;
        var service = new ImagePilotService(
            settings,
            provider,
            workspaceRoot: workspace,
            progress: message => output.WriteLineAsync(message));
        return command.Name switch
        {
            "plan" => await PlanAsync(command, service, cancellationToken),
            "generate" => await GenerateAsync(command, service, cancellationToken),
            "regenerate" => await RegenerateAsync(command, service, cancellationToken),
            "approve" => await ApproveAsync(command, service, cancellationToken),
            "upload" => await UploadAsync(command, service, cancellationToken),
            "seed" => await SeedAsync(command, workspace, cancellationToken),
            _ => throw new ArgumentException($"Comando desconhecido: images {command.Name}")
        };
    }

    private async Task<int> SeedAsync(ParsedCommand command, string workspace, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--execute");
        command.EnsureFlag("--execute");
        var execute = command.HasOption("--execute");
        var result = await new ExerciseSeedExporter(settings, workspace).ExportAsync(execute, cancellationToken);
        await output.WriteLineAsync(
            $"Seed: catálogo={result.NormalizedCount}; legado preservado={result.LegacyCount}; novos={result.GeneratedCount}; " +
            $"modo={(execute ? "gerado" : "dry-run")}.");
        if (!execute)
            await output.WriteLineAsync("Dry-run: manifesto validado; nenhum arquivo foi alterado. Use --execute para gerar o C# determinístico.");
        else
            await output.WriteLineAsync($"Arquivo gerado: {result.OutputPath}");
        return 0;
    }

    private async Task<int> RegenerateAsync(ParsedCommand command, ImagePilotService service, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--slug", "--max-cost", "--execute");
        command.EnsureFlag("--execute");
        if (!decimal.TryParse(command.RequiredOption("--max-cost"), NumberStyles.Number, CultureInfo.InvariantCulture, out var maxCost))
            throw new ArgumentException("--max-cost inválido; use ponto como separador decimal.");
        var slug = command.RequiredOption("--slug");
        var execute = command.HasOption("--execute");
        var result = await service.RegenerateAsync(slug, maxCost, execute, cancellationToken);
        if (!execute)
            await output.WriteLineAsync($"Regeneração dry-run: {result.Name}. Nenhuma chamada paga foi feita.");
        else
            await output.WriteLineAsync($"Regeneração concluída: {result.Name}. A imagem anterior foi preservada em rejected/.");
        return 0;
    }

    private async Task<int> PlanAsync(ParsedCommand command, ImagePilotService service, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--max-items", "--all", "--max-cost");
        command.EnsureFlag("--all");
        var (maxItems, maxCost) = ReadLimits(command);
        var manifest = await service.PlanAsync(maxItems, maxCost, cancellationToken);
        await WritePlanAsync(manifest, maxItems);
        await output.WriteLineAsync("Plano local criado; nenhuma chamada OpenAI foi feita.");
        return 0;
    }

    private async Task<int> GenerateAsync(ParsedCommand command, ImagePilotService service, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--max-items", "--all", "--max-cost", "--execute");
        command.EnsureFlag("--all");
        command.EnsureFlag("--execute");
        var (maxItems, maxCost) = ReadLimits(command);
        var execute = command.HasOption("--execute");
        var result = await service.GenerateAsync(maxItems, maxCost, execute, cancellationToken);
        if (!execute)
        {
            await WritePlanAsync(result.Manifest, maxItems);
            await output.WriteLineAsync("Dry-run: zero chamadas pagas. Acrescente --execute somente após revisar o plano.");
        }
        else await output.WriteLineAsync($"Geração concluída: novos={result.Generated}; preservados={result.Skipped}. Revise os PNGs antes de aprovar.");
        return 0;
    }

    private async Task<int> ApproveAsync(ParsedCommand command, ImagePilotService service, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--file");
        var file = Path.GetFullPath(command.RequiredOption("--file"));
        var manifest = await service.ApproveAsync(file, cancellationToken);
        await output.WriteLineAsync($"Aprovações registradas: {manifest.Items.Count(item => item.Approved)}. Nenhum upload foi feito.");
        return 0;
    }

    private async Task<int> UploadAsync(ParsedCommand command, ImagePilotService service, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--workspace", "--execute");
        command.EnsureFlag("--execute");
        var execute = command.HasOption("--execute");
        var result = await service.UploadAsync(execute, cancellationToken);
        if (!execute)
            await output.WriteLineAsync($"Upload dry-run: aprovadas={result.Manifest.Items.Count(item => item.Approved)}; pendentes={result.Manifest.Items.Count(item => item.Approved && !item.Uploaded)}. Use --execute para enviar.");
        else
            await output.WriteLineAsync($"Upload concluído: novos={result.Uploaded}; preservados={result.Skipped}.");
        return 0;
    }

    private static (int MaxItems, decimal MaxCost) ReadLimits(ParsedCommand command)
    {
        var all = command.HasOption("--all");
        if (all && command.HasOption("--max-items"))
            throw new ArgumentException("Use --all ou --max-items, não ambos.");
        var maxItems = all
            ? int.MaxValue
            : int.TryParse(command.RequiredOption("--max-items"), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : throw new ArgumentException("--max-items inválido.");
        if (!decimal.TryParse(command.RequiredOption("--max-cost"), NumberStyles.Number, CultureInfo.InvariantCulture, out var maxCost))
            throw new ArgumentException("--max-cost inválido; use ponto como separador decimal.");
        return (maxItems, maxCost);
    }

    private async Task WritePlanAsync(ImagePilotManifest manifest, int maxItems)
    {
        var selected = manifest.Items.Take(Math.Min(maxItems, manifest.Items.Count)).ToArray();
        var pending = selected.Count(item => item.Sha256 is null);
        await output.WriteLineAsync($"Batch: total={selected.Length}; prontas={selected.Length - pending}; pendentes={pending} | {manifest.Model} | {manifest.Size} | qualidade={manifest.Quality} | estimativa pendente USD {pending * manifest.EstimatedCostPerImageUsd:F2}");
    }

}
