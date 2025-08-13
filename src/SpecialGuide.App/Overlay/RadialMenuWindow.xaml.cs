using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace SpecialGuide.App.Overlay;

public partial class RadialMenuWindow : Window, IRadialMenu
{
    private readonly ClipboardService _clipboardService;
    private readonly HookService _hookService;
    private readonly AudioService _audioService;
    private readonly OpenAIService _openAIService;

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
        MicButton.Content = "🎤";
        RecordingIndicator.Visibility = Visibility.Collapsed;
        var count = suggestions.Length;
        var radius = 80d;
        RootCanvas.Children.Add(RecordingIndicator);
        RootCanvas.Children.Add(MicButton);
        Canvas.SetLeft(MicButton, radius - MicButton.Width / 2);
        Canvas.SetTop(MicButton, radius - MicButton.Height / 2);
        Canvas.SetLeft(RecordingIndicator, radius - RecordingIndicator.Width / 2);
        Canvas.SetTop(RecordingIndicator, radius - RecordingIndicator.Height / 2);
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
        if (_audioService.IsRecording)
        {
            _audioService.Stop();
            MicButton.Content = "🎤";
            RecordingIndicator.Visibility = Visibility.Collapsed;
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
        if (_audioService.IsRecording)
        {
            var data = _audioService.Stop();
            MicButton.Content = "🎤";
            RecordingIndicator.Visibility = Visibility.Collapsed;
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
                ShowToast("Transcription failed");
            }
            Hide();
        }
        else
        {
            _audioService.Start();
            MicButton.Content = "■";
            RecordingIndicator.Visibility = Visibility.Visible;
        }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Hide();
    }

    private static async void ShowToast(string message)
    {
        var icon = new WinForms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Information,
            Visible = true,
            BalloonTipTitle = "SpecialGuide",
            BalloonTipText = message
        };
        icon.ShowBalloonTip(3000);
        await Task.Delay(4000);
        icon.Dispose();
    }
}
