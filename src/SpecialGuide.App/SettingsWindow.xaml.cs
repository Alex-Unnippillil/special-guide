using System.Windows;
using System.Windows.Input;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly HookService _hookService;

    public SettingsWindow(SettingsService settings, HookService hookService)
    {
        InitializeComponent();
        _settings = settings;
        _hookService = hookService;
        DataContext = _settings.Settings;
    }

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            HotkeyBox.Text = string.Empty;
            _settings.Settings.Hotkey = string.Empty;
            return;
        }
        var hotkey = string.Empty;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) hotkey += "Control+";
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) hotkey += "Shift+";
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) hotkey += "Alt+";
        hotkey += key.ToString();
        HotkeyBox.Text = hotkey;
        _settings.Settings.Hotkey = hotkey;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!HookService.TryParseHotkey(_settings.Settings.Hotkey, out var parsed) || HookService.IsReservedHotkey(parsed))
        {
            MessageBox.Show("Hotkey is reserved or invalid. Falling back to middle click.", "Invalid hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
            _settings.Settings.Hotkey = string.Empty;
        }
        _settings.Save();
        _hookService.Reload();
        Close();
    }
}

