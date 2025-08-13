using System.Windows;
using Microsoft.Extensions.Logging;
using SpecialGuide.Core.Services;
using System.Threading;

namespace SpecialGuide.App;

public partial class MainWindow : Window
{
    private readonly HookService _hookService;
    private readonly OverlayService _overlayService;
    private readonly SuggestionService _suggestionService;
    private readonly WindowService _windowService;
    private readonly ILogger<MainWindow> _logger;
    private bool _isBusy;
    private CancellationTokenSource? _cts;
    public MainWindow(HookService hookService, OverlayService overlayService, SuggestionService suggestionService, WindowService windowService, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _hookService = hookService;
        _overlayService = overlayService;
        _suggestionService = suggestionService;
        _windowService = windowService;
        _logger = logger;
        _overlayService.Cancelled += (_, _) => _cts?.Cancel();
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
        Closed += (_, _) => _hookService.Stop();
    }

    private async Task OnMiddleClick(object? sender, EventArgs e)
    {
        if (_isBusy) return;
        _isBusy = true;
        _cts = new CancellationTokenSource();
        _overlayService.ShowLoadingAtCursor();
        try
        {
            var app = _windowService.GetActiveProcessName();
            var suggestions = await _suggestionService.GetSuggestionsAsync(app, _cts.Token);
            if (!_cts.Token.IsCancellationRequested)
            {
                _overlayService.ShowAtCursor(suggestions);
            }
        }
        catch (OperationCanceledException)
        {
            // cancelled
        }
        finally
        {
            _overlayService.Hide();
            _cts.Dispose();
            _cts = null;
            _isBusy = false;
        }
    }
}
