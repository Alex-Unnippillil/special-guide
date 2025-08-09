using System.Windows;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class MainWindow : Window
{
    private readonly HookService _hookService;
    private readonly OverlayService _overlayService;
    private readonly SuggestionService _suggestionService;

    public MainWindow(HookService hookService, OverlayService overlayService, SuggestionService suggestionService)
    {
        InitializeComponent();
        _hookService = hookService;
        _overlayService = overlayService;
        _suggestionService = suggestionService;
        _hookService.MiddleClick += OnMiddleClick;
        _hookService.Start();
    }

    private async void OnMiddleClick(object? sender, EventArgs e)
    {
        var suggestions = await _suggestionService.GetSuggestionsAsync("app");
        _overlayService.ShowAtCursor(suggestions);
    }
}
