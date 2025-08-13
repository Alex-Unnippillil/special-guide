using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using System.Threading.Tasks;

namespace SpecialGuide.App.Overlay;

public partial class RadialMenuWindow : Window, IRadialMenu
{
    private readonly ClipboardService _clipboardService;
    private readonly HookService _hookService;
    private readonly AudioService _audioService;
    private readonly OpenAIService _openAIService;
    private Button? _micButton;

    public RadialMenuWindow(ClipboardService clipboardService, HookService hookService, AudioService audioService, OpenAIService openAIService)
    {
        InitializeComponent();
        _clipboardService = clipboardService;
        _hookService = hookService;
        _audioService = audioService;
        _openAIService = openAIService;
    }

    public void Populate(string[] suggestions)
    {
        RootCanvas.Children.Clear();
        var count = suggestions.Length;
        var radius = 80d;
        for (int i = 0; i < count; i++)
        {
            var angle = 2 * Math.PI * i / count;
            var button = new Button
            {
                Content = suggestions[i],
                Width = 80,
                Height = 30,
                Tag = suggestions[i]
            };
            Canvas.SetLeft(button, radius + radius * Math.Cos(angle) - 40);
            Canvas.SetTop(button, radius + radius * Math.Sin(angle) - 15);
            button.Click += (_, _) => OnSuggestionSelected((string)button.Tag);
            RootCanvas.Children.Add(button);
        }

        var micButton = new Button
        {
            Content = "🎤",
            Width = 40,
            Height = 40
        };
        Canvas.SetLeft(micButton, RootCanvas.Width / 2 - 20);
        Canvas.SetTop(micButton, RootCanvas.Height / 2 - 20);
        micButton.Click += async (s, e) => await ToggleRecordingAsync();
        RootCanvas.Children.Add(micButton);
        _micButton = micButton;
    }

    public void Show(double x, double y)
    {
        Left = x - Width / 2;
        Top = y - Height / 2;
        Show();
        _hookService.SetOverlayVisible(true);
        Activate();
    }

    public void Hide()
    {
        base.Hide();
        _hookService.SetOverlayVisible(false);
    }

    private void OnSuggestionSelected(string text)
    {
        _clipboardService.SetText(text);
        Hide();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (_audioService.IsRecording)
        {
            _audioService.Stop();
        }
        Hide();
    }

    private async Task ToggleRecordingAsync()
    {
        if (!_audioService.IsRecording)
        {
            _audioService.Start();
            if (_micButton != null)
            {
                _micButton.Background = Brushes.Red;
                _micButton.Content = "■";
            }
            return;
        }

        var data = _audioService.Stop();
        if (_micButton != null)
        {
            _micButton.ClearValue(Button.BackgroundProperty);
            _micButton.Content = "🎤";
        }

        if (data.Length == 0)
        {
            Hide();
            return;
        }

        try
        {
            var text = await _openAIService.TranscribeAsync(data);
            if (!string.IsNullOrWhiteSpace(text))
            {
                _clipboardService.SetText(text);
            }
            Hide();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Transcription failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
