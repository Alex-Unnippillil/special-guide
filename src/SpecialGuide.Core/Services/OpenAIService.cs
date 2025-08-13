using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SpecialGuide.Core.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIError? LastError { get; private set; }

    public OpenAIService(HttpClient httpClient, SettingsService settings, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public virtual async Task<string[]> GenerateSuggestionsAsync(byte[] image, string appName)
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
        using var response = await SendAsync(() => CreateChatRequest(payload));
        if (response == null) return Array.Empty<string>();
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();
            var suggestions = JsonSerializer.Deserialize<string[]>(content!);
            LastError = null;
            return suggestions ?? Array.Empty<string>();
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            LastError = new OpenAIError(null, ex.Message);
            _logger.LogError(ex, "Failed to parse suggestions");
            return Array.Empty<string>();
        }
    }

    public async Task<string> TranscribeAsync(byte[] wav)
    {
        var apiKey = _settings.ApiKey;
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(wav), "file", "audio.wav");
        content.Add(new StringContent("whisper-1"), "model");
        using var response = await SendAsync(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new MultipartFormDataContent
            {
                { new ByteArrayContent(wav), "file", "audio.wav" },
                { new StringContent("whisper-1"), "model" }
            };
            return req;
        });
        if (response == null) return string.Empty;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        LastError = null;
        return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }

    public async Task<string> ChatAsync(string prompt)
    {
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };
        using var response = await SendAsync(() => CreateChatRequest(payload));
        if (response == null) return string.Empty;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        LastError = null;
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    private HttpRequestMessage CreateChatRequest(object payload)
    {
        var apiKey = _settings.ApiKey;
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<HttpResponseMessage?> SendAsync(Func<HttpRequestMessage> requestFactory)
    {
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using var request = requestFactory();
            try
            {
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    LastError = null;
                    return response;
                }

                if (IsTransient(response.StatusCode) && attempt < maxRetries - 1)
                {
                    response.Dispose();
                    await Task.Delay(delay);
                    delay *= 2;
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync();
                LastError = new OpenAIError((int)response.StatusCode, body);
                _logger.LogError("OpenAI API call failed with status code {StatusCode}: {Body}", response.StatusCode, body);
                response.Dispose();
                return null;
            }
            catch (HttpRequestException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(delay);
                delay *= 2;
            }
            catch (Exception ex)
            {
                LastError = new OpenAIError(null, ex.Message);
                _logger.LogError(ex, "OpenAI API call failed");
                return null;
            }
        }
        return null;
    }

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    public record OpenAIError(int? StatusCode, string Message);
}
