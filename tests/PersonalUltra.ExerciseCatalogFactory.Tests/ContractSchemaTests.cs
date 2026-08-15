using System.Text.Json;
using PersonalUltra.ExerciseCatalogFactory.Contracts;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ContractSchemaTests
{
    [Theory]
    [InlineData("catalog-input.schema.json")]
    [InlineData("run-manifest.schema.json")]
    [InlineData("review-decision.schema.json")]
    [InlineData("output-package.schema.json")]
    public void Versioned_schema_is_valid_json_with_stable_id(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Schemas", "v1", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", document.RootElement.GetProperty("$schema").GetString());
        Assert.Contains("/v1/", document.RootElement.GetProperty("$id").GetString());
    }

    [Fact]
    public void Json_contract_rejects_unknown_schema_and_reports_field()
    {
        var diagnostics = CatalogInputValidator.ValidateJson("batch.json", """
            { "schemaVersion": 99, "source": "batch", "items": [{ "name": "Supino" }] }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("batch.json", diagnostic.File);
        Assert.Equal("schemaVersion", diagnostic.Field);
        Assert.Contains("Schema desconhecido", diagnostic.Message);
    }

    [Fact]
    public void Csv_contract_reports_file_line_and_required_name()
    {
        var diagnostics = CatalogInputValidator.ValidateCsv("batch.csv", "external_key,name,equipment\na,,Barra\n");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("batch.csv", diagnostic.File);
        Assert.Equal(2, diagnostic.Line);
        Assert.Equal("name", diagnostic.Field);
    }

    [Fact]
    public void Csv_contract_accepts_quoted_commas()
    {
        var diagnostics = CatalogInputValidator.ValidateCsv("batch.csv", "name,notes\nSupino,\"Banco, barra e anilhas\"\n");

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("{ \"schemaVersion\": 1, \"source\": \"x\", \"items\": [null] }", "items[0]")]
    [InlineData("{ \"schemaVersion\": 1, \"source\": \"x\", \"items\": [{ \"name\": \"A\", \"externalKey\": \"\" }] }", "externalKey")]
    [InlineData("{ \"schemaVersion\": 1, \"source\": \"x\", \"items\": [{ \"name\": \"A\", \"aliases\": [] }] }", "aliases")]
    [InlineData("{ \"schemaVersion\": 1, \"source\": \"x\", \"items\": [{ \"name\": \"A\", \"aliases\": [\"B\", \"B\"] }] }", "aliases")]
    [InlineData("{ \"schemaVersion\": 1, \"source\": \"x\", \"items\": [{ \"name\": \"A\", \"lockedFields\": [\"\"] }] }", "lockedFields")]
    [InlineData("{ \"schemaVersion\": 1, \"source\": \"x\", \"items\": [{ \"name\": \"A\", \"unknown\": true }] }", "unknown")]
    public void Json_contract_rejects_values_forbidden_by_v1_schema(string json, string expectedField)
    {
        var diagnostics = CatalogInputValidator.ValidateJson("negative.json", json);

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Field.Contains(expectedField, StringComparison.Ordinal));
    }

    [Fact]
    public void Output_schema_requires_full_artifact_contract()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Schemas", "v1", "output-package.schema.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var artifacts = document.RootElement.GetProperty("properties").GetProperty("artifacts");
        var artifactDefinition = document.RootElement.GetProperty("$defs").GetProperty("artifact");

        Assert.Equal("#/$defs/artifact", artifacts.GetProperty("items").GetProperty("$ref").GetString());
        Assert.Equal(["stage", "relativePath", "sha256", "length"],
            artifactDefinition.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        Assert.False(artifactDefinition.GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void Json_multiline_diagnostic_uses_json_path_without_claiming_a_physical_line()
    {
        var diagnostics = CatalogInputValidator.ValidateJson("multiline.json", """
            {
              "schemaVersion": 1,
              "source": "batch",
              "items": [
                { "name": "" }
              ]
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Null(diagnostic.Line);
        Assert.Equal("items[0].name", diagnostic.Field);
    }

    [Fact]
    public void Optional_arrays_have_same_non_empty_rule_in_schema_and_runtime_validator()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Schemas", "v1", "catalog-input.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllText(path));
        var itemProperties = schema.RootElement.GetProperty("properties").GetProperty("items")
            .GetProperty("items").GetProperty("properties");

        Assert.Equal(1, itemProperties.GetProperty("aliases").GetProperty("minItems").GetInt32());
        Assert.Equal(1, itemProperties.GetProperty("lockedFields").GetProperty("minItems").GetInt32());

        var diagnostics = CatalogInputValidator.ValidateJson("arrays.json", """
            { "schemaVersion": 1, "source": "batch", "items": [{ "name": "A", "aliases": [], "lockedFields": [] }] }
            """);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Field == "items[0].aliases" && diagnostic.Line is null);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Field == "items[0].lockedFields" && diagnostic.Line is null);
    }
}
