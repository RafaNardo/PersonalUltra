using Microsoft.Extensions.Configuration;

namespace PersonalUltra.ExerciseCatalogFactory.Configuration;

public sealed class FactorySettings
{
    private readonly string? _openAiApiKey;
    private readonly string? _bucketName;
    private readonly string? _bucketAccessKeyId;
    private readonly string? _bucketSecretAccessKey;

    public FactorySettings(
        string workspaceRoot,
        string schemaVersion,
        string? openAiApiKey,
        string? metadataModel,
        string imageModel,
        string bucketEndpointUrl,
        string bucketRegion,
        bool bucketForcePathStyle,
        int signedUrlLifetimeMinutes,
        string? bucketName,
        string? bucketAccessKeyId,
        string? bucketSecretAccessKey)
    {
        WorkspaceRoot = workspaceRoot;
        SchemaVersion = schemaVersion;
        MetadataModel = NullIfWhiteSpace(metadataModel);
        ImageModel = imageModel;
        BucketEndpointUrl = bucketEndpointUrl;
        BucketRegion = bucketRegion;
        BucketForcePathStyle = bucketForcePathStyle;
        SignedUrlLifetimeMinutes = signedUrlLifetimeMinutes;
        _openAiApiKey = NullIfWhiteSpace(openAiApiKey);
        _bucketName = NullIfWhiteSpace(bucketName);
        _bucketAccessKeyId = NullIfWhiteSpace(bucketAccessKeyId);
        _bucketSecretAccessKey = NullIfWhiteSpace(bucketSecretAccessKey);
    }

    public string WorkspaceRoot { get; }
    public string SchemaVersion { get; }
    public string? MetadataModel { get; }
    public string ImageModel { get; }
    public string BucketEndpointUrl { get; }
    public string BucketRegion { get; }
    public bool BucketForcePathStyle { get; }
    public int SignedUrlLifetimeMinutes { get; }

    internal BucketOptions GetBucketOptions() => new(
        BucketEndpointUrl,
        BucketRegion,
        BucketForcePathStyle,
        TimeSpan.FromMinutes(SignedUrlLifetimeMinutes));

    internal BucketCredentials GetBucketCredentials()
    {
        var missing = MissingBucketSecretNames();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Bucket não configurado. Secrets ausentes: {string.Join(", ", missing)}.");
        }

        return new BucketCredentials(_bucketName!, _bucketAccessKeyId!, _bucketSecretAccessKey!);
    }

    public static FactorySettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets(typeof(FactorySettings).Assembly, optional: true)
            .AddEnvironmentVariables(prefix: "PERSONAL_ULTRA_FACTORY_")
            .Build();

        var configuredWorkspace = configuration["Factory:WorkspaceRoot"]
            ?? "tools/PersonalUltra.ExerciseCatalogFactory/workspace";

        return new FactorySettings(
            workspaceRoot: ResolveWorkspaceRoot(configuredWorkspace),
            schemaVersion: configuration["Factory:SchemaVersion"] ?? "1",
            openAiApiKey: configuration["ai-api-key"],
            metadataModel: configuration["OpenAI:MetadataModel"],
            imageModel: configuration["OpenAI:ImageModel"] ?? "gpt-image-2",
            bucketEndpointUrl: configuration["RailwayBucket:EndpointUrl"] ?? string.Empty,
            bucketRegion: configuration["RailwayBucket:Region"] ?? "auto",
            bucketForcePathStyle: configuration.GetValue("RailwayBucket:ForcePathStyle", false),
            signedUrlLifetimeMinutes: configuration.GetValue("RailwayBucket:SignedUrlLifetimeMinutes", 360),
            bucketName: configuration["RailwayBucket:BucketName"],
            bucketAccessKeyId: configuration["RailwayBucket:AccessKeyId"],
            bucketSecretAccessKey: configuration["RailwayBucket:SecretAccessKey"]);
    }

    public static string ResolveWorkspaceRoot(
        string configuredPath,
        string? currentDirectory = null,
        string? applicationDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("Factory:WorkspaceRoot não pode ficar vazio.");
        }

        if (Path.IsPathFullyQualified(configuredPath)) return Path.GetFullPath(configuredPath);

        var repositoryRoot = FindRepositoryRoot(currentDirectory ?? Directory.GetCurrentDirectory())
            ?? FindRepositoryRoot(applicationDirectory ?? AppContext.BaseDirectory);
        if (repositoryRoot is null)
        {
            throw new InvalidOperationException(
                "Não foi possível resolver o workspace relativo: PersonalUltra.sln não foi encontrado.");
        }

        return Path.GetFullPath(Path.Combine(repositoryRoot, configuredPath));
    }

    public IReadOnlyList<string> MissingSecretNames()
    {
        var missing = new List<string>();
        if (_openAiApiKey is null) missing.Add("ai-api-key");
        if (_bucketName is null) missing.Add("RailwayBucket:BucketName");
        if (_bucketAccessKeyId is null) missing.Add("RailwayBucket:AccessKeyId");
        if (_bucketSecretAccessKey is null) missing.Add("RailwayBucket:SecretAccessKey");
        return missing;
    }

    public IReadOnlyList<string> MissingBucketSecretNames()
    {
        var missing = new List<string>();
        if (_bucketName is null) missing.Add("RailwayBucket:BucketName");
        if (_bucketAccessKeyId is null) missing.Add("RailwayBucket:AccessKeyId");
        if (_bucketSecretAccessKey is null) missing.Add("RailwayBucket:SecretAccessKey");
        return missing;
    }

    public IReadOnlyList<string> ValidateLocalConfiguration()
    {
        var errors = new List<string>();
        if (SchemaVersion != "1") errors.Add($"Factory:SchemaVersion não suportado: {SchemaVersion}");
        if (string.IsNullOrWhiteSpace(ImageModel)) errors.Add("OpenAI:ImageModel não pode ficar vazio.");
        try
        {
            _ = GetBucketOptions();
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        return errors;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PersonalUltra.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        return null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
