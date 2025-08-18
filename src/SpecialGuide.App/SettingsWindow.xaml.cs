using System.Windows;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    public Settings Settings { get; }

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        Settings = _settings.Settings;
        DataContext = Settings;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Save();

        Close();
    }
}

