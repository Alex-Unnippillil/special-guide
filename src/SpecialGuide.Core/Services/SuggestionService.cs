using System.Linq;
using System.Threading.Tasks;
namespace SpecialGuide.Core.Services;

public class SuggestionService
{
    private readonly CaptureService _capture;
    private readonly OpenAIService _openAI;

    public SuggestionService(CaptureService capture, OpenAIService openAI)
    {
        _capture = capture;
        _openAI = openAI;
    }

    public async Task<string[]> GetSuggestionsAsync(string appName)
    {
        var image = await _capture.CaptureScreenAsync();
        var suggestions = await _openAI.GenerateSuggestionsAsync(image, appName);
        return suggestions.Select(s => s.Length > 80 ? s[..80] : s).ToArray();
    }
}
