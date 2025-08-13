using System.Windows;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        LoadValues(settingsService.Settings);
        _settingsService.SettingsChanged += SettingsServiceOnSettingsChanged;
        _settingsService.Error += msg => Dispatcher.Invoke(() => MessageBox.Show(msg));
        Closing += (s, e) => { e.Cancel = true; Hide(); };
    }

    private void SettingsServiceOnSettingsChanged(Settings s)
    {
        Dispatcher.Invoke(() => LoadValues(s));
    }

    private void LoadValues(Settings s)
    {
        ApiKeyTextBox.Text = s.ApiKey;
        AutoPasteCheckBox.IsChecked = s.AutoPaste;
        MaxSuggestionTextBox.Text = s.MaxSuggestionLength.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = new Settings
        {
            ApiKey = ApiKeyTextBox.Text,
            AutoPaste = AutoPasteCheckBox.IsChecked ?? false,
            MaxSuggestionLength = int.TryParse(MaxSuggestionTextBox.Text, out var v)
                ? v : _settingsService.Settings.MaxSuggestionLength
        };
        _settingsService.Save(settings);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
