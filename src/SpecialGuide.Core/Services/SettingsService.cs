using System;
using System.IO;
using System.Text.Json;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Core.Services;

public class SettingsService
{
    private readonly string _path;
    public Settings Settings { get; private set; }
    public string ApiKey => Settings.ApiKey;

    public SettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpecialGuide");
        _path = Path.Combine(folder, "settings.json");
        if (File.Exists(_path))
        {
            Settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path)) ?? new Settings();
        }
        else
        {
            Settings = new Settings();
        }
    }

    public void Save()
    {
        var folder = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(folder);
        File.WriteAllText(_path, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
