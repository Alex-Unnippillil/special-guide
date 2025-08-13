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
    private bool _busy;
    private CancellationTokenSource? _cts;
    public MainWindow(HookService hookService, OverlayService overlayService, SuggestionService suggestionService, WindowService windowService, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _hookService = hookService;
        _overlayService = overlayService;
        _suggestionService = suggestionService;
        _windowService = windowService;
        _logger = logger;
        _overlayService.Canceled += (_, _) => _cts?.Cancel();
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
        if (_busy)
            return;
        _busy = true;
        _cts = new CancellationTokenSource();
        _overlayService.ShowLoadingAtCursor();
        try
        {
            var app = _windowService.GetActiveProcessName();
            var result = await _suggestionService.GetSuggestionsAsync(app, _cts.Token);
            if (!string.IsNullOrEmpty(result.Error))
            {
                _overlayService.Hide();
                MessageBox.Show(result.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _overlayService.ShowAtCursor(result.Suggestions);
        }
        catch (OperationCanceledException)
        {
            _overlayService.Hide();
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _busy = false;
        }
    }
}
