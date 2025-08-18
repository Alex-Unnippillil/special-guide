using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace SpecialGuide.Core.Services;

public record OpenAIError(HttpStatusCode? StatusCode, string Message);

public record SuggestionResult(string[] Suggestions, OpenAIError? Error);

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient httpClient, SettingsService settings, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public virtual async Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName, CancellationToken cancellationToken = default)
    {
        var base64 = Convert.ToBase64String(image);
        var imageUrl = "data:image/png;base64," + base64;
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = $"Return 6 short, actionable next-step prompts tailored to {appName}." },
                new { role = "user", content = new object[]{ new { type="image_url", image_url = new { url = imageUrl } } } }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "suggestions",
                    schema = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    }
                }
            }
        };
        var (response, error) = await SendWithRetryAsync(() => CreateChatRequest(payload), cancellationToken);
        if (error != null || response == null)
        {
            return new SuggestionResult(Array.Empty<string>(), error);
        }

        using (response)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(content))
                    return new SuggestionResult(Array.Empty<string>(), null);
                var suggestions = JsonSerializer.Deserialize<string[]>(content!);
                return new SuggestionResult(suggestions ?? Array.Empty<string>(), null);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize suggestions");
                return new SuggestionResult(Array.Empty<string>(), new OpenAIError(null, "Malformed response from OpenAI"));
            }
        }
    }

    public virtual async Task<string> TranscribeAsync(byte[] wav, CancellationToken cancellationToken = default)
    {
        var (response, error) = await SendWithRetryAsync(() => CreateTranscribeRequest(wav), cancellationToken);
        if (error != null || response == null)
            throw new HttpRequestException(error?.Message, null, error?.StatusCode);

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
        }
    }

    public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };
        var (response, error) = await SendWithRetryAsync(() => CreateChatRequest(payload), cancellationToken);
        if (error != null || response == null)
            throw new HttpRequestException(error?.Message, null, error?.StatusCode);

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }
    }

    private HttpRequestMessage CreateChatRequest(object payload)
    {
        var apiKey = _settings.ApiKey;
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private HttpRequestMessage CreateTranscribeRequest(byte[] wav)
    {
        var apiKey = _settings.ApiKey;
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(wav), "file", "audio.wav");
        content.Add(new StringContent("whisper-1"), "model");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = content;
        return request;
    }

    private async Task<(HttpResponseMessage? Response, OpenAIError? Error)> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var request = requestFactory();
                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return (response, null);
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                    (int)response.StatusCode >= 500)
                {
                    response.Dispose();
                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                        continue;
                    }
                }

                var body = await response.Content.ReadAsStringAsync();
                response.Dispose();
                var error = new OpenAIError(response.StatusCode, $"OpenAI request failed with status code {response.StatusCode}: {body}");
                _logger.LogError(error.Message);
                return (null, error);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                    continue;
                }
                _logger.LogError(ex, "OpenAI API call failed");
                return (null, new OpenAIError(null, ex.Message));
            }
        }

        return (null, new OpenAIError(null, cancellationToken.IsCancellationRequested ? "Canceled" : "Unknown error"));
    }
}
