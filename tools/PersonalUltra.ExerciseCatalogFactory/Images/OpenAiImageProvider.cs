using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalUltra.ExerciseCatalogFactory.Images;

internal sealed class OpenAiImageProvider(HttpClient httpClient, string apiKey) : IImageProvider
{
    public async Task<GeneratedImage> GenerateAsync(
        string model,
        string prompt,
        string size,
        string quality,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { model, prompt, size, quality, n = 1 });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            // The request may have reached the provider. Never retry an ambiguous transport failure automatically.
            throw new ImageProviderException("Falha de rede com resultado incerto; verifique a cobrança antes de tentar novamente.", false, exception);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                var retryable = status == 429 || status >= 500;
                throw new ImageProviderException($"OpenAI Image API recusou a geração (HTTP {status}).", retryable);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0 || !data[0].TryGetProperty("b64_json", out var encoded))
                throw new InvalidDataException("OpenAI Image API não retornou data[0].b64_json.");
            var base64 = encoded.GetString();
            if (string.IsNullOrWhiteSpace(base64))
                throw new InvalidDataException("OpenAI Image API retornou uma imagem vazia.");

            try
            {
                return new GeneratedImage(Convert.FromBase64String(base64),
                    response.Headers.TryGetValues("x-request-id", out var values) ? values.FirstOrDefault() : null);
            }
            catch (FormatException exception)
            {
                throw new InvalidDataException("OpenAI Image API retornou base64 inválido.", exception);
            }
        }
    }
}
