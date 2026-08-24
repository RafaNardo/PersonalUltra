using System.Net;
using System.Text;
using System.Text.Json;
using PersonalUltra.ExerciseCatalogFactory.Images;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class OpenAiImageProviderTests
{
    [Fact]
    public async Task Generate_uses_official_image_endpoint_and_decodes_base64()
    {
        var bytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1 };
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { data = new[] { new { b64_json = Convert.ToBase64String(bytes) } } }), Encoding.UTF8, "application/json")
        });
        handler.Response.Headers.Add("x-request-id", "req_test");
        var provider = new OpenAiImageProvider(new HttpClient(handler), "private-test-key");

        var result = await provider.GenerateAsync("gpt-image-2", "prompt", "1024x1024", "low", default);

        Assert.Equal(bytes, result.Bytes);
        Assert.Equal("req_test", result.RequestId);
        Assert.Equal("https://api.openai.com/v1/images/generations", handler.Request!.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("private-test-key", handler.Request.Headers.Authorization.Parameter);
        var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("gpt-image-2", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("1024x1024", body.RootElement.GetProperty("size").GetString());
        Assert.Equal("low", body.RootElement.GetProperty("quality").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("n").GetInt32());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task Generate_classifies_only_safe_http_failures_for_retry(HttpStatusCode status, bool retryable)
    {
        var provider = new OpenAiImageProvider(
            new HttpClient(new CaptureHandler(new HttpResponseMessage(status))), "private-test-key");

        var exception = await Assert.ThrowsAsync<ImageProviderException>(() =>
            provider.GenerateAsync("gpt-image-2", "prompt", "1024x1024", "low", default));

        Assert.Equal(retryable, exception.Retryable);
    }

    private sealed class CaptureHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        internal HttpRequestMessage? Request { get; private set; }
        internal string? Body { get; private set; }
        internal HttpResponseMessage Response { get; } = response;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Response;
        }
    }
}
