using System;
using Microsoft.Extensions.Options;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Core.Services;

public class SettingsService
{
    private readonly IOptionsMonitor<Settings> _settings;
    public Settings Settings => _settings.CurrentValue;
    public string ApiKey => Settings.ApiKey;

    public event Action<Settings>? SettingsChanged;

    public SettingsService(IOptionsMonitor<Settings> settings)
    {
        _settings = settings;
        _settings.OnChange(s => SettingsChanged?.Invoke(s));
    }
}
