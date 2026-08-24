using System.Text.Json;
using PersonalUltra.ExerciseCatalogFactory.Cli;
using PersonalUltra.ExerciseCatalogFactory.Persistence;
using PersonalUltra.ExerciseCatalogFactory.Providers.Text;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class MetadataEnrichmentTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"personal-ultra-metadata-{Guid.NewGuid():N}");

    [Fact]
    public async Task Dry_run_plans_without_calling_provider_or_changing_manifest()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var before = await File.ReadAllBytesAsync(Path.Combine(workspace, "runs", runId, "manifest.json"));
        var provider = new FakeProvider();
        var (application, output, error) = CreateApplication(workspace, provider);

        var exit = await application.RunAsync(["metadata", "enrich", "--run", runId, "--max-items", "1", "--max-cost", "1"]);

        Assert.Equal(0, exit);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(before, await File.ReadAllBytesAsync(Path.Combine(workspace, "runs", runId, "manifest.json")));
        Assert.Contains("Dry-run", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Execute_preserves_locked_fields_and_never_auto_approves()
    {
        var input = """
            { "schemaVersion": 1, "source": "test", "items": [{
              "externalKey": "bench", "name": "Supino confiável", "aliases": ["Supino"],
              "primaryMuscleGroup": "Peito", "equipment": "Barra",
              "instructionsHint": "Mantenha os pés apoiados.", "visualHint": "Banco horizontal e barra.",
              "lockedFields": ["name", "aliases", "primaryMuscleGroup", "equipment", "instructionsHint", "visualHint"]
            }] }
            """;
        var (runId, workspace) = await ImportAsync(input);
        var provider = new FakeProvider();
        var (application, output, error) = CreateApplication(workspace, provider);

        var exit = await application.RunAsync(["metadata", "enrich", "--run", runId, "--max-items", "1", "--max-cost", "1", "--execute"]);

        var run = await new RunStore(workspace).LoadAsync(runId);
        var item = Assert.Single(run!.Items!);
        Assert.Equal(0, exit);
        Assert.Equal("metadata_review", item.State);
        Assert.DoesNotContain(item.Reviews!, review => review.Decision == "approved");
        var artifact = Assert.Single(item.Artifacts!, value => value.Stage == "metadata");
        var json = await new RunStore(workspace).ReadVerifiedArtifactAsync(runId, artifact);
        var proposal = JsonSerializer.Deserialize<MetadataProposalArtifact>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Supino confiável", proposal!.Proposal.CanonicalName);
        Assert.Equal("Peito", proposal.Proposal.PrimaryMuscleGroup);
        Assert.Equal("Mantenha os pés apoiados.", proposal.Proposal.Instructions);
        Assert.Contains("aguardam revisão humana", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Retry_is_bounded_and_resume_does_not_repeat_success()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var provider = new FakeProvider(failuresBeforeSuccess: 1);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        var store = new RunStore(workspace);
        var run = (await store.LoadAsync(runId))!;
        var processor = new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero);

        var first = await processor.ExecuteAsync(run, new MetadataExecutionOptions(1, 1, true));
        var second = await processor.ExecuteAsync(first.Run, new MetadataExecutionOptions(1, 1, true));

        Assert.Equal(2, provider.Calls);
        Assert.Equal(1, first.Generated);
        Assert.Equal(0, second.Generated);
        Assert.Equal(2, first.Run.Attempts!.Count);
        Assert.Equal(["failed_retryable", "succeeded"], first.Run.Attempts.Select(value => value.Status));
    }

    [Fact]
    public async Task Cancellation_after_retryable_checkpoint_resumes_only_remaining_lifetime_attempts_and_budget()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        var store = new RunStore(workspace);
        using var cancellation = new CancellationTokenSource();
        var cancellingProvider = new AlwaysRetryableProvider();
        var firstProcessor = new MetadataEnrichmentProcessor(store, settings, cancellingProvider, _ =>
        {
            cancellation.Cancel();
            return TimeSpan.FromMinutes(1);
        });
        var initial = (await store.LoadAsync(runId))!;

        await Assert.ThrowsAsync<TaskCanceledException>(() => firstProcessor.ExecuteAsync(initial,
            new MetadataExecutionOptions(1, 0.03m, true), cancellation.Token));
        var checkpoint = (await store.LoadAsync(runId))!;
        Assert.Equal("failed_retryable", Assert.Single(checkpoint.Attempts!).Status);
        Assert.Equal(0.01m, checkpoint.Usage!.EstimatedCost);

        var remainingProvider = new AlwaysRetryableProvider();
        var resumed = await new MetadataEnrichmentProcessor(store, settings, remainingProvider, _ => TimeSpan.Zero)
            .ExecuteAsync(checkpoint, new MetadataExecutionOptions(1, 0.03m, true));
        var finalResume = await new MetadataEnrichmentProcessor(store, settings, remainingProvider, _ => TimeSpan.Zero)
            .ExecuteAsync(resumed.Run, new MetadataExecutionOptions(1, 0.03m, true));

        Assert.Equal(2, remainingProvider.Calls);
        Assert.Equal(3, resumed.Run.Attempts!.Count);
        Assert.Equal(0.03m, resumed.Run.Usage!.EstimatedCost);
        Assert.All(resumed.Run.Attempts!, attempt => Assert.InRange(attempt.Attempt, 1, 3));
        Assert.Equal(0, finalResume.Planned);
    }

    [Fact]
    public async Task Orphan_artifact_is_recovered_without_second_provider_call_or_logical_charge()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        var store = new RunStore(workspace);
        var provider = new FakeProvider();
        var crashing = new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero,
            (_, _) => throw new OperationCanceledException("crash after durable artifact"));
        var initial = (await store.LoadAsync(runId))!;

        await Assert.ThrowsAsync<OperationCanceledException>(() => crashing.ExecuteAsync(initial,
            new MetadataExecutionOptions(1, 0.03m, true)));
        var orphaned = (await store.LoadAsync(runId))!;
        Assert.Equal("started", Assert.Single(orphaned.Attempts!).Status);

        var recovered = await new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero)
            .ExecuteAsync(orphaned, new MetadataExecutionOptions(1, 0.03m, true));

        Assert.Equal(1, provider.Calls);
        Assert.Equal(1, recovered.CacheHits);
        Assert.Equal("succeeded", Assert.Single(recovered.Run.Attempts!).Status);
        Assert.Equal(1, recovered.Run.Usage!.Attempts);
        Assert.Equal(0.01m, recovered.Run.Usage.EstimatedCost);
        Assert.Equal("metadata_review", Assert.Single(recovered.Run.Items!).State);
    }

    [Fact]
    public async Task Uncertain_attempt_without_explicit_confirmation_blocks_with_zero_redispatch()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        var store = new RunStore(workspace);
        var provider = new AmbiguousCancellationProvider();
        var processor = new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero);
        var initial = (await store.LoadAsync(runId))!;

        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ExecuteAsync(initial,
            new MetadataExecutionOptions(1, 0.03m, true)));
        var inFlight = (await store.LoadAsync(runId))!;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ExecuteAsync(inFlight, new MetadataExecutionOptions(1, 0.03m, true)));

        Assert.Single(provider.Calls);
        Assert.Contains("--retry-uncertain", exception.Message);
        Assert.Equal("started", Assert.Single((await store.LoadAsync(runId))!.Attempts!).Status);
    }

    [Fact]
    public async Task Explicit_uncertain_retry_closes_old_attempt_and_creates_new_charge_reservation()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        var store = new RunStore(workspace);
        var provider = new AmbiguousCancellationProvider();
        var processor = new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero);
        var initial = (await store.LoadAsync(runId))!;
        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ExecuteAsync(initial,
            new MetadataExecutionOptions(1, 0.03m, true)));

        var resumed = await processor.ExecuteAsync((await store.LoadAsync(runId))!,
            new MetadataExecutionOptions(1, 0.03m, true, RetryUncertain: true));

        Assert.Equal(2, provider.Calls.Count);
        Assert.NotEqual(provider.Calls[0].Key, provider.Calls[1].Key);
        Assert.Equal((1, 2), (provider.Calls[0].Attempt, provider.Calls[1].Attempt));
        Assert.Equal(["failed_uncertain", "succeeded"], resumed.Run.Attempts!.Select(attempt => attempt.Status));
        Assert.Equal(2, resumed.Run.Usage!.Attempts);
        Assert.Equal(0.02m, resumed.Run.Usage.EstimatedCost);
    }

    [Fact]
    public async Task Explicit_uncertain_retry_is_blocked_when_max_attempts_is_exhausted()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key", metadataMaxAttempts: 1);
        var store = new RunStore(workspace);
        var provider = new AmbiguousCancellationProvider();
        var processor = new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero);
        var initial = (await store.LoadAsync(runId))!;
        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ExecuteAsync(initial,
            new MetadataExecutionOptions(1, 0.01m, true)));

        var uncertain = (await store.LoadAsync(runId))!;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ExecuteAsync(
            uncertain, new MetadataExecutionOptions(1, 0.01m, true, RetryUncertain: true)));

        Assert.Single(provider.Calls);
        Assert.Contains("MaxAttempts", exception.Message);
        var final = (await store.LoadAsync(runId))!;
        Assert.Equal("failed_uncertain", Assert.Single(final.Attempts!).Status);
        Assert.Equal(0.01m, final.Usage!.EstimatedCost);
    }

    [Fact]
    public async Task Worst_case_retry_reservation_blocks_calls_above_budget()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var provider = new FakeProvider();
        var (application, _, error) = CreateApplication(workspace, provider);

        var exit = await application.RunAsync(["metadata", "enrich", "--run", runId, "--max-items", "1", "--max-cost", "0.01", "--execute"]);

        Assert.Equal(2, exit);
        Assert.Equal(0, provider.Calls);
        Assert.Contains("acima de --max-cost", error.ToString());
    }

    [Fact]
    public async Task Cli_rejects_retry_uncertain_without_execute_confirmation()
    {
        var (runId, workspace) = await ImportAsync(BasicInput);
        var provider = new FakeProvider();
        var (application, _, error) = CreateApplication(workspace, provider);

        var exit = await application.RunAsync(["metadata", "enrich", "--run", runId, "--max-items", "1",
            "--max-cost", "1", "--retry-uncertain"]);

        Assert.Equal(2, exit);
        Assert.Equal(0, provider.Calls);
        Assert.Contains("exige confirmação conjunta", error.ToString());
    }

    [Fact]
    public async Task Intake_ambiguity_is_not_sent_or_resolved_silently()
    {
        var ambiguous = """
            { "schemaVersion": 1, "source": "test", "items": [{ "externalKey": "squat", "name": "Agachamento livre com barra" }] }
            """;
        var (runId, workspace) = await ImportAsync(ambiguous);
        var provider = new FakeProvider();
        var (application, _, error) = CreateApplication(workspace, provider);

        var exit = await application.RunAsync(["metadata", "enrich", "--run", runId, "--max-items", "1", "--max-cost", "1", "--execute"]);
        var run = await new RunStore(workspace).LoadAsync(runId);

        Assert.Equal(0, exit);
        Assert.Equal(0, provider.Calls);
        Assert.Equal("needs_review", Assert.Single(run!.Items!).State);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Terminal_failure_of_one_item_does_not_discard_or_block_next_item()
    {
        var twoItems = """
            { "schemaVersion": 1, "source": "test", "items": [
              { "externalKey": "bench", "name": "Supino" },
              { "externalKey": "row", "name": "Remada unilateral com halter" }
            ] }
            """;
        var (runId, workspace) = await ImportAsync(twoItems);
        var provider = new FakeProvider(failuresBeforeSuccess: 3);
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        var store = new RunStore(workspace);
        var processor = new MetadataEnrichmentProcessor(store, settings, provider, _ => TimeSpan.Zero);

        var result = await processor.ExecuteAsync((await store.LoadAsync(runId))!, new MetadataExecutionOptions(2, 1, true));

        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Generated);
        Assert.Contains(result.Run.Items!, item => item.State == "failed_terminal");
        Assert.Contains(result.Run.Items!, item => item.State == "metadata_review");
    }

    [Fact]
    public void Strict_validator_rejects_unknown_property_and_taxonomy_value()
    {
        var valid = JsonSerializer.Serialize(FakeProvider.ValidProposal, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var unknown = valid[..^1] + ",\"extra\":true}";
        var invalidTaxonomy = valid.Replace("Peito", "Grupo inventado", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => MetadataProposalValidator.ParseAndValidate(unknown));
        Assert.Throws<InvalidDataException>(() => MetadataProposalValidator.ParseAndValidate(invalidTaxonomy));
    }

    [Fact]
    public void OpenAi_payload_uses_strict_json_schema_and_configured_variability()
    {
        var request = new MetadataRequest("bench", "Supino", [], null, null, null, null,
            new HashSet<string>(), "gpt-test", "exercise-metadata-v1", "Strict test prompt.", 0.25m);

        var json = JsonSerializer.Serialize(OpenAiMetadataProvider.BuildPayload(request));

        Assert.Contains("\"type\":\"json_schema\"", json);
        Assert.Contains("\"strict\":true", json);
        Assert.Contains("\"additionalProperties\":false", json);
        Assert.Contains("\"temperature\":0.25", json);
        Assert.DoesNotContain("api-key", json, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private async Task<(string RunId, string Workspace)> ImportAsync(string input)
    {
        Directory.CreateDirectory(_root);
        var source = Path.Combine(_root, $"input-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(source, input);
        var workspace = Path.Combine(_root, $"workspace-{Guid.NewGuid():N}");
        var (application, _, _) = CreateApplication(workspace, new FakeProvider());
        Assert.Equal(0, await application.RunAsync(["import", "--file", source]));
        return (Assert.Single(await new RunStore(workspace).LoadAllAsync()).RunId, workspace);
    }

    private static (FactoryApplication Application, StringWriter Output, StringWriter Error) CreateApplication(
        string workspace, IMetadataProvider provider)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var settings = FactorySettingsTests.CreateSettings(workspace, openAiApiKey: "test-key");
        return (new FactoryApplication(settings, output, error, provider), output, error);
    }

    private const string BasicInput = """
        { "schemaVersion": 1, "source": "test", "items": [{ "externalKey": "bench", "name": "Supino" }] }
        """;

    private sealed class FakeProvider(int failuresBeforeSuccess = 0) : IMetadataProvider
    {
        public static readonly MetadataProposal ValidProposal = new("Supino reto com barra", ["Supino"], "Peito", "Barra",
            "Mantenha os pés apoiados e controle o movimento.", "Pessoa em banco horizontal usando uma barra.", [],
            new MetadataConfidence("high", "high"));

        public int Calls { get; private set; }

        public Task<MetadataProviderResult> GenerateAsync(MetadataRequest request, ProviderCallContext call, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls <= failuresBeforeSuccess)
                throw new MetadataProviderException("transient sk-must-not-leak", retryable: true);
            return Task.FromResult(new MetadataProviderResult(ValidProposal, "resp_test", 100, 50, 0.001m));
        }
    }

    private sealed class AlwaysRetryableProvider : IMetadataProvider
    {
        public int Calls { get; private set; }
        public Task<MetadataProviderResult> GenerateAsync(MetadataRequest request, ProviderCallContext call, CancellationToken cancellationToken)
        {
            Calls++;
            throw new MetadataProviderException("retryable", retryable: true);
        }
    }

    private sealed class AmbiguousCancellationProvider : IMetadataProvider
    {
        public List<(string Key, int Attempt)> Calls { get; } = [];
        public Task<MetadataProviderResult> GenerateAsync(MetadataRequest request, ProviderCallContext call, CancellationToken cancellationToken)
        {
            Calls.Add((call.IdempotencyKey, call.Attempt));
            if (Calls.Count == 1) throw new OperationCanceledException("transport interrupted after dispatch");
            return Task.FromResult(new MetadataProviderResult(FakeProvider.ValidProposal, "resp_replayed", 100, 50, 0.001m));
        }
    }
}
