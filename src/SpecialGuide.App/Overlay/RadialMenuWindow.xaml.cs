using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Collections.Generic;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App.Overlay;

public partial class RadialMenuWindow : Window, IRadialMenu
{
    private readonly ClipboardService _clipboardService;
    private readonly HookService _hookService;
    private readonly AudioService _audioService;
    private readonly OpenAIService _openAIService;
    private readonly SuggestionHistoryService _historyService;
    private int _historyIndex = -1;
    private bool _recording;
    private CancellationTokenSource? _transcriptionCts;

    public RadialMenuWindow(ClipboardService clipboardService, HookService hookService, AudioService audioService, OpenAIService openAIService, SuggestionHistoryService historyService)
    {
        InitializeComponent();
        _clipboardService = clipboardService;
        _hookService = hookService;
        _audioService = audioService;
        _openAIService = openAIService;
        _historyService = historyService;
        MicButton.Click += OnMicClicked;
        CancelButton.Click += (_, _) => { CancelTranscription(); CancelRequested?.Invoke(this, EventArgs.Empty); Hide(); };
        HistoryButton.Click += (_, _) => ShowHistory();
    }
    
    public event EventHandler? CancelRequested;

    public void ShowLoading()
    {
        RootCanvas.Children.Clear();
        var radius = 80d;
        var diameter = radius * 2;
        RootCanvas.Width = diameter;
        RootCanvas.Height = diameter;
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
        _historyIndex = -1;
        MicButton.Content = "🎤";
        MicButton.ClearValue(Control.BackgroundProperty);
        Spinner.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;
        var count = suggestions.Length;
        var radius = 80d;

        var buttons = new List<Button>();
        double maxWidth = 0, maxHeight = 0;
        foreach (var text in suggestions)
        {
            var button = new Button
            {
                Content = text,
                Tag = text,
                Style = (Style)FindResource("SuggestionButtonStyle")
            };
            button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            maxWidth = Math.Max(maxWidth, button.DesiredSize.Width);
            maxHeight = Math.Max(maxHeight, button.DesiredSize.Height);
            buttons.Add(button);
        }

        var centerX = radius + maxWidth / 2;
        var centerY = radius + maxHeight / 2;

        RootCanvas.Width = 2 * radius + maxWidth;
        RootCanvas.Height = 2 * radius + maxHeight;

        RootCanvas.Children.Add(MicButton);
        RootCanvas.Children.Add(HistoryButton);
        Canvas.SetLeft(MicButton, centerX - MicButton.Width / 2);
        Canvas.SetTop(MicButton, centerY - MicButton.Height / 2);
        Canvas.SetLeft(HistoryButton, centerX - HistoryButton.Width / 2);
        Canvas.SetTop(HistoryButton, centerY + 20);
        HistoryButton.Visibility = Visibility.Visible;

        for (int i = 0; i < count; i++)
        {
            var angle = 2 * Math.PI * i / count;
            var button = buttons[i];
            var w = button.DesiredSize.Width;
            var h = button.DesiredSize.Height;
            Canvas.SetLeft(button, centerX + radius * Math.Cos(angle) - w / 2);
            Canvas.SetTop(button, centerY + radius * Math.Sin(angle) - h / 2);
            button.Click += (_, _) => OnSuggestionSelected((string)button.Tag);
            RootCanvas.Children.Add(button);
        }
    }

    public void Show(double x, double y)
    {
        // Display the window so that ActualWidth/ActualHeight are measured
        Show();
        // Ensure layout is up to date before positioning
        UpdateLayout();

        // Center the menu on the cursor position
        Left = x - ActualWidth / 2;
        Top = y - ActualHeight / 2;
        _hookService.SetOverlayVisible(true);
        Activate();
    }

    public new void Hide()
    {
        CancelTranscription();
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
            _transcriptionCts = new CancellationTokenSource();
            try
            {
                var text = await _openAIService.TranscribeAsync(data, _transcriptionCts.Token);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _clipboardService.SetText(text);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                MessageBox.Show("Transcription failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CancelTranscription();
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelTranscription();
            CancelRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            Hide();
            return;
        }
        if (e.Key == Key.H)
        {
            ShowHistory();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    private void CancelTranscription()
    {
        if (_transcriptionCts != null)
        {
            if (!_transcriptionCts.IsCancellationRequested)
            {
                _transcriptionCts.Cancel();
            }
            _transcriptionCts.Dispose();
            _transcriptionCts = null;
        }
    }

    private void ShowHistory()
    {
        var history = _historyService.GetHistory();
        if (history.Count == 0) return;
        _historyIndex = (_historyIndex + 1) % history.Count;
        Populate(history[_historyIndex]);
    }
}
