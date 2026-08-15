using PersonalUltra.ExerciseCatalogFactory.Cli;
using PersonalUltra.ExerciseCatalogFactory.Persistence;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class FactoryApplicationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"personal-ultra-cli-{Guid.NewGuid():N}");

    [Fact]
    public async Task Import_and_resume_use_stored_source_without_original_file()
    {
        Directory.CreateDirectory(_root);
        var sourcePath = Path.Combine(_root, "input.json");
        await File.WriteAllTextAsync(sourcePath, ValidInputJson);
        var workspace = Path.Combine(_root, "workspace");
        var (application, output, error) = CreateApplication(workspace);

        var importExit = await application.RunAsync(["import", "--file", sourcePath]);
        var run = Assert.Single(await new RunStore(workspace).LoadAllAsync());
        File.Delete(sourcePath);
        var resumeExit = await application.RunAsync(["import", "--resume", run.RunId]);
        var resumed = await new RunStore(workspace).LoadAsync(run.RunId);

        Assert.Equal(0, importExit);
        Assert.Equal(0, resumeExit);
        Assert.NotNull(resumed);
        Assert.Equal(1, resumed.ResumeCount);
        Assert.Contains("Source imutável verificado", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Resume_rejects_tampered_stored_source()
    {
        Directory.CreateDirectory(_root);
        var sourcePath = Path.Combine(_root, "input.json");
        await File.WriteAllTextAsync(sourcePath, ValidInputJson);
        var workspace = Path.Combine(_root, "workspace");
        var (application, _, error) = CreateApplication(workspace);
        Assert.Equal(0, await application.RunAsync(["import", "--file", sourcePath]));
        var run = Assert.Single(await new RunStore(workspace).LoadAllAsync());
        await File.WriteAllTextAsync(Path.Combine(workspace, "runs", run.RunId, "source", "input.json"), "tampered");

        var exitCode = await application.RunAsync(["import", "--resume", run.RunId]);

        Assert.Equal(2, exitCode);
        Assert.Contains("integridade", error.ToString());
    }

    [Fact]
    public async Task Doctor_reports_local_ready_and_external_pending_separately()
    {
        var workspace = Path.Combine(_root, "workspace");
        var (application, output, error) = CreateApplication(workspace);

        var exitCode = await application.RunAsync(["doctor"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Readiness local: READY", output.ToString());
        Assert.Contains("Integrações externas: PENDING", output.ToString());
        Assert.Contains("ai-api-key", output.ToString());
        Assert.DoesNotContain("sk-", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task Doctor_blocks_invalid_local_configuration()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var invalidSettings = new PersonalUltra.ExerciseCatalogFactory.Configuration.FactorySettings(
            Path.Combine(_root, "workspace"), "invalid", null, null, "", "http://invalid", "", false, 0,
            null, null, null);
        var application = new FactoryApplication(invalidSettings, output, error);

        var exitCode = await application.RunAsync(["doctor"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Readiness local: BLOCKED", output.ToString());
    }

    [Fact]
    public async Task Import_rejects_invalid_contract_before_creating_a_run()
    {
        Directory.CreateDirectory(_root);
        var sourcePath = Path.Combine(_root, "invalid.json");
        await File.WriteAllTextAsync(sourcePath, "{ \"schemaVersion\": 1, \"source\": \"batch\", \"items\": [{ \"name\": \"\" }] }");
        var workspace = Path.Combine(_root, "workspace");
        var (application, _, error) = CreateApplication(workspace);

        var exitCode = await application.RunAsync(["import", "--file", sourcePath]);

        Assert.Equal(2, exitCode);
        Assert.Contains("invalid.json [items[0].name]", error.ToString());
        Assert.False(Directory.Exists(Path.Combine(workspace, "runs")));
    }

    [Fact]
    public async Task Import_initializes_only_source_stage_contracts()
    {
        Directory.CreateDirectory(_root);
        var sourcePath = Path.Combine(_root, "valid.json");
        await File.WriteAllTextAsync(sourcePath, ValidInputJson);
        var workspace = Path.Combine(_root, "workspace");
        var (application, _, _) = CreateApplication(workspace);

        Assert.Equal(0, await application.RunAsync(["import", "--file", sourcePath]));
        var run = Assert.Single(await new RunStore(workspace).LoadAllAsync());

        Assert.Empty(run.Items!);
        Assert.Equal(run.Source.Sha256, Assert.Single(run.StageHashes!).Value);
        Assert.Equal("pending", run.Versions!.TaxonomyVersion);
        Assert.Empty(run.Attempts!);
        Assert.Empty(run.Outputs!);
    }

    [Theory]
    [InlineData("import", "--file")]
    [InlineData("status", "unexpected")]
    [InlineData("status", "--run", "../escape")]
    [InlineData("init", "--unknown", "value")]
    public async Task Cli_rejects_malformed_or_unsafe_arguments(params string[] args)
    {
        var (application, _, error) = CreateApplication(Path.Combine(_root, "workspace"));

        var exitCode = await application.RunAsync(args);

        Assert.Equal(2, exitCode);
        Assert.NotEqual(string.Empty, error.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static (FactoryApplication Application, StringWriter Output, StringWriter Error) CreateApplication(string workspace)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var settings = FactorySettingsTests.CreateSettings(workspace);
        return (new FactoryApplication(settings, output, error), output, error);
    }

    private const string ValidInputJson = """
        { "schemaVersion": 1, "source": "test", "items": [{ "externalKey": "bench", "name": "Supino" }] }
        """;
}
