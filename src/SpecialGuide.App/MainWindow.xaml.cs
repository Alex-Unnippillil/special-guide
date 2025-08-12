using System.Windows;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class MainWindow : Window
{
    private readonly HookService _hookService;
    private readonly OverlayService _overlayService;
    private readonly SuggestionService _suggestionService;
    private readonly ActiveWindowService _activeWindowService;

    public MainWindow(HookService hookService, OverlayService overlayService, SuggestionService suggestionService, ActiveWindowService activeWindowService)
    {
        InitializeComponent();
        _hookService = hookService;
        _overlayService = overlayService;
        _suggestionService = suggestionService;
        _activeWindowService = activeWindowService;
        _hookService.MiddleClick += OnMiddleClick;
        _hookService.Start();
    }

    private async void OnMiddleClick(object? sender, EventArgs e)
    {
        var appName = _activeWindowService.GetActiveWindowTitle();
        var suggestions = await _suggestionService.GetSuggestionsAsync(appName);
        _overlayService.ShowAtCursor(suggestions);
    }
}
