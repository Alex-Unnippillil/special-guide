using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.Core.Models
{
    public enum CaptureMode
    {
        FullScreen,
        ActiveWindow
    }

    public class Settings
    {
        public string ApiKey { get; set; } = string.Empty;
        public bool AutoPaste { get; set; }
        public int MaxSuggestionLength { get; set; } = SuggestionService.DefaultMaxSuggestionLength;
        public CaptureMode CaptureMode { get; set; } = CaptureMode.FullScreen;
    }
}

namespace SpecialGuide.Core.Services
{
    public class SettingsService
    {
        public Settings Settings { get; }
        public string ApiKey => Settings.ApiKey;
        public SettingsService(Settings settings) => Settings = settings;
    }

    public class CaptureService
    {
        public CaptureService() { }
        public CaptureService(SettingsService _) { }
        public virtual byte[] CaptureScreen() => Array.Empty<byte>();
    }
}
