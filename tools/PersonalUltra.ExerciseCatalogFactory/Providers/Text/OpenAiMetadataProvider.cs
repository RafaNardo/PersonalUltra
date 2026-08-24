using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PersonalUltra.ExerciseCatalogFactory.Providers.Text;

public sealed class OpenAiMetadataProvider(HttpClient httpClient, string apiKey) : IMetadataProvider
{
    private const string Endpoint = "https://api.openai.com/v1/responses";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<MetadataProviderResult> GenerateAsync(
        MetadataRequest request,
        ProviderCallContext call,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        message.Headers.TryAddWithoutValidation("Idempotency-Key", call.IdempotencyKey);
        message.Content = JsonContent.Create(BuildPayload(request), options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new MetadataProviderException("Falha de rede ao consultar o provider de metadados.", retryable: true, exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var retryable = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.Conflict or
                    HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
                throw new MetadataProviderException(
                    $"Provider de metadados respondeu HTTP {(int)response.StatusCode}.", retryable);
            }

            OpenAiResponse body;
            try
            {
                body = await response.Content.ReadFromJsonAsync<OpenAiResponse>(JsonOptions, cancellationToken)
                    ?? throw new JsonException("Resposta vazia.");
            }
            catch (JsonException exception)
            {
                throw new MetadataProviderException("Provider retornou envelope JSON inválido.", retryable: false, exception);
            }

            if (body.Error is not null)
                throw ProviderStatusFailure("failed", body.Error.Code ?? body.Error.Type);
            if (body.Status == "incomplete")
                throw ProviderStatusFailure("incomplete", body.IncompleteDetails?.Reason);
            if (body.Status != "completed")
                throw new MetadataProviderException("Provider retornou status de resposta desconhecido.", retryable: false);

            var contents = body.Output?.SelectMany(output => output.Content ?? []).ToArray() ?? [];
            if (contents.Any(content => content.Type == "refusal"))
                throw new MetadataProviderException("Provider recusou o pedido de metadados.", retryable: false);

            var text = contents
                .FirstOrDefault(content => content.Type == "output_text")?.Text;
            if (text is null)
                throw new MetadataProviderException("Provider não retornou output_text estruturado.", retryable: false);

            MetadataProposal proposal;
            try
            {
                proposal = MetadataProposalValidator.ParseAndValidate(text);
            }
            catch (InvalidDataException exception)
            {
                throw new MetadataProviderException("Provider retornou metadados fora do contrato estrito.", retryable: true, exception);
            }

            return new MetadataProviderResult(
                proposal,
                SafeRequestId(body.Id ?? (response.Headers.TryGetValues("x-request-id", out var ids) ? ids.FirstOrDefault() : null)),
                body.Usage?.InputTokens,
                body.Usage?.OutputTokens,
                ObservedCostUsd: null);
        }
    }

    internal static object BuildPayload(MetadataRequest request) => new
    {
        model = request.Model,
        temperature = request.Temperature,
        input = new object[]
        {
            new
            {
                role = "system",
                content = request.PromptText
            },
            new
            {
                role = "user",
                content = JsonSerializer.Serialize(new
                {
                    request.ExternalKey,
                    request.Name,
                    request.Aliases,
                    request.PrimaryMuscleGroup,
                    request.Equipment,
                    request.InstructionsHint,
                    request.VisualHint,
                    lockedFields = request.LockedFields.Order(StringComparer.Ordinal),
                    allowedPrimaryMuscleGroups = MetadataTaxonomy.MuscleGroups,
                    allowedEquipment = MetadataTaxonomy.Equipment,
                    promptVersion = request.PromptVersion
                }, JsonOptions)
            }
        },
        text = new
        {
            format = new
            {
                type = "json_schema",
                name = "exercise_metadata_v1",
                strict = true,
                schema = MetadataSchemaCatalog.Load()
            }
        }
    };

    private static MetadataProviderException ProviderStatusFailure(string status, string? reason)
    {
        var retryable = reason is "max_output_tokens" or "rate_limit_exceeded" or "server_error" or
            "temporarily_unavailable" or "timeout";
        return new MetadataProviderException(
            $"Provider retornou resposta {status}; classificação={(retryable ? "retryable" : "terminal")}.", retryable);
    }

    private static string? SafeRequestId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tsec_", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tid_", StringComparison.OrdinalIgnoreCase)) return null;
        return value.Length <= 128 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            ? value
            : null;
    }
}

internal static class MetadataSchemaCatalog
{
    private static readonly IReadOnlySet<string> StructuralKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "type", "properties", "required", "additionalProperties", "items", "enum", "minLength", "maxLength",
        "minItems", "maxItems", "minimum", "maximum", "pattern", "const", "anyOf"
    };
    private static readonly IReadOnlySet<string> StrippedAnnotations = new HashSet<string>(StringComparer.Ordinal)
    {
        "$schema", "$id", "title", "description", "$comment", "examples", "default"
    };

    public static JsonElement Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Schemas", "v1", "metadata-proposal.schema.json");
        if (!File.Exists(path)) throw new InvalidDataException("JSON Schema de metadados v1 não encontrado.");
        var source = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidDataException("JSON Schema de metadados v1 deve ser um objeto.");
        var wire = CleanForWire(source);
        using var document = JsonDocument.Parse(wire.ToJsonString());
        return document.RootElement.Clone();
    }

    internal static JsonObject CleanForWire(JsonObject source) => CleanSchemaObject(source);

    private static JsonObject CleanSchemaObject(JsonObject source)
    {
        var result = new JsonObject();
        foreach (var property in source)
        {
            if (StrippedAnnotations.Contains(property.Key)) continue;
            if (!StructuralKeywords.Contains(property.Key))
                throw new InvalidDataException($"Keyword JSON Schema não suportada no wire contract: {property.Key}.");
            result[property.Key] = property.Key switch
            {
                "properties" => CleanProperties(property.Value as JsonObject
                    ?? throw new InvalidDataException("properties deve ser objeto.")),
                "items" when property.Value is JsonObject item => CleanSchemaObject(item),
                "anyOf" => CleanSchemaArray(property.Value as JsonArray
                    ?? throw new InvalidDataException($"{property.Key} deve ser array.")),
                _ => property.Value?.DeepClone()
            };
        }
        return result;
    }

    private static JsonObject CleanProperties(JsonObject properties)
    {
        var result = new JsonObject();
        foreach (var property in properties)
            result[property.Key] = CleanSchemaObject(property.Value as JsonObject
                ?? throw new InvalidDataException($"Schema da propriedade {property.Key} deve ser objeto."));
        return result;
    }

    private static JsonArray CleanSchemaArray(JsonArray schemas)
    {
        var result = new JsonArray();
        foreach (var schema in schemas)
            result.Add(CleanSchemaObject(schema as JsonObject
                ?? throw new InvalidDataException("Alternativa de schema deve ser objeto.")));
        return result;
    }
}
