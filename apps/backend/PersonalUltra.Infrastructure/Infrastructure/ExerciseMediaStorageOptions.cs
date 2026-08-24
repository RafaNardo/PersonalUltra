namespace PersonalUltra.Infrastructure;

public sealed class ExerciseMediaStorageOptions
{
    public const string SectionName = "RailwayBucket";

    public string EndpointUrl { get; set; } = string.Empty;
    public string Region { get; set; } = "auto";
    public bool ForcePathStyle { get; set; }
    public int SignedUrlLifetimeMinutes { get; set; } = 360;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
}
