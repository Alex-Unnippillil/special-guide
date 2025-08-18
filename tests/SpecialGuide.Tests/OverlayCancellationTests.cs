using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class OverlayCancellationTests
{
    [Fact]
    public async Task Cancel_Request_Hides_Menu_And_Cancels_Suggestions()
    {
        var menu = new FakeRadialMenu();
        var overlay = new OverlayService(menu);
        var suggestion = new CancelableSuggestionService();
        var harness = new Harness(overlay, suggestion);

        var task = harness.TriggerAsync();
        await Task.Delay(50);
        menu.TriggerCancel();
        await task; // wait for completion

        Assert.True(menu.HideCalled);
        Assert.True(suggestion.WasCanceled);
    }

    private class Harness
    {
        private readonly OverlayService _overlayService;
        private readonly SuggestionService _suggestionService;
        private bool _busy;
        private CancellationTokenSource? _cts;

        public Harness(OverlayService overlayService, SuggestionService suggestionService)
        {
            _overlayService = overlayService;
            _suggestionService = suggestionService;
            _overlayService.CancelRequested += (_, _) => CancelActive();
        }

        public Task TriggerAsync() => OnHotkeyPressed();

        private async Task OnHotkeyPressed()
        {
            if (_busy) return;
            _busy = true;
            _cts = new CancellationTokenSource();
            try
            {
                await _suggestionService.GetSuggestionsAsync("app", _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _overlayService.Hide();
            }
            finally
            {
                _busy = false;
            }
        }

        private void CancelActive()
        {
            if (!_busy) return;
            _cts?.Cancel();
        }
    }

    private class FakeRadialMenu : IRadialMenu
    {
        public bool HideCalled { get; private set; }
        public event EventHandler? CancelRequested;
        public void Populate(string[] suggestions) { }
        public void Show(double x, double y) { }
        public void ShowLoading() { }
        public void Hide() => HideCalled = true;
        public void TriggerCancel()
        {
            Hide();
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private class CancelableSuggestionService : SuggestionService
    {
        public bool WasCanceled { get; private set; }
        public CancelableSuggestionService() : base(new FakeCapture(), new FakeOpenAI(), new SettingsService(new Settings())) { }
        public override async Task<SuggestionResult> GetSuggestionsAsync(string app, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
                throw;
            }
            return new SuggestionResult(Array.Empty<string>(), null);
        }

        private class FakeCapture : CaptureService
        {
            public FakeCapture() : base(new SettingsService(new Settings())) { }
            public override byte[] CaptureScreen() => Array.Empty<byte>();
        }

        private class FakeOpenAI : OpenAIService
        {
            public FakeOpenAI() : base(new HttpClient(), new SettingsService(new Settings()), new LoggingService()) { }
            public override Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName, CancellationToken cancellationToken = default)
                => Task.FromResult(new SuggestionResult(Array.Empty<string>(), null));
        }
    }
}

namespace SpecialGuide.Core.Services
{
    public class LoggingService : Microsoft.Extensions.Logging.ILogger<OpenAIService>
    {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        public void LogError(Exception ex, string message) { }
        private class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
