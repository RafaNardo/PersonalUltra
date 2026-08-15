using PersonalUltra.ExerciseCatalogFactory.Configuration;
using PersonalUltra.ExerciseCatalogFactory.Publishing.S3;

namespace PersonalUltra.ExerciseCatalogFactory.Cli;

internal sealed class BucketCommands(
    FactorySettings settings,
    TextWriter output,
    TextWriter error,
    Func<BucketOptions, BucketCredentials, IObjectStore>? storeFactory = null,
    HttpClient? httpClient = null)
{
    private readonly Func<BucketOptions, BucketCredentials, IObjectStore> _storeFactory =
        storeFactory ?? ((options, credentials) => new S3ObjectStore(options, credentials));
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    internal async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Use 'bucket doctor' ou 'bucket smoke [--execute]'.");
            return 2;
        }

        var command = CommandLine.Parse(args);
        return command.Name switch
        {
            "doctor" => await DoctorAsync(command, cancellationToken),
            "smoke" => await SmokeAsync(command, cancellationToken),
            _ => Unknown(command.Name)
        };
    }

    private async Task<int> DoctorAsync(ParsedCommand command, CancellationToken cancellationToken)
    {
        command.EnsureOnly();
        if (!TryGetConfiguration(out var options, out var credentials)) return 2;

        await using var store = _storeFactory(options!, credentials!);
        await output.WriteLineAsync(
            $"Config: endpointHost={options!.Endpoint.Host} | region={options.Region} | addressing={options.AddressingStyle}");
        var result = await store.ProbeAsync(cancellationToken);
        await output.WriteLineAsync("Bucket doctor: READY");
        await output.WriteLineAsync($"Addressing configurado: {options!.AddressingStyle}");
        await output.WriteLineAsync($"HEAD bucket: OK | requestId={RedactRequestId(result.RequestId)}");
        await output.WriteLineAsync("Operação somente leitura; nenhum objeto foi listado, criado ou removido.");
        return 0;
    }

    private async Task<int> SmokeAsync(ParsedCommand command, CancellationToken cancellationToken)
    {
        command.EnsureOnly("--execute");
        command.EnsureFlag("--execute");
        if (!command.HasOption("--execute"))
        {
            await output.WriteLineAsync("Bucket smoke: DRY-RUN");
            await output.WriteLineAsync(
                "Plano: PUT/HEAD/GET/GET assinado/DELETE/CONFIRM NOT FOUND em uma única chave smoke delimitada.");
            await output.WriteLineAsync("Nenhuma chamada externa executada. Use --execute para confirmar.");
            return 0;
        }

        if (!TryGetConfiguration(out var options, out var credentials)) return 2;
        var runId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..47];
        await using var store = _storeFactory(options!, credentials!);
        await output.WriteLineAsync(
            $"Config: endpointHost={options!.Endpoint.Host} | region={options.Region} | addressing={options.AddressingStyle}");
        var report = await new BucketSmokeRunner(store, _httpClient, options!)
            .RunAsync(runId, cancellationToken);

        await output.WriteLineAsync("Bucket smoke: PASSED");
        await output.WriteLineAsync($"Addressing validado: {report.AddressingStyle}");
        await output.WriteLineAsync("Bytes, SHA-256 e MIME: OK");
        foreach (var step in report.Steps)
        {
            await output.WriteLineAsync(
                $"{step.Operation}: OK | requestId={RedactRequestId(step.RequestId)}");
        }

        await output.WriteLineAsync("Cleanup delimitado: confirmado; objeto do smoke não existe mais.");
        return 0;
    }

    private bool TryGetConfiguration(out BucketOptions? options, out BucketCredentials? credentials)
    {
        options = null;
        credentials = null;
        var missing = settings.MissingBucketSecretNames();
        if (missing.Count > 0)
        {
            error.WriteLine("Bucket: BLOCKED. Configure os User Secrets abaixo:");
            foreach (var name in missing) error.WriteLine($"- {name}");
            return false;
        }

        try
        {
            options = settings.GetBucketOptions();
            credentials = settings.GetBucketCredentials();
            return true;
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine($"Bucket: BLOCKED. {exception.Message}");
            return false;
        }
    }

    private int Unknown(string command)
    {
        error.WriteLine($"Subcomando de bucket desconhecido: {command}");
        return 2;
    }

    internal static string RedactRequestId(string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) return "n/a";
        return requestId.Length <= 6 ? "[REDACTED]" : requestId[..6] + "…[REDACTED]";
    }

    internal static string DescribeFailure(Exception exception) => exception switch
    {
        BucketOperationException bucket =>
            $"operation={bucket.Operation} | " +
            $"status={(bucket.StatusCode is null ? "n/a" : ((int)bucket.StatusCode.Value).ToString())} | " +
            $"errorCode={bucket.ErrorCode ?? "n/a"} | " +
            $"requestId={RedactRequestId(bucket.RequestId)} | provider={bucket.SafeProviderMessage}",
        OperationCanceledException =>
            "operation=cancelled-or-timeout | status=n/a | errorCode=n/a | requestId=n/a | provider=omitted",
        HttpRequestException =>
            "operation=HTTP | status=n/a | errorCode=n/a | requestId=n/a | provider=URL omitted",
        InvalidDataException =>
            "operation=validation | status=n/a | errorCode=n/a | requestId=n/a | provider=omitted",
        _ =>
            "operation=unknown | status=n/a | errorCode=n/a | requestId=n/a | provider=omitted"
    };

    internal static string DescribeSmokeFailure(BucketSmokeException exception) =>
        $"Bucket smoke: FAILED | primary=({DescribeFailure(exception.PrimaryFailure)}) | " +
        $"cleanupStage={exception.CleanupFailure.Stage} | " +
        $"cleanup=({DescribeFailure(exception.CleanupFailure.InnerException!)}) | possibleOrphan=true";

    internal static string DescribeCleanupFailure(BucketCleanupException exception) =>
        $"Bucket smoke cleanup: FAILED | cleanupStage={exception.Stage} | " +
        $"cleanup=({DescribeFailure(exception.InnerException!)}) | possibleOrphan=true";
}
