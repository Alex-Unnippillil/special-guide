using System.Windows;
using System.Windows.Controls;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App.Overlay;

public partial class RadialMenuWindow : Window, IRadialMenu
{
    private readonly ClipboardService _clipboardService;
    private readonly HookService _hookService;

    public RadialMenuWindow(ClipboardService clipboardService, HookService hookService)
    {
        InitializeComponent();
        _clipboardService = clipboardService;
        _hookService = hookService;
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
        Hide();
    }
}
