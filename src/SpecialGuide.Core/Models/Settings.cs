using System.Collections.Generic;

namespace SpecialGuide.Core.Models;

public enum CaptureMode
{
    FullScreen,
    ActiveWindow
}

public class Settings
{
    public string ApiKey { get; set; } = string.Empty;
    public CaptureMode CaptureMode { get; set; } = CaptureMode.FullScreen;
    public string Hotkey { get; set; } = string.Empty;
    public bool AutoPaste { get; set; }
    public int MaxSuggestionLength { get; set; } = SpecialGuide.Core.Services.SuggestionService.DefaultMaxSuggestionLength;
    public bool RedactTitle { get; set; }
    public List<string> RedactTitlePatterns { get; set; } = new();

}
