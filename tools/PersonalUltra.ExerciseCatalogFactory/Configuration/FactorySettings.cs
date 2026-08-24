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
        string metadataPromptVersion,
        decimal metadataTemperature,
        decimal metadataEstimatedCostUsd,
        int metadataMaxAttempts,
        string imageModel,
        string bucketEndpointUrl,
        string bucketRegion,
        bool bucketForcePathStyle,
        int signedUrlLifetimeMinutes,
        string? bucketName,
        string? bucketAccessKeyId,
        string? bucketSecretAccessKey,
        string? imageCatalogPath = null,
        string imageSize = "1024x1024",
        string imageQuality = "low",
        decimal imageEstimatedCostUsd = 0.02m,
        string imagePromptVersion = "personal-ultra-exercise-image-v2")
    {
        WorkspaceRoot = workspaceRoot;
        SchemaVersion = schemaVersion;
        MetadataModel = NullIfWhiteSpace(metadataModel);
        MetadataPromptVersion = metadataPromptVersion;
        MetadataTemperature = metadataTemperature;
        MetadataEstimatedCostUsd = metadataEstimatedCostUsd;
        MetadataMaxAttempts = metadataMaxAttempts;
        ImageModel = imageModel;
        ImageCatalogPath = ResolveRepositoryPath(imageCatalogPath ?? "tools/PersonalUltra.ExerciseCatalogFactory/Inputs/v1/exercise-inventory-v1.csv");
        ImageSize = imageSize;
        ImageQuality = imageQuality;
        ImageEstimatedCostUsd = imageEstimatedCostUsd;
        ImagePromptVersion = imagePromptVersion;
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
    public string MetadataPromptVersion { get; }
    public decimal MetadataTemperature { get; }
    public decimal MetadataEstimatedCostUsd { get; }
    public int MetadataMaxAttempts { get; }
    public string ImageModel { get; }
    public string ImageCatalogPath { get; }
    public string ImageSize { get; }
    public string ImageQuality { get; }
    public decimal ImageEstimatedCostUsd { get; }
    public string ImagePromptVersion { get; }
    public string BucketEndpointUrl { get; }
    public string BucketRegion { get; }
    public bool BucketForcePathStyle { get; }
    public int SignedUrlLifetimeMinutes { get; }

    internal OpenAiCredentials GetOpenAiCredentials()
    {
        if (_openAiApiKey is null) throw new InvalidOperationException("OpenAI não configurada. Secret ausente: ai-api-key.");
        return new OpenAiCredentials(_openAiApiKey);
    }

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
            metadataPromptVersion: configuration["OpenAI:MetadataPromptVersion"] ?? "exercise-metadata-v1",
            metadataTemperature: configuration.GetValue("OpenAI:MetadataTemperature", 0m),
            metadataEstimatedCostUsd: configuration.GetValue("OpenAI:MetadataEstimatedCostUsd", 0.01m),
            metadataMaxAttempts: configuration.GetValue("OpenAI:MetadataMaxAttempts", 3),
            imageModel: configuration["OpenAI:ImageModel"] ?? "gpt-image-2",
            bucketEndpointUrl: configuration["RailwayBucket:EndpointUrl"] ?? string.Empty,
            bucketRegion: configuration["RailwayBucket:Region"] ?? "auto",
            bucketForcePathStyle: configuration.GetValue("RailwayBucket:ForcePathStyle", false),
            signedUrlLifetimeMinutes: configuration.GetValue("RailwayBucket:SignedUrlLifetimeMinutes", 360),
            bucketName: configuration["RailwayBucket:BucketName"],
            bucketAccessKeyId: configuration["RailwayBucket:AccessKeyId"],
            bucketSecretAccessKey: configuration["RailwayBucket:SecretAccessKey"],
            imageCatalogPath: configuration["Images:CatalogPath"],
            imageSize: configuration["OpenAI:ImageSize"] ?? "1024x1024",
            imageQuality: configuration["OpenAI:ImageQuality"] ?? "low",
            imageEstimatedCostUsd: configuration.GetValue("OpenAI:ImageEstimatedCostUsd", 0.02m),
            imagePromptVersion: configuration["OpenAI:ImagePromptVersion"] ?? "personal-ultra-exercise-image-v2");
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

    internal FactorySettings ForImageBatch(string? batch)
    {
        if (string.IsNullOrWhiteSpace(batch) || batch == "catalog-v2") return this;
        if (batch != "legacy-v3")
            throw new ArgumentException("Batch de imagens desconhecido. Use catalog-v2 ou legacy-v3.", nameof(batch));

        return new FactorySettings(
            WorkspaceRoot, SchemaVersion, _openAiApiKey, MetadataModel, MetadataPromptVersion,
            MetadataTemperature, MetadataEstimatedCostUsd, MetadataMaxAttempts, ImageModel,
            BucketEndpointUrl, BucketRegion, BucketForcePathStyle, SignedUrlLifetimeMinutes,
            _bucketName, _bucketAccessKeyId, _bucketSecretAccessKey,
            ResolveRepositoryPath("tools/PersonalUltra.ExerciseCatalogFactory/Inputs/v3/legacy-exercise-images-v3.csv"),
            ImageSize, ImageQuality, ImageEstimatedCostUsd, "personal-ultra-exercise-image-v3");
    }

    public IReadOnlyList<string> ValidateLocalConfiguration()
    {
        var errors = new List<string>();
        if (SchemaVersion != "1") errors.Add($"Factory:SchemaVersion não suportado: {SchemaVersion}");
        if (MetadataModel is null) errors.Add("OpenAI:MetadataModel precisa ser configurado explicitamente.");
        if (string.IsNullOrWhiteSpace(ImageModel)) errors.Add("OpenAI:ImageModel não pode ficar vazio.");
        if (!File.Exists(ImageCatalogPath)) errors.Add($"Catálogo de imagens não encontrado: {ImageCatalogPath}");
        if (ImageSize != "1024x1024") errors.Add("OpenAI:ImageSize deve ser 1024x1024 neste piloto.");
        if (ImageQuality is not ("low" or "medium" or "high")) errors.Add("OpenAI:ImageQuality deve ser low, medium ou high.");
        if (ImageEstimatedCostUsd <= 0) errors.Add("OpenAI:ImageEstimatedCostUsd deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(ImagePromptVersion)) errors.Add("OpenAI:ImagePromptVersion não pode ficar vazio.");
        if (string.IsNullOrWhiteSpace(MetadataPromptVersion)) errors.Add("OpenAI:MetadataPromptVersion não pode ficar vazio.");
        if (MetadataTemperature is < 0 or > 2) errors.Add("OpenAI:MetadataTemperature deve ficar entre 0 e 2.");
        if (MetadataEstimatedCostUsd <= 0) errors.Add("OpenAI:MetadataEstimatedCostUsd deve ser maior que zero.");
        if (MetadataMaxAttempts is < 1 or > 5) errors.Add("OpenAI:MetadataMaxAttempts deve ficar entre 1 e 5.");
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

    private static string ResolveRepositoryPath(string configuredPath)
    {
        if (Path.IsPathFullyQualified(configuredPath)) return Path.GetFullPath(configuredPath);
        var root = FindRepositoryRoot(Directory.GetCurrentDirectory()) ?? FindRepositoryRoot(AppContext.BaseDirectory);
        return root is null ? Path.GetFullPath(configuredPath) : Path.GetFullPath(Path.Combine(root, configuredPath));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}

internal sealed class OpenAiCredentials(string apiKey)
{
    public string ApiKey { get; } = apiKey;
}
