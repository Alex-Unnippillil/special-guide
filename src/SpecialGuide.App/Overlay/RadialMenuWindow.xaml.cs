using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing;
using System.Threading.Tasks;
using Forms = System.Windows.Forms;
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
        CancelButton.Click += (_, _) => { CancelRequested?.Invoke(this, EventArgs.Empty); Hide(); };
    }
    
    public event EventHandler? CancelRequested;

    public void ShowLoading()
    {
        RootCanvas.Children.Clear();
        var radius = 80d;
        RootCanvas.Children.Add(Spinner);
        RootCanvas.Children.Add(CancelButton);
        Canvas.SetLeft(Spinner, radius - Spinner.Width / 2);
        Canvas.SetTop(Spinner, radius - Spinner.Height / 2);
        Canvas.SetLeft(CancelButton, radius - CancelButton.Width / 2);
        Canvas.SetTop(CancelButton, radius + 20);
        Spinner.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
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
        RootCanvas.Children.Add(Spinner);
        RootCanvas.Children.Add(CancelButton);
        Spinner.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;
        Canvas.SetLeft(MicButton, radius - MicButton.Width / 2);
        Canvas.SetTop(MicButton, radius - MicButton.Height / 2);
        Canvas.SetLeft(Spinner, radius - Spinner.Width / 2);
        Canvas.SetTop(Spinner, radius - Spinner.Height / 2);
        Canvas.SetLeft(CancelButton, radius - CancelButton.Width / 2);
        Canvas.SetTop(CancelButton, radius + 20);
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
            Spinner.Visibility = Visibility.Visible;
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
                ShowErrorToast("Transcription failed");
            }
            finally
            {
                Spinner.Visibility = Visibility.Collapsed;
                Hide();
            }
        }
        else
        {
            _audioService.Start();
            _recording = true;
            MicButton.Content = "■";
            MicButton.Background = Brushes.Red;
        }
    }

    private static void ShowErrorToast(string message)
    {
        var icon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Error,
            Visible = true
        };
        icon.ShowBalloonTip(3000, "Error", message, Forms.ToolTipIcon.Error);
        _ = Task.Delay(4000).ContinueWith(_ => icon.Dispose());
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            Hide();
            return;
        }
        base.OnKeyDown(e);
    }
}
