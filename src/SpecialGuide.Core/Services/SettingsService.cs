using System;
using System.IO;
using System.Text.Json;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Core.Services;

public class SettingsService : IDisposable
{
    private readonly string _settingsPath;
    private readonly FileSystemWatcher _watcher;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private bool _suppressWatcher;

    public Settings Settings { get; private set; } = new();
    public string ApiKey => Settings.ApiKey;

    public event Action<Settings>? SettingsChanged;
    public event Action<string>? Error;

    public SettingsService()
    {
        _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpecialGuide", "appsettings.json");
        Load();
        var dir = Path.GetDirectoryName(_settingsPath)!;
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(_settingsPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };
        _watcher.Changed += (_, _) => Reload();
        _watcher.Created += (_, _) => Reload();
        _watcher.Renamed += (_, _) => Reload();
        _watcher.EnableRaisingEvents = true;
    }

    private void Reload()
    {
        if (_suppressWatcher) return;
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                Settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            else
            {
                Settings = new Settings();
                Save();
            }
            SettingsChanged?.Invoke(Settings);
        }
        catch (Exception ex)
        {
            Settings = new Settings();
            Error?.Invoke($"Failed to load settings: {ex.Message}");
        }
    }

    public void Save(Settings? settings = null)
    {
        if (settings != null)
        {
            Settings = settings;
        }
        try
        {
            _suppressWatcher = true;
            var dir = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
            SettingsChanged?.Invoke(Settings);
        }
        catch (Exception ex)
        {
            Error?.Invoke($"Failed to save settings: {ex.Message}");
        }
        finally
        {
            _suppressWatcher = false;
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}

