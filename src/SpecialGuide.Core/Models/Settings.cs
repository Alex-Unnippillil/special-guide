namespace SpecialGuide.Core.Models;

public class Settings
{
    public string ApiKey { get; set; } = string.Empty;
    public bool AutoPaste { get; set; }
    public int MaxSuggestionLength { get; set; } = SpecialGuide.Core.Services.SuggestionService.DefaultMaxSuggestionLength;

    // Configurable hotkey used to activate the overlay.  The property was
    // historically called "ActivationHotkey" so we expose an alias for
    // backwards compatibility.
    public string Hotkey { get; set; } = string.Empty;

    // Alias for older settings files that still serialize under the name
    // "ActivationHotkey".  Reading from or writing to either property keeps the
    // values in sync.
    public string ActivationHotkey
    {
        get => Hotkey;
        set => Hotkey = value;
    }
}
