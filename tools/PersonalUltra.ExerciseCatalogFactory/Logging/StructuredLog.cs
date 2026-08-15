using System.Text.Json;
using System.Text.RegularExpressions;

namespace PersonalUltra.ExerciseCatalogFactory.Logging;

public sealed partial class StructuredLog(string logsRoot)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _logsRoot = Path.GetFullPath(logsRoot);

    public async Task WriteAsync(
        string level,
        string eventName,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_logsRoot);
        var safeProperties = properties?.ToDictionary(
            pair => pair.Key,
            pair => RedactProperty(pair.Key, pair.Value),
            StringComparer.Ordinal) ?? new Dictionary<string, object?>();
        var entry = new
        {
            timestamp = DateTimeOffset.UtcNow,
            level,
            eventName,
            message = RedactText(message),
            properties = safeProperties
        };
        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        var path = Path.Combine(_logsRoot, $"factory-{DateTime.UtcNow:yyyyMMdd}.jsonl");
        await File.AppendAllTextAsync(path, line, cancellationToken);
    }

    public static string RedactText(string value)
    {
        var truncated = value.Length > 512 ? value[..512] + "…[TRUNCATED]" : value;
        return CredentialPattern().Replace(truncated, match => $"{match.Groups[1].Value}[REDACTED]");
    }

    private static object? RedactProperty(string key, object? value)
    {
        if (SensitiveKeyPattern().IsMatch(key)) return "[REDACTED]";
        return value is string text ? RedactText(text) : value;
    }

    [GeneratedRegex("(?i)(authorization|credential|password|secret|token|api[-_]?key|access[-_]?key)")]
    private static partial Regex SensitiveKeyPattern();

    [GeneratedRegex("(?i)(bearer\\s+|sk-|tsec_|tid_)[A-Za-z0-9._-]+")]
    private static partial Regex CredentialPattern();
}
