using System.Text.RegularExpressions;

namespace PersonalUltra.Application.Training;

public readonly partial record struct ExerciseMediaReference
{
    private const string SchemePrefix = "media://";

    private ExerciseMediaReference(string value, string objectKey)
    {
        Value = value;
        ObjectKey = objectKey;
    }

    public string Value { get; }
    public string ObjectKey { get; }

    public static bool IsMediaReference(string? value) =>
        value?.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase) == true;

    public static ExerciseMediaReference Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("media", StringComparison.Ordinal) ||
            !uri.Host.Equals("exercise-catalog", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !SupportedPath().IsMatch(uri.AbsolutePath))
        {
            throw new FormatException(
                "Exercise ImageRef must match media://exercise-catalog/v2/<slug>.png.");
        }

        return new ExerciseMediaReference(value, $"exercise-catalog{uri.AbsolutePath}");
    }

    [GeneratedRegex("^/v2/[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.png$", RegexOptions.CultureInvariant)]
    private static partial Regex SupportedPath();
}
