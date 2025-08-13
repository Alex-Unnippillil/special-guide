using System.Windows;
using Microsoft.Extensions.Logging;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class MainWindow : Window
{
    private readonly HookService _hookService;
    private readonly OverlayService _overlayService;
    private readonly SuggestionService _suggestionService;
    private readonly WindowService _windowService;
    private readonly ILogger<MainWindow> _logger;
    public MainWindow(HookService hookService, OverlayService overlayService, SuggestionService suggestionService, WindowService windowService, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _hookService = hookService;
        _overlayService = overlayService;
        _suggestionService = suggestionService;
        _windowService = windowService;
        _logger = logger;
        _hookService.HotkeyPressed += async (sender, e) =>
        {
            try
            {
                await OnHotkeyPressed(sender, e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling hotkey");
            }
        };
        _hookService.Start();
        Closed += (_, _) => _hookService.Stop();
    }

    private async Task OnHotkeyPressed(object? sender, EventArgs e)
    {
        var app = _windowService.GetActiveProcessName();
        var suggestions = await _suggestionService.GetSuggestionsAsync(app);
        _overlayService.ShowAtCursor(suggestions);
    }
}
