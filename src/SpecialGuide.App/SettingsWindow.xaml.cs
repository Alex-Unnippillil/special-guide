using System.Windows;
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

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Save();
        _hookService.Start();
        if (_hookService.UsingFallback && !string.IsNullOrWhiteSpace(_settings.Settings.ActivationHotkey))
        {
            MessageBox.Show("Failed to register hotkey. Using middle-click fallback.", "Hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Close();
    }
}

