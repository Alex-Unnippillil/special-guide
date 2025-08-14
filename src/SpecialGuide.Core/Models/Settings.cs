namespace SpecialGuide.Core.Models;

public class Settings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
    public bool AutoPaste { get; set; }
    public int MaxSuggestionLength { get; set; } = SpecialGuide.Core.Services.SuggestionService.DefaultMaxSuggestionLength;

}
