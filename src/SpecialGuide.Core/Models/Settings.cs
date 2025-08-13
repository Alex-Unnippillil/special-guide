namespace SpecialGuide.Core.Models;

public class Settings
{
    public string ApiKey { get; set; } = string.Empty;
    public bool AutoPaste { get; set; }
    public int MaxSuggestionLength { get; set; } = SpecialGuide.Core.Services.SuggestionService.DefaultMaxSuggestionLength;
    public string ActivationHotkey { get; set; } = string.Empty;
}
