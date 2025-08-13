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

    public SuggestionService(CaptureService capture, OpenAIService openAI, SettingsService settings)
    {
        _capture = capture;
        _openAI = openAI;
        _settings = settings;
    }

    public async Task<SuggestionResult> GetSuggestionsAsync(string appName, CancellationToken token)
    {
        var image = _capture.CaptureScreen();
        var result = await _openAI.GenerateSuggestionsAsync(image, appName).WaitAsync(token);
        var max = _settings.Settings.MaxSuggestionLength;
        var suggestions = result.Suggestions.Select(s => s.Length > max ? s[..max] : s).ToArray();
        return new SuggestionResult(suggestions, result.Error);
    }
}
