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
    private readonly LoggingService _logger;

    public OpenAIService(SettingsService settings, IHttpClientFactory httpClientFactory, LoggingService logger)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public virtual async Task<string[]> GenerateSuggestionsAsync(byte[] image, string appName)
    {
        var apiKey = _settings.ApiKey;
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var base64 = Convert.ToBase64String(image);
        var imageUrl = "data:image/png;base64," + base64;
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = $"Return 6 short, actionable next-step prompts tailored to {appName}." },
                new { role = "user", content = new object[]{ new { type="image_url", image_url = new { url = imageUrl } } } }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            var ex = new HttpRequestException($"Failed to generate suggestions: {response.StatusCode}");
            _logger.LogError(ex, error);
            throw ex;
        }
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatCompletionResponse>(json);
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();
        try
        {
            var suggestions = JsonSerializer.Deserialize<string[]>(content!);
            return suggestions ?? Array.Empty<string>();
        }
        catch
        {
            return content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public async Task<string> TranscribeAsync(byte[] wav)
    {
        var apiKey = _settings.ApiKey;
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(wav), "file", "audio.wav");
        content.Add(new StringContent("whisper-1"), "model");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = content;
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            var ex = new HttpRequestException($"Transcription failed: {response.StatusCode}");
            _logger.LogError(ex, error);
            throw ex;
        }
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TranscriptionResponse>(json);
        return result?.Text ?? string.Empty;
    }

    public async Task<string> ChatAsync(string prompt)
    {
        var apiKey = _settings.ApiKey;
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            var ex = new HttpRequestException($"Chat request failed: {response.StatusCode}");
            _logger.LogError(ex, error);
            throw ex;
        }
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatCompletionResponse>(json);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }
}

internal record ChatCompletionResponse(ChatChoice[] Choices);
internal record ChatChoice(ChatMessage Message);
internal record ChatMessage(string Content);
internal record TranscriptionResponse(string Text);
