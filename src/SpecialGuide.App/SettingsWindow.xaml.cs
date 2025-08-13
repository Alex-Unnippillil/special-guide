using System.Windows;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        DataContext = _settings.Settings;
        _settings.SettingsChanged += s => Dispatcher.Invoke(() => DataContext = s);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Save();
        Close();
    }
}

