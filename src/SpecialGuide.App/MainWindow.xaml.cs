using System.Threading;
using System.Windows;
using Microsoft.Extensions.Logging;
using SpecialGuide.App.Overlay;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class MainWindow : Window
{
    private readonly HookService _hookService;
    private readonly OverlayService _overlayService;
    private readonly SuggestionService _suggestionService;
    private readonly WindowService _windowService;
    private readonly ILogger<MainWindow> _logger;
    private readonly RadialMenuWindow _menu;
    private bool _busy;
    private CancellationTokenSource? _cts;
    public MainWindow(HookService hookService, OverlayService overlayService, SuggestionService suggestionService, WindowService windowService, ILogger<MainWindow> logger, RadialMenuWindow menu)
    {
        InitializeComponent();
        _hookService = hookService;
        _overlayService = overlayService;
        _suggestionService = suggestionService;
        _windowService = windowService;
        _logger = logger;
        _menu = menu;
        _menu.CancelRequested += (_, _) => CancelRequest();
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
        _overlayService.ShowAtCursor(Array.Empty<string>());

        try
        {
            var app = _windowService.GetActiveProcessName();
            var suggestions = await _suggestionService.GetSuggestionsAsync(app, _cts.Token);
            if (!_cts.IsCancellationRequested)
            {
                _overlayService.ShowAtCursor(suggestions);
            }
        }
        catch (OperationCanceledException)
        {
            _overlayService.Hide();
        }
        catch (Exception)
        {
            _overlayService.Hide();
            throw;
        }
        finally
        {
            _busy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelRequest()
    {
        if (!_busy)
            return;
        _cts?.Cancel();
    }
}
