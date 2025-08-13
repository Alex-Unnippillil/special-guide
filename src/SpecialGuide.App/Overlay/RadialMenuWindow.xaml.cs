using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App.Overlay;

public partial class RadialMenuWindow : Window, IRadialMenu
{
    private readonly ClipboardService _clipboardService;
    private readonly HookService _hookService;
    private readonly AudioService _audioService;
    private readonly OpenAIService _openAIService;
    private bool _recording;

    public RadialMenuWindow(ClipboardService clipboardService, HookService hookService, AudioService audioService, OpenAIService openAIService)
    {
        InitializeComponent();
        _clipboardService = clipboardService;
        _hookService = hookService;
        _audioService = audioService;
        _openAIService = openAIService;
        MicButton.Click += OnMicClicked;
    }

    public void Populate(string[] suggestions)
    {
        RootCanvas.Children.Clear();
        _recording = false;
        MicButton.Content = "🎤";
        MicButton.ClearValue(Control.BackgroundProperty);
        var count = suggestions.Length;
        var radius = 80d;
        RootCanvas.Children.Add(MicButton);
        Canvas.SetLeft(MicButton, radius - MicButton.Width / 2);
        Canvas.SetTop(MicButton, radius - MicButton.Height / 2);
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
    }

    public void Show(double x, double y)
    {
        Left = x - Width / 2;
        Top = y - Height / 2;
        Show();
        _hookService.SetOverlayVisible(true);
        Activate();
    }

    public new void Hide()
    {
        if (_recording)
        {
            _audioService.Stop();
            _recording = false;
            MicButton.Content = "🎤";
            MicButton.ClearValue(Control.BackgroundProperty);
        }
        base.Hide();
        _hookService.SetOverlayVisible(false);
    }

    private void OnSuggestionSelected(string text)
    {
        _clipboardService.SetText(text);
        Hide();
    }

    private async void OnMicClicked(object sender, RoutedEventArgs e)
    {
        if (_recording)
        {
            var data = _audioService.Stop();
            _recording = false;
            MicButton.Content = "🎤";
            MicButton.ClearValue(Control.BackgroundProperty);
            try
            {
                var text = await _openAIService.TranscribeAsync(data);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _clipboardService.SetText(text);
                }
            }
            catch
            {
                MessageBox.Show("Transcription failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Hide();
        }
        else
        {
            _audioService.Start();
            _recording = true;
            MicButton.Content = "■";
            MicButton.Background = Brushes.Red;
        }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Hide();
    }
}
