using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly Settings _model;

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        var s = settings.Settings;
        _model = new Settings
        {
            ApiKey = s.ApiKey,
            AutoPaste = s.AutoPaste,
            CaptureMode = s.CaptureMode,
            Hotkey = s.Hotkey,
            MaxSuggestionLength = s.MaxSuggestionLength,
        };
        DataContext = _model;
    }

    private void OnHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            _model.Hotkey = string.Empty;
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var parts = new List<string>();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Control");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());
        _model.Hotkey = string.Join("+", parts);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Settings.ApiKey = _model.ApiKey;
        _settings.Settings.AutoPaste = _model.AutoPaste;
        _settings.Settings.CaptureMode = _model.CaptureMode;
        _settings.Settings.Hotkey = _model.Hotkey;
        _settings.Settings.MaxSuggestionLength = _model.MaxSuggestionLength;
        _settings.Save();

        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}

