using System.Text.Json;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void SerializesAndDeserializesHotkey()
    {
        var settings = new Settings { Hotkey = "Ctrl+Shift+H" };
        var json = JsonSerializer.Serialize(settings);
        var loaded = JsonSerializer.Deserialize<Settings>(json);

        Assert.Equal("Ctrl+Shift+H", loaded?.Hotkey);
        Assert.Contains("\"Hotkey\":\"Ctrl+Shift+H\"", json);
    }
}

