using System.Collections.Generic;
using System.Windows;

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
}

