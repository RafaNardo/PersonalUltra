using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalUltra.ExerciseCatalogFactory.Contracts;
using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Intake;

public static class CatalogInputReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<IReadOnlyList<CatalogInputRow>> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = await CatalogInputValidator.ValidateFileAsync(path, cancellationToken);
        if (diagnostics.Count > 0)
            throw new InvalidDataException(string.Join(Environment.NewLine, diagnostics.Select(FormatDiagnostic)));

        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? ReadJson(text)
            : ReadCsv(text);
    }

    private static IReadOnlyList<CatalogInputRow> ReadJson(string json)
    {
        var document = JsonSerializer.Deserialize<CatalogInputDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("Documento JSON vazio.");
        return document.Items.Select((item, index) => new CatalogInputRow(item!, index + 1)).ToArray();
    }

    private static IReadOnlyList<CatalogInputRow> ReadCsv(string csv)
    {
        var records = ParseCsv(csv);
        var header = records[0].Values.Select(value => value.Trim().ToLowerInvariant()).ToArray();
        var result = new List<CatalogInputRow>();
        foreach (var record in records.Skip(1))
        {
            if (record.Values.All(string.IsNullOrWhiteSpace)) continue;
            string? Value(string name)
            {
                var index = Array.IndexOf(header, name);
                return index < 0 || string.IsNullOrWhiteSpace(record.Values[index]) ? null : record.Values[index].Trim();
            }

            var aliases = SplitList(Value("aliases"));
            var lockedFields = SplitList(Value("locked_fields"));
            result.Add(new CatalogInputRow(new CatalogInputItem(
                Value("external_key"), Value("name")!, aliases, Value("primary_muscle_group"),
                Value("equipment"), Value("notes") ?? Value("instructions_hint"), Value("visual_hint"), lockedFields),
                record.Line));
        }
        return result;
    }

    private static IReadOnlyList<string>? SplitList(string? value) => value is null
        ? null
        : value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static IReadOnlyList<CsvRecord> ParseCsv(string csv)
    {
        var records = new List<CsvRecord>();
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        var line = 1;
        var recordLine = 1;
        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (character == '"')
            {
                if (quoted && index + 1 < csv.Length && csv[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else if ((character == '\r' || character == '\n') && !quoted)
            {
                if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n') index++;
                values.Add(value.ToString());
                value.Clear();
                records.Add(new CsvRecord(recordLine, values.ToArray()));
                values.Clear();
                line++;
                recordLine = line;
            }
            else
            {
                value.Append(character);
                if (character == '\n') line++;
            }
        }

        if (value.Length > 0 || values.Count > 0)
        {
            values.Add(value.ToString());
            records.Add(new CsvRecord(recordLine, values.ToArray()));
        }
        return records;
    }

    private static string FormatDiagnostic(ContractDiagnostic diagnostic) =>
        $"{diagnostic.File}{(diagnostic.Line is null ? string.Empty : $":{diagnostic.Line}")} [{diagnostic.Field}]: {diagnostic.Message}";

    private sealed record CsvRecord(int Line, IReadOnlyList<string> Values);
}
