using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly SettingsViewModel _model;


    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _model = new SettingsViewModel
        {
            ApiKey = _settings.Settings.ApiKey,
            AutoPaste = _settings.Settings.AutoPaste,
            CaptureMode = _settings.Settings.CaptureMode,
            Hotkey = _settings.Settings.Hotkey,
            MaxSuggestionLength = _settings.Settings.MaxSuggestionLength
        };
        DataContext = _model;
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

    private void HotkeyBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        Forms.Keys mods = Forms.Keys.None;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= Forms.Keys.Control;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= Forms.Keys.Shift;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mods |= Forms.Keys.Alt;

        var vk = (Forms.Keys)KeyInterop.VirtualKeyFromKey(key);
        var hotkey = new HookService.Hotkey(vk, mods);
        if (HookService.IsReservedHotkey(hotkey))
        {
            HotkeyError.Text = "Reserved or unsupported hotkey";
            HotkeyError.Visibility = Visibility.Visible;
            HotkeyBox.BorderBrush = Brushes.Red;
            return;
        }

        HotkeyError.Visibility = Visibility.Collapsed;
        HotkeyBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);

        var parts = new List<string>();
        if (mods.HasFlag(Forms.Keys.Control)) parts.Add("Control");
        if (mods.HasFlag(Forms.Keys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(Forms.Keys.Alt)) parts.Add("Alt");
        parts.Add(vk.ToString());
        _model.Hotkey = string.Join("+", parts);
    }
}

