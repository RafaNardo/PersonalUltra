using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SvrMethod.Api.Application.Coach;
using Xunit;

namespace SvrMethod.Api.IntegrationTests;

public sealed class OpenAiCoachResponderTests
{
    private static readonly CoachContext Context = new("Rafael", "Superior 1", 4, false);

    [Fact]
    public async Task OpenAi_responder_calls_responses_endpoint_and_returns_text_reply()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"output_text\":\"Vamos manter a consistência esta semana.\"}", Encoding.UTF8, "application/json"),
        });
        using var client = new HttpClient(handler);
        var responder = new OpenAiCoachResponder(client, Options.Create(new OpenAiCoachOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            Endpoint = "https://unit.test/v1/responses",
        }));

        var reply = await responder.ReplyAsync("Como estou indo?", Context, CancellationToken.None);

        Assert.Equal("Text", reply.Kind);
        Assert.Equal("OPENAI_COACH_RESPONSE", reply.ReasonCode);
        Assert.Equal("Vamos manter a consistência esta semana.", reply.Content);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("test-key", handler.AuthorizationParameter);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("Como estou indo?", body.RootElement.GetProperty("input").GetString());
        Assert.Contains("não toma decisões", body.RootElement.GetProperty("instructions").GetString());
        Assert.DoesNotContain("Rafael", handler.Body!);
    }

    [Fact]
    public async Task Resilient_responder_uses_deterministic_reply_when_key_is_missing_without_network_call()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("Provider should not be called."));
        using var client = new HttpClient(handler);
        var options = Options.Create(new OpenAiCoachOptions());
        var responder = CreateResilient(client, options);

        var reply = await responder.ReplyAsync("Quero trocar um exercício", Context, CancellationToken.None);

        Assert.Equal("Choice", reply.Kind);
        Assert.Equal("EXERCISE_SELECTION_REQUIRED", reply.ReasonCode);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task Resilient_responder_preserves_safety_and_material_change_paths_when_openai_is_configured()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("Provider should not be called."));
        using var client = new HttpClient(handler);
        var options = Options.Create(new OpenAiCoachOptions { ApiKey = "test-key" });
        var responder = CreateResilient(client, options);

        var reply = await responder.ReplyAsync("Estou com dor no joelho", Context, CancellationToken.None);

        Assert.Equal("Choice", reply.Kind);
        Assert.Equal("PAIN_TRIAGE_REQUIRED", reply.ReasonCode);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task Resilient_responder_falls_back_when_openai_is_unavailable()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var client = new HttpClient(handler);
        var options = Options.Create(new OpenAiCoachOptions { ApiKey = "test-key", Endpoint = "https://unit.test/v1/responses" });
        var responder = CreateResilient(client, options);

        var reply = await responder.ReplyAsync("Como está minha consistência?", Context, CancellationToken.None);

        Assert.True(handler.WasCalled);
        Assert.Equal("DETERMINISTIC_DEMO_COACH", reply.ReasonCode);
    }

    private static ResilientCoachResponder CreateResilient(HttpClient client, IOptions<OpenAiCoachOptions> options) => new(
        new OpenAiCoachResponder(client, options),
        new DeterministicCoachResponder(),
        options,
        NullLogger<ResilientCoachResponder>.Instance);

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return response(request);
        }
    }
}
