using System.Text.Json;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Tests;

public class SettingsSerializationTests
{
    [Fact]
    public void Hotkey_Serializes_And_Deserializes()
    {
        var settings = new Settings { Hotkey = "Ctrl+Alt+S" };

        var json = JsonSerializer.Serialize(settings);
        Assert.Contains("\"Hotkey\":\"Ctrl+Alt+S\"", json);

        var loaded = JsonSerializer.Deserialize<Settings>(json);
        Assert.Equal("Ctrl+Alt+S", loaded?.Hotkey);
    }
}

