using System.Collections.Generic;
using System.Windows;

using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;


    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;

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

