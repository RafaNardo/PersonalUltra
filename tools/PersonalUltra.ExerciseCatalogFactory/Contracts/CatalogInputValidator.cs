using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalUltra.ExerciseCatalogFactory.Domain;

namespace PersonalUltra.ExerciseCatalogFactory.Contracts;

public static class CatalogInputValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<IReadOnlyList<ContractDiagnostic>> ValidateFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path);
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            return ValidateJson(Path.GetFileName(path), await File.ReadAllTextAsync(path, cancellationToken));
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return ValidateCsv(Path.GetFileName(path), await File.ReadAllTextAsync(path, cancellationToken));
        return [new(Path.GetFileName(path), null, "$", "Formato não suportado. Use .csv ou .json.")];
    }

    public static IReadOnlyList<ContractDiagnostic> ValidateJson(string file, string json)
    {
        CatalogInputDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<CatalogInputDocument>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            return [new(file, null, exception.Path ?? "$", "JSON inválido para o contrato de entrada.")];
        }

        if (document is null) return [new(file, null, "$", "Documento JSON vazio.")];
        var diagnostics = new List<ContractDiagnostic>();
        if (document.SchemaVersion != 1)
            diagnostics.Add(new(file, null, "schemaVersion", $"Schema desconhecido: {document.SchemaVersion}. Suportado: 1."));
        if (string.IsNullOrWhiteSpace(document.Source)) diagnostics.Add(new(file, null, "source", "Campo obrigatório."));
        if (document.Items is null || document.Items.Count == 0)
            diagnostics.Add(new(file, null, "items", "Ao menos um item é obrigatório."));
        else
            for (var index = 0; index < document.Items.Count; index++)
            {
                var item = document.Items[index];
                if (item is null)
                    diagnostics.Add(new(file, null, $"items[{index}]", "Item nulo não é permitido."));
                else
                    ValidateItem(file, index, item, diagnostics);
            }
        return diagnostics;
    }

    public static IReadOnlyList<ContractDiagnostic> ValidateCsv(string file, string csv)
    {
        var diagnostics = new List<ContractDiagnostic>();
        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
            return [new(file, 1, "$", "Cabeçalho CSV ausente.")];

        if (!TryParseCsvLine(lines[0], out var header, out var headerError))
            return [new(file, 1, "$", headerError!)];

        var normalized = header.Select(value => value.Trim().ToLowerInvariant()).ToArray();
        var nameIndex = Array.IndexOf(normalized, "name");
        if (nameIndex < 0) diagnostics.Add(new(file, 1, "name", "Coluna obrigatória ausente."));
        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
            diagnostics.Add(new(file, 1, "$", "Cabeçalho contém colunas duplicadas."));

        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex])) continue;
            if (!TryParseCsvLine(lines[lineIndex], out var values, out var error))
            {
                diagnostics.Add(new(file, lineIndex + 1, "$", error!));
                continue;
            }
            if (values.Count != header.Count)
            {
                diagnostics.Add(new(file, lineIndex + 1, "$", $"Esperadas {header.Count} colunas; recebidas {values.Count}."));
                continue;
            }
            if (nameIndex >= 0 && string.IsNullOrWhiteSpace(values[nameIndex]))
                diagnostics.Add(new(file, lineIndex + 1, "name", "Campo obrigatório."));
        }

        return diagnostics;
    }

    private static void ValidateItem(string file, int itemIndex, CatalogInputItem item, ICollection<ContractDiagnostic> diagnostics)
    {
        if (item.ExternalKey is not null && string.IsNullOrWhiteSpace(item.ExternalKey))
            diagnostics.Add(new(file, null, $"items[{itemIndex}].externalKey", "Quando presente, o campo não pode ficar vazio."));
        if (string.IsNullOrWhiteSpace(item.Name))
            diagnostics.Add(new(file, null, $"items[{itemIndex}].name", "Campo obrigatório."));
        ValidateStringArray(file, $"items[{itemIndex}].aliases", item.Aliases, diagnostics);
        ValidateStringArray(file, $"items[{itemIndex}].lockedFields", item.LockedFields, diagnostics);
    }

    private static void ValidateStringArray(
        string file,
        string field,
        IReadOnlyList<string>? values,
        ICollection<ContractDiagnostic> diagnostics)
    {
        if (values is null) return;
        if (values.Count == 0)
            diagnostics.Add(new(file, null, field, "Quando presente, a lista deve conter ao menos um valor."));
        if (values.Any(string.IsNullOrWhiteSpace))
            diagnostics.Add(new(file, null, field, "Valores vazios não são permitidos."));
        if (values.Where(value => value is not null).GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new(file, null, field, "Valores duplicados não são permitidos."));
    }

    private static bool TryParseCsvLine(string line, out IReadOnlyList<string> values, out string? error)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else current.Append(character);
        }

        if (quoted)
        {
            values = [];
            error = "Campo CSV com aspas não encerradas.";
            return false;
        }
        result.Add(current.ToString());
        values = result;
        error = null;
        return true;
    }
}
