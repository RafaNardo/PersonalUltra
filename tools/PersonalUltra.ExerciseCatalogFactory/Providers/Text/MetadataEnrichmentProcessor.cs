using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Domain;
using PersonalUltra.ExerciseCatalogFactory.Intake;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Providers.Text;

public sealed record MetadataExecutionOptions(int MaxItems, decimal MaxCostUsd, bool Execute, bool RetryUncertain = false);
public sealed record MetadataExecutionResult(FactoryRun Run, int Planned, int Generated, int CacheHits, int Failed, decimal EstimatedCostUsd);

public sealed record MetadataProposalArtifact(
    int SchemaVersion,
    string ExternalKey,
    string Provider,
    string Model,
    string PromptVersion,
    string InputHash,
    IReadOnlyList<string> LockedFields,
    MetadataProposal Proposal,
    string? RequestId,
    int? InputTokens,
    int? OutputTokens,
    decimal? ObservedCostUsd);

public sealed class MetadataEnrichmentProcessor(
    RunStore store,
    FactorySettings settings,
    IMetadataProvider provider,
    Func<int, TimeSpan>? retryDelay = null,
    Func<string, CancellationToken, Task>? afterArtifactSaved = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly Func<int, TimeSpan> _retryDelay = retryDelay ?? (attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
    private readonly Func<string, CancellationToken, Task>? _afterArtifactSaved = afterArtifactSaved;

    public async Task<MetadataExecutionResult> ExecuteAsync(
        FactoryRun run,
        MetadataExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options.MaxItems < 1) throw new ArgumentException("--max-items deve ser maior que zero.");
        if (options.MaxCostUsd <= 0) throw new ArgumentException("--max-cost deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(settings.MetadataModel))
            throw new InvalidOperationException("Configure OpenAI:MetadataModel antes do enriquecimento, inclusive para planejar o run.");

        await store.VerifySourceAsync(run, cancellationToken);
        var normalizedArtifact = (run.Outputs ?? []).SingleOrDefault(output =>
            output.Stage == "normalization" && output.RelativePath == "normalization/catalog.normalized.v1.json")
            ?? throw new InvalidOperationException("Execute o intake antes do enriquecimento.");
        var normalizedBytes = await store.ReadVerifiedArtifactAsync(run.RunId, normalizedArtifact, cancellationToken);
        var catalog = JsonSerializer.Deserialize<NormalizedCatalog>(normalizedBytes, JsonOptions)
            ?? throw new InvalidDataException("Catálogo normalizado vazio.");
        var sourceRows = await CatalogInputReader.ReadAsync(store.GetStoredSourcePath(run), cancellationToken);
        var rowsByNumber = sourceRows.ToDictionary(row => row.Row);
        var normalizedByKey = catalog.Items.ToDictionary(item => item.ExternalKey, StringComparer.Ordinal);

        var candidates = new List<(ManifestItem Item, MetadataRequest Request, string InputHash)>();
        foreach (var item in (run.Items ?? []).Where(item => item.State is "normalized" or "metadata_pending" or "failed_retryable")
                     .OrderBy(item => item.ExternalKey, StringComparer.Ordinal))
        {
            if (normalizedByKey.GetValueOrDefault(item.ExternalKey) is not { State: "normalized" } normalized) continue;
            if (!rowsByNumber.TryGetValue(normalized.SourceRow, out var sourceRow))
                throw new InvalidDataException($"Linha de origem ausente para {item.ExternalKey}.");
            var request = BuildRequest(normalized, sourceRow.Item, settings.MetadataModel!, settings.MetadataPromptVersion,
                settings.MetadataTemperature);
            candidates.Add((item, request, ComputeInputHash(request)));
            if (candidates.Count == options.MaxItems) break;
        }

        var estimated = candidates.Sum(candidate => LifetimeBudgetFor(run, candidate.Item.ExternalKey, candidate.InputHash));
        if (estimated > options.MaxCostUsd)
            throw new InvalidOperationException($"Plano lifetime estimado em USD {estimated:F4}, acima de --max-cost USD {options.MaxCostUsd:F4}.");
        if (!options.Execute)
            return new MetadataExecutionResult(run, candidates.Count, 0, 0, 0, estimated);

        var current = run with { DryRun = false, Status = "processing", UpdatedAt = DateTimeOffset.UtcNow };
        await store.SaveAsync(current, cancellationToken);
        var generated = 0;
        var cacheHits = 0;
        var failed = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await GenerateItemAsync(current, candidate.Item, candidate.Request, candidate.InputHash,
                options.RetryUncertain, cancellationToken);
            current = result.Run;
            generated += result.Generated ? 1 : 0;
            cacheHits += result.CacheHit ? 1 : 0;
            failed += result.Failed ? 1 : 0;
        }

        var items = current.Items ?? [];
        var finalStatus = items.Any(item => item.State is "failed_retryable" or "failed_terminal") ? "partial" :
            items.Any(item => item.State is "needs_review" or "metadata_review") ? "needs_review" : "ready";
        current = current with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            Status = finalStatus,
            Versions = (current.Versions ?? throw new InvalidDataException("Versões do run ausentes.")) with
            {
                MetadataPromptVersion = settings.MetadataPromptVersion
            }
        };
        await store.SaveAsync(current, cancellationToken);
        return new MetadataExecutionResult(current, candidates.Count, generated, cacheHits, failed, estimated);
    }

    private decimal LifetimeBudgetFor(FactoryRun run, string itemKey, string inputHash)
    {
        var attempts = RelevantAttempts(run, itemKey, inputHash);
        var reserved = attempts.Sum(attempt => attempt.Cost.Estimated ?? settings.MetadataEstimatedCostUsd);
        var remaining = Math.Max(0, settings.MetadataMaxAttempts - attempts.Count);
        return reserved + remaining * settings.MetadataEstimatedCostUsd;
    }

    private async Task<ItemResult> GenerateItemAsync(
        FactoryRun run,
        ManifestItem originalItem,
        MetadataRequest request,
        string inputHash,
        bool retryUncertain,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var attempts = RelevantAttempts(run, originalItem.ExternalKey, inputHash);
            var inFlight = attempts.LastOrDefault(attempt => attempt.Status == "started");
            if (inFlight is not null)
            {
                var recovered = await TryRecoverArtifactAsync(run, originalItem, request, inputHash, inFlight, cancellationToken);
                if (recovered is not null) return new ItemResult(recovered, Generated: false, CacheHit: true, Failed: false);
                if (!retryUncertain)
                    throw new InvalidOperationException(
                        $"Tentativa incerta detectada para {originalItem.ExternalKey}. Nenhuma nova chamada foi feita. " +
                        "Verifique o provider; para assumir possível cobrança anterior e criar uma nova tentativa, use --execute --retry-uncertain.");

                var uncertain = inFlight with { FinishedAt = DateTimeOffset.UtcNow, Status = "failed_uncertain" };
                run = UpsertAttempt(run, uncertain);
                await store.SaveAsync(run, cancellationToken);
                if (RelevantAttempts(run, originalItem.ExternalKey, inputHash).Count >= settings.MetadataMaxAttempts)
                {
                    var exhausted = ReplaceItem(run, originalItem with { State = "failed_terminal" });
                    await store.SaveAsync(exhausted, cancellationToken);
                    throw new InvalidOperationException(
                        $"Tentativa incerta contabilizada para {originalItem.ExternalKey}, mas MaxAttempts foi atingido. Nenhuma nova chamada foi feita.");
                }
                inFlight = null;
            }

            ProviderAttempt attempt;
            if (inFlight is not null)
            {
                attempt = inFlight;
            }
            else
            {
                var nextNumber = attempts.Count == 0 ? 1 : attempts.Max(value => value.Attempt) + 1;
                if (nextNumber > settings.MetadataMaxAttempts)
                {
                    var exhausted = ReplaceItem(run, originalItem with { State = "failed_terminal" });
                    await store.SaveAsync(exhausted, cancellationToken);
                    return new ItemResult(exhausted, false, false, true);
                }

                attempt = new ProviderAttempt("metadata", originalItem.ExternalKey, "openai", request.Model,
                    CreateIdempotencyKey(run.RunId, originalItem.ExternalKey, inputHash, nextNumber), null, nextNumber,
                    DateTimeOffset.UtcNow, null, inputHash, request.PromptVersion, "started",
                    new ProviderCost("USD", settings.MetadataEstimatedCostUsd, null));
                var pendingItem = originalItem with
                {
                    State = "metadata_pending",
                    Hashes = originalItem.Hashes with { MetadataInput = inputHash }
                };
                run = UpsertAttempt(ReplaceItem(run, pendingItem), attempt);
                await store.SaveAsync(run, cancellationToken); // durable pre-call checkpoint
            }

            try
            {
                var result = await provider.GenerateAsync(request,
                    new ProviderCallContext(attempt.IdempotencyKey, attempt.Attempt), cancellationToken);
                var finalProposal = ApplyLockedFields(result.Proposal, request);
                ValidateLockedFields(finalProposal, request);
                var artifactDocument = new MetadataProposalArtifact(1, originalItem.ExternalKey, "openai", request.Model,
                    request.PromptVersion, inputHash, request.LockedFields.Order(StringComparer.Ordinal).ToArray(),
                    finalProposal, SafeRequestId(result.RequestId), result.InputTokens, result.OutputTokens,
                    result.ObservedCostUsd);
                var artifact = await store.SaveArtifactAsync(run.RunId, "metadata", ArtifactPath(originalItem.ExternalKey),
                    JsonSerializer.SerializeToUtf8Bytes(artifactDocument, JsonOptions), cancellationToken);
                if (_afterArtifactSaved is not null) await _afterArtifactSaved(artifact.RelativePath, cancellationToken);
                var succeeded = FinalizeSuccess(run, originalItem, inputHash, attempt, artifactDocument, artifact);
                await store.SaveAsync(succeeded, cancellationToken);
                return new ItemResult(succeeded, true, false, false);
            }
            catch (MetadataProviderException exception)
            {
                var canRetry = exception.Retryable && attempt.Attempt < settings.MetadataMaxAttempts;
                var status = canRetry ? "failed_retryable" : "failed_terminal";
                var finished = attempt with { FinishedAt = DateTimeOffset.UtcNow, Status = status };
                run = UpsertAttempt(ReplaceItem(run, originalItem with
                {
                    State = status,
                    Hashes = originalItem.Hashes with { MetadataInput = inputHash }
                }), finished);
                await store.SaveAsync(run, cancellationToken);
                if (!canRetry) return new ItemResult(run, false, false, true);
                await Task.Delay(_retryDelay(attempt.Attempt), cancellationToken);
            }
            catch (InvalidDataException)
            {
                var finished = attempt with { FinishedAt = DateTimeOffset.UtcNow, Status = "failed_terminal" };
                run = UpsertAttempt(ReplaceItem(run, originalItem with
                {
                    State = "failed_terminal",
                    Hashes = originalItem.Hashes with { MetadataInput = inputHash }
                }), finished);
                await store.SaveAsync(run, cancellationToken);
                return new ItemResult(run, false, false, true);
            }
        }
    }

    private async Task<FactoryRun?> TryRecoverArtifactAsync(FactoryRun run, ManifestItem item, MetadataRequest request,
        string inputHash, ProviderAttempt attempt, CancellationToken cancellationToken)
    {
        var artifact = await store.TryReferenceArtifactAsync(run.RunId, "metadata", ArtifactPath(item.ExternalKey), cancellationToken);
        if (artifact is null) return null;
        try
        {
            var bytes = await store.ReadVerifiedArtifactAsync(run.RunId, artifact, cancellationToken);
            var document = JsonSerializer.Deserialize<MetadataProposalArtifact>(bytes, JsonOptions)
                ?? throw new InvalidDataException("Artefato de metadados vazio.");
            if (document.SchemaVersion != 1 || document.ExternalKey != item.ExternalKey || document.InputHash != inputHash ||
                document.Model != request.Model || document.PromptVersion != request.PromptVersion)
                return null;
            ValidateLockedFields(document.Proposal, request);
            var recovered = FinalizeSuccess(run, item, inputHash, attempt, document, artifact);
            await store.SaveAsync(recovered, cancellationToken);
            return recovered;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return null;
        }
    }

    private static FactoryRun FinalizeSuccess(FactoryRun run, ManifestItem item, string inputHash,
        ProviderAttempt attempt, MetadataProposalArtifact document, ArtifactReference artifact)
    {
        var succeededAttempt = attempt with
        {
            RequestId = SafeRequestId(document.RequestId),
            FinishedAt = DateTimeOffset.UtcNow,
            Status = "succeeded",
            Cost = attempt.Cost with { Observed = document.ObservedCostUsd },
            Usage = new ProviderUsage(document.InputTokens, document.OutputTokens, null)
        };
        var updatedItem = item with
        {
            State = "metadata_review",
            Hashes = item.Hashes with { MetadataInput = inputHash, ImageInput = null, ExportInput = null },
            Artifacts = (item.Artifacts ?? []).Where(value => value.Stage != "metadata").Append(artifact).ToArray(),
            Reviews = (item.Reviews ?? []).Where(review => review.Stage != "metadata").ToArray()
        };
        return UpsertAttempt(ReplaceItem(run, updatedItem), succeededAttempt);
    }

    private static FactoryRun ReplaceItem(FactoryRun run, ManifestItem updatedItem) => run with
    {
        UpdatedAt = DateTimeOffset.UtcNow,
        Items = (run.Items ?? []).Select(item => item.ExternalKey == updatedItem.ExternalKey ? updatedItem : item).ToArray()
    };

    private static FactoryRun UpsertAttempt(FactoryRun run, ProviderAttempt updatedAttempt)
    {
        var attempts = (run.Attempts ?? []).Where(attempt => !(attempt.Stage == updatedAttempt.Stage &&
            attempt.ItemKey == updatedAttempt.ItemKey && attempt.InputHash == updatedAttempt.InputHash &&
            attempt.Attempt == updatedAttempt.Attempt)).Append(updatedAttempt).OrderBy(attempt => attempt.StartedAt).ToArray();
        return run with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            Attempts = attempts,
            Usage = new UsageSummary("USD", attempts.Sum(value => value.Cost.Estimated ?? 0),
                attempts.Sum(value => value.Cost.Observed ?? 0), attempts.Length)
        };
    }

    private static IReadOnlyList<ProviderAttempt> RelevantAttempts(FactoryRun run, string itemKey, string inputHash) =>
        (run.Attempts ?? []).Where(attempt => attempt.Stage == "metadata" && attempt.ItemKey == itemKey &&
            attempt.InputHash == inputHash).OrderBy(attempt => attempt.Attempt).ToArray();

    internal static MetadataRequest BuildRequest(NormalizedExercise normalized, CatalogInputItem source,
        string model, string promptVersion, decimal temperature)
    {
        var locked = (source.LockedFields ?? []).Select(NormalizeLockedField).ToHashSet(StringComparer.Ordinal);
        var promptText = MetadataPromptCatalog.Load(promptVersion);
        return new MetadataRequest(normalized.ExternalKey, normalized.CanonicalName, normalized.Aliases,
            normalized.PrimaryMuscleGroup, normalized.Equipment, source.InstructionsHint, source.VisualHint,
            locked, model, promptVersion, promptText, temperature);
    }

    internal static MetadataProposal ApplyLockedFields(MetadataProposal proposal, MetadataRequest request) => proposal with
    {
        CanonicalName = request.LockedFields.Contains("canonicalName") ? request.Name : proposal.CanonicalName,
        Aliases = request.LockedFields.Contains("aliases") ? request.Aliases : proposal.Aliases,
        PrimaryMuscleGroup = request.LockedFields.Contains("primaryMuscleGroup") ? request.PrimaryMuscleGroup ?? string.Empty : proposal.PrimaryMuscleGroup,
        Equipment = request.LockedFields.Contains("equipment") ? request.Equipment ?? string.Empty : proposal.Equipment,
        Instructions = request.LockedFields.Contains("instructions") ? request.InstructionsHint ?? string.Empty : proposal.Instructions,
        VisualDescription = request.LockedFields.Contains("visualDescription") ? request.VisualHint ?? string.Empty : proposal.VisualDescription
    };

    private static void ValidateLockedFields(MetadataProposal proposal, MetadataRequest request)
    {
        _ = MetadataProposalValidator.ParseAndValidate(JsonSerializer.Serialize(proposal, JsonOptions));
        if (request.LockedFields.Contains("canonicalName") && proposal.CanonicalName != request.Name ||
            request.LockedFields.Contains("aliases") && !proposal.Aliases.SequenceEqual(request.Aliases) ||
            request.LockedFields.Contains("primaryMuscleGroup") && proposal.PrimaryMuscleGroup != request.PrimaryMuscleGroup ||
            request.LockedFields.Contains("equipment") && proposal.Equipment != request.Equipment ||
            request.LockedFields.Contains("instructions") && proposal.Instructions != request.InstructionsHint ||
            request.LockedFields.Contains("visualDescription") && proposal.VisualDescription != request.VisualHint)
            throw new InvalidDataException("Um campo locked foi alterado.");
    }

    private static string NormalizeLockedField(string field) => field switch
    {
        "name" or "canonicalName" => "canonicalName",
        "aliases" => "aliases",
        "primaryMuscleGroup" or "primary_muscle_group" => "primaryMuscleGroup",
        "equipment" => "equipment",
        "instructions" or "instructionsHint" => "instructions",
        "visualDescription" or "visualHint" => "visualDescription",
        _ => throw new InvalidDataException($"lockedField não suportado: {field}")
    };

    private static string ArtifactPath(string externalKey) => $"metadata/{externalKey}.proposal.v1.json";
    private static string ComputeInputHash(MetadataRequest request) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOptions)))).ToLowerInvariant();
    private static string CreateIdempotencyKey(string runId, string itemKey, string inputHash, int attempt) =>
        "ecf-meta-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{runId}\nmetadata\n{itemKey}\n{inputHash}\n{attempt}"))).ToLowerInvariant();
    private static string? SafeRequestId(string? value) => string.IsNullOrWhiteSpace(value) || value.Length > 128 ||
        value.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) || value.StartsWith("tsec_", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("tid_", StringComparison.OrdinalIgnoreCase) || value.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
        ? null : value;

    private sealed record ItemResult(FactoryRun Run, bool Generated, bool CacheHit, bool Failed);
}

internal static class MetadataPromptCatalog
{
    public static string Load(string version)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
            throw new InvalidDataException("Versão de prompt de metadados inválida.");
        var path = Path.Combine(AppContext.BaseDirectory, "prompts", "metadata", $"{version}.md");
        if (!File.Exists(path)) throw new InvalidDataException($"Prompt versionado não encontrado: {version}.");
        var content = File.ReadAllText(path).Trim();
        if (content.Length is < 20 or > 10_000) throw new InvalidDataException("Prompt versionado vazio ou grande demais.");
        return content;
    }
}
