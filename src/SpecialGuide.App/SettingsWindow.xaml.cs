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
        if (!_hookService.Reload())
        {
            MessageBox.Show("Hotkey registration failed. Using middle-click instead.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Close();
    }
}

