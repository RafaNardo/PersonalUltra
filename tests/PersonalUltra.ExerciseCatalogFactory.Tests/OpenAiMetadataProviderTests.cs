using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PersonalUltra.ExerciseCatalogFactory.Providers.Text;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class OpenAiMetadataProviderTests
{
    [Fact]
    public async Task Adapter_sends_bearer_outside_payload_and_parses_structured_output()
    {
        string? requestBody = null;
        string? authorization = null;
        string? idempotencyKey = null;
        var handler = new StubHandler(async request =>
        {
            authorization = request.Headers.Authorization?.ToString();
            idempotencyKey = Assert.Single(request.Headers.GetValues("Idempotency-Key"));
            requestBody = await request.Content!.ReadAsStringAsync();
            var proposal = """{"canonicalName":"Supino","aliases":[],"primaryMuscleGroup":"Peito","equipment":"Barra","instructions":"Controle o movimento.","visualDescription":"Pessoa usando barra em banco.","ambiguities":[],"confidence":{"primaryMuscleGroup":"high","equipment":"high"}}""";
            var envelope = System.Text.Json.JsonSerializer.Serialize(new
            {
                id = "resp_safe",
                status = "completed",
                output = new[] { new { content = new[] { new { type = "output_text", text = proposal } } } },
                usage = new { input_tokens = 12, output_tokens = 34 }
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(envelope, Encoding.UTF8, "application/json") };
        });
        var provider = new OpenAiMetadataProvider(new HttpClient(handler), "super-secret-test-key");

        var result = await provider.GenerateAsync(Request(), new ProviderCallContext("idem-test", 1), CancellationToken.None);

        Assert.Equal("Bearer super-secret-test-key", authorization);
        Assert.Equal("idem-test", idempotencyKey);
        Assert.DoesNotContain("super-secret-test-key", requestBody);
        Assert.Equal("Supino", result.Proposal.CanonicalName);
        Assert.Equal("resp_safe", result.RequestId);
        Assert.Equal(12, result.InputTokens);
        Assert.Equal(34, result.OutputTokens);
    }

    [Fact]
    public async Task Adapter_classifies_rate_limit_as_retryable_without_exposing_response_body()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":{\"message\":\"secret-provider-detail\"}}")
        }));
        var provider = new OpenAiMetadataProvider(new HttpClient(handler), "secret-key");

        var exception = await Assert.ThrowsAsync<MetadataProviderException>(() =>
            provider.GenerateAsync(Request(), new ProviderCallContext("idem-test", 1), CancellationToken.None));

        Assert.True(exception.Retryable);
        Assert.DoesNotContain("secret-provider-detail", exception.Message);
        Assert.Contains("HTTP 429", exception.Message);
    }

    [Theory]
    [InlineData("incomplete", "max_output_tokens", true)]
    [InlineData("incomplete", "content_filter", false)]
    [InlineData("failed", "rate_limit_exceeded", true)]
    [InlineData("failed", "invalid_request_error", false)]
    public async Task Adapter_classifies_response_lifecycle_without_persisting_provider_detail(
        string status, string reason, bool retryable)
    {
        object envelope = status == "incomplete"
            ? new { id = "resp", status, incomplete_details = new { reason }, output = Array.Empty<object>() }
            : new { id = "resp", status, error = new { code = reason, type = reason, message = "provider-secret-detail" }, output = Array.Empty<object>() };
        var handler = JsonHandler(envelope);
        var provider = new OpenAiMetadataProvider(new HttpClient(handler), "secret-key");

        var exception = await Assert.ThrowsAsync<MetadataProviderException>(() =>
            provider.GenerateAsync(Request(), new ProviderCallContext("idem-test", 1), CancellationToken.None));

        Assert.Equal(retryable, exception.Retryable);
        Assert.DoesNotContain(reason, exception.Message);
        Assert.DoesNotContain("provider-secret-detail", exception.Message);
    }

    [Fact]
    public async Task Adapter_treats_refusal_as_terminal_without_exposing_refusal_text()
    {
        var handler = JsonHandler(new
        {
            id = "resp",
            status = "completed",
            output = new[] { new { content = new[] { new { type = "refusal", refusal = "sensitive refusal detail" } } } }
        });
        var provider = new OpenAiMetadataProvider(new HttpClient(handler), "secret-key");

        var exception = await Assert.ThrowsAsync<MetadataProviderException>(() =>
            provider.GenerateAsync(Request(), new ProviderCallContext("idem-test", 1), CancellationToken.None));

        Assert.False(exception.Retryable);
        Assert.DoesNotContain("sensitive refusal detail", exception.Message);
    }

    [Fact]
    public void Payload_schema_is_loaded_from_versioned_file_as_supported_strict_wire_subset()
    {
        var payload = JsonNode.Parse(JsonSerializer.Serialize(OpenAiMetadataProvider.BuildPayload(Request())))!;
        var actualSchema = payload["text"]!["format"]!["schema"];

        Assert.Null(actualSchema!["$schema"]);
        Assert.Null(actualSchema["$id"]);
        Assert.Null(actualSchema["title"]);
        Assert.False(actualSchema["additionalProperties"]!.GetValue<bool>());
        Assert.NotNull(actualSchema["properties"]!["confidence"]!["additionalProperties"]);
        Assert.Contains("canonicalName", actualSchema["required"]!.AsArray().Select(value => value!.GetValue<string>()));
    }

    [Theory]
    [InlineData("oneOf")]
    [InlineData("allOf")]
    public void Wire_schema_cleaner_rejects_unsupported_composition_keywords(string keyword)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            [keyword] = new JsonArray(new JsonObject { ["type"] = "string" })
        };

        var exception = Assert.Throws<InvalidDataException>(() => MetadataSchemaCatalog.CleanForWire(schema));

        Assert.Contains(keyword, exception.Message);
    }

    private static MetadataRequest Request() => new("bench", "Supino", [], null, null, null, null,
        new HashSet<string>(), "gpt-test", "exercise-metadata-v1", "Use strict output.", 0);

    private static StubHandler JsonHandler(object body) => new(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
    }));

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
