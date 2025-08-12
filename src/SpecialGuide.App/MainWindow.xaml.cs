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
        _hookService.MiddleClick += async (sender, e) =>
        {
            try
            {
                await OnMiddleClick(sender, e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling middle click");
            }
        };
        _hookService.Start();
    }

    private async Task OnMiddleClick(object? sender, EventArgs e)
    {
        var app = _windowService.GetActiveProcessName();
        var suggestions = await _suggestionService.GetSuggestionsAsync(app);
        _overlayService.ShowAtCursor(suggestions);
    }
}
