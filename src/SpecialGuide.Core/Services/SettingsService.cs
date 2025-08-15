using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Core.Services;

public class SettingsService : IDisposable
{
    private readonly string _path;
    private readonly FileSystemWatcher? _watcher;
    private Settings _settings;

    public Settings Settings => _settings;
    public string ApiKey => _settings.ApiKey;


    public event Action<Settings>? SettingsChanged;

    public SettingsService() : this(new Settings()) { }

    public SettingsService(Settings defaults)
    {
        _settings = defaults;
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpecialGuide",
            "appsettings.json");
        try
        {
            var dir = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(dir);
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<Settings>(json, CreateOptions());
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
            else
            {
                Save();
            }

            _watcher = new FileSystemWatcher(dir, Path.GetFileName(_path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName
            };
            _watcher.Changed += (_, _) => Reload();
            _watcher.Created += (_, _) => Reload();
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Warn($"Failed to initialize settings file: {ex.Message}");
        }
    }

    private void Reload()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<Settings>(json, CreateOptions());
                if (loaded != null)
                {
                    _settings = loaded;
                    SettingsChanged?.Invoke(_settings);
                }
            }
        }
        catch (Exception ex)
        {
            Warn($"Failed to read settings: {ex.Message}");
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, CreateOptions());
            File.WriteAllText(_path, json);
            SettingsChanged?.Invoke(_settings);
        }
        catch (Exception ex)
        {
            Warn($"Failed to save settings: {ex.Message}");
        }
    }

    private static JsonSerializerOptions CreateOptions()
        => new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

    private static void Warn(string message) => Console.Error.WriteLine(message);

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}

