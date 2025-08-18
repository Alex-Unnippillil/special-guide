using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace SpecialGuide.Core.Services;

public class SuggestionService
{
    public const int DefaultMaxSuggestionLength = 80;

    private readonly CaptureService _capture;
    private readonly OpenAIService _openAI;
    private readonly SettingsService _settings;
    private readonly SuggestionHistoryService _history;

    public SuggestionService(CaptureService capture, OpenAIService openAI, SettingsService settings, SuggestionHistoryService history)
    {
        _capture = capture;
        _openAI = openAI;
        _settings = settings;
        _history = history;
    }

    public async Task<SuggestionResult> GetSuggestionsAsync(string appName, CancellationToken cancellationToken = default)
    {
        byte[] image;
        try
        {
            image = _capture.CaptureScreen();
        }
        catch (Exception ex)
        {
            return new SuggestionResult(Array.Empty<string>(), new OpenAIError(null, ex.Message));
        }

        try
        {
            var result = await _openAI.GenerateSuggestionsAsync(image, appName, cancellationToken);
            var max = _settings.Settings.MaxSuggestionLength;
            var suggestions = result.Suggestions.Select(s => s.Length > max ? s[..max] : s).ToArray();
            if (result.Error == null && suggestions.Length > 0)
            {
                _history.Add(suggestions);
            }
            return result with { Suggestions = suggestions };
        }
        catch (OperationCanceledException)
        {
            return new SuggestionResult(Array.Empty<string>(), new OpenAIError(null, "Canceled"));
        }
    }
}
