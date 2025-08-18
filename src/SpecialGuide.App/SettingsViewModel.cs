using System.ComponentModel;
using SpecialGuide.Core.Models;

namespace SpecialGuide.App;

public class SettingsViewModel : INotifyPropertyChanged
{
    private string _apiKey = string.Empty;
    private bool _autoPaste;
    private CaptureMode _captureMode;
    private string _hotkey = string.Empty;
    private int _maxSuggestionLength;

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey != value)
            {
                _apiKey = value;
                OnPropertyChanged(nameof(ApiKey));
            }
        }
    }

    public bool AutoPaste
    {
        get => _autoPaste;
        set
        {
            if (_autoPaste != value)
            {
                _autoPaste = value;
                OnPropertyChanged(nameof(AutoPaste));
            }
        }
    }

    public CaptureMode CaptureMode
    {
        get => _captureMode;
        set
        {
            if (_captureMode != value)
            {
                _captureMode = value;
                OnPropertyChanged(nameof(CaptureMode));
            }
        }
    }

    public string Hotkey
    {
        get => _hotkey;
        set
        {
            if (_hotkey != value)
            {
                _hotkey = value;
                OnPropertyChanged(nameof(Hotkey));
            }
        }
    }

    public int MaxSuggestionLength
    {
        get => _maxSuggestionLength;
        set
        {
            if (_maxSuggestionLength != value)
            {
                _maxSuggestionLength = value;
                OnPropertyChanged(nameof(MaxSuggestionLength));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

