using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Normalization;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Intake;

public sealed class IntakeProcessor(RunStore store)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<IntakeResult> ExecuteAsync(FactoryRun run, CancellationToken cancellationToken = default)
    {
        await store.VerifySourceAsync(run, cancellationToken);
        var inputHash = ComputeInputHash(run.Source.Sha256);
        var existingCatalogArtifact = (run.Outputs ?? []).SingleOrDefault(output =>
            output.Stage == "normalization" && output.RelativePath == "normalization/catalog.normalized.v1.json");
        var existingReportArtifact = (run.Outputs ?? []).SingleOrDefault(output =>
            output.Stage == "normalization" && output.RelativePath == "normalization/intake-report.v1.md");
        if (run.StageHashes?.GetValueOrDefault("normalization") == inputHash &&
            existingCatalogArtifact is not null && existingReportArtifact is not null)
        {
            try
            {
                var bytes = await store.ReadVerifiedArtifactAsync(run.RunId, existingCatalogArtifact, cancellationToken);
                _ = await store.ReadVerifiedArtifactAsync(run.RunId, existingReportArtifact, cancellationToken);
                var cached = JsonSerializer.Deserialize<NormalizedCatalog>(bytes, JsonOptions)
                    ?? throw new InvalidDataException("Catálogo normalizado vazio.");
                return new IntakeResult(run, cached, CacheHit: true);
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException)
            {
                // A fonte imutável será processada novamente. Um artefato parcial ou
                // corrompido jamais pode ser considerado cache hit.
            }
        }

        var sourcePath = store.GetStoredSourcePath(run);
        var rows = await CatalogInputReader.ReadAsync(sourcePath, cancellationToken);
        var catalog = new CatalogNormalizer().Normalize(rows, run.Source.FileName, run.Source.Sha256);
        var catalogBytes = JsonSerializer.SerializeToUtf8Bytes(catalog, JsonOptions);
        var reportBytes = Encoding.UTF8.GetBytes(BuildReport(catalog));
        var catalogArtifact = await store.SaveArtifactAsync(run.RunId, "normalization",
            "normalization/catalog.normalized.v1.json", catalogBytes, cancellationToken);
        var reportArtifact = await store.SaveArtifactAsync(run.RunId, "normalization",
            "normalization/intake-report.v1.md", reportBytes, cancellationToken);

        var artifacts = (run.Outputs ?? []).Where(output => output.Stage != "normalization")
            .Concat([catalogArtifact, reportArtifact]).OrderBy(output => output.RelativePath, StringComparer.Ordinal).ToArray();
        var items = catalog.Items.Select(item => new ManifestItem(item.ExternalKey, item.State,
            new ItemSource(run.Source.StoredRelativePath, item.SourceRow, item.SourceHash),
            new ItemStageHashes(item.SourceHash, inputHash),
            Artifacts: [catalogArtifact], Reviews: [])).ToArray();
        var now = DateTimeOffset.UtcNow;
        var processed = run with
        {
            UpdatedAt = now,
            Status = catalog.Issues.Any(issue => issue.Severity == "blocking") ? "needs_review" : "ready",
            Items = items,
            Versions = new PipelineVersions("factory-v1", CatalogNormalizer.TaxonomyVersion, "pending", "pending", "pending", CatalogNormalizer.TargetProfileVersion),
            StageHashes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = run.Source.Sha256,
                ["normalization"] = inputHash
            },
            Outputs = artifacts
        };
        await store.SaveAsync(processed, cancellationToken);
        return new IntakeResult(processed, catalog, CacheHit: false);
    }

    private static string ComputeInputHash(string sourceSha256) => Sha256(string.Join("\n", sourceSha256,
        CatalogNormalizer.PipelineVersion, CatalogNormalizer.TaxonomyVersion, CatalogNormalizer.TargetProfileVersion));

    private static string BuildReport(NormalizedCatalog catalog)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Relatório de intake — PU-ECF-003").AppendLine();
        builder.AppendLine($"- Itens: {catalog.Items.Count}");
        builder.AppendLine($"- Normalizados: {catalog.Items.Count(item => item.State == "normalized")}");
        builder.AppendLine($"- Aguardando revisão: {catalog.Items.Count(item => item.State == "needs_review")}");
        builder.AppendLine($"- Identidades legadas preservadas no registry: {catalog.PreservedLegacyIdentities.Count}");
        builder.AppendLine($"- Identidades legadas vinculadas por slug exato: {catalog.MatchedLegacyIdentities.Count}");
        builder.AppendLine($"- Identidades legadas ainda sem vínculo: {catalog.UnresolvedLegacyIdentities.Count}");
        builder.AppendLine("- Chamadas externas/custo: nenhuma / USD 0").AppendLine();
        builder.AppendLine("### Legados ainda sem vínculo (gate humano)").AppendLine();
        foreach (var identity in catalog.UnresolvedLegacyIdentities)
            builder.AppendLine($"- `{identity.Slug}` — {identity.Name} ({identity.Id})");
        builder.AppendLine();
        builder.AppendLine("## Gate humano").AppendLine();
        if (catalog.Issues.Count == 0) builder.AppendLine("Nenhuma ambiguidade detectada.");
        foreach (var issue in catalog.Issues)
            builder.AppendLine($"- **{issue.Code}** [{string.Join(", ", issue.ExternalKeys)}]: {issue.Message}");
        builder.AppendLine().AppendLine("## Impacto da taxonomia").AppendLine();
        builder.AppendLine($"- Grupos presentes: {string.Join(", ", catalog.TaxonomyImpact.GroupsInInput)}");
        builder.AppendLine($"- Grupos novos para os filtros mobile atuais: {string.Join(", ", catalog.TaxonomyImpact.GroupsOutsideCurrentMobileTaxonomy)}");
        builder.AppendLine($"- Itens sem grupo: {catalog.TaxonomyImpact.ItemsWithoutGroup}");
        builder.AppendLine($"- Equipamentos presentes: {string.Join(", ", catalog.TaxonomyImpact.EquipmentInInput)}");
        builder.AppendLine($"- Equipamentos fora da proposta: {string.Join(", ", catalog.TaxonomyImpact.EquipmentOutsideProposedTaxonomy)}");
        builder.AppendLine($"- Itens sem equipamento: {catalog.TaxonomyImpact.ItemsWithoutEquipment}");
        builder.AppendLine("- Origem dos equipamentos ausentes: o inventário documental v1 não forneceu esse campo; o preenchimento foi intencionalmente diferido para enriquecimento e revisão humana, portanto não representa perda durante o intake.");
        builder.AppendLine().AppendLine("Este relatório não aprova merges, aliases ou mudanças na taxonomia.");
        return builder.ToString();
    }

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record IntakeResult(FactoryRun Run, NormalizedCatalog Catalog, bool CacheHit);
