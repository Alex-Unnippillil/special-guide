using System;
using System.Threading;
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

        _overlayService.CancelRequested += (_, _) => CancelActive();

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
        if (_busy) return;
        _busy = true;
        _cts = new CancellationTokenSource();
        _overlayService.ShowLoadingAtCursor();
        try
        {
            var app = _windowService.GetActiveProcessName();
            var result = await _suggestionService.GetSuggestionsAsync(app, _cts.Token);
            if (result.Error != null)
            {
                MessageBox.Show(result.Error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _overlayService.Hide();
            }
            else
            {
                _overlayService.ShowSuggestions(result.Suggestions);
            }
        }
        catch (OperationCanceledException)
        {
            _overlayService.Hide();
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _busy = false;
        }
    }

    private void CancelActive()
    {
        if (!_busy || _cts == null) return;
        _cts.Cancel();
    }
}
