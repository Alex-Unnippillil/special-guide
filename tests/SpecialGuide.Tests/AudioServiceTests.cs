using System.Reflection;
using NAudio.Wave;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class AudioServiceTests
{
    [Fact]
    public void Stop_Disposes_Resources()
    {
        var service = new AudioService();

        // Inject dummy resources
        var stream = new MemoryStream();
        var writer = new WaveFileWriter(stream, new WaveFormat(8000, 16, 1));

        var type = typeof(AudioService);
        type.GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(service, stream);
        type.GetField("_writer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(service, writer);

        var data = service.Stop();
        Assert.NotNull(data);

        Assert.Null(type.GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service));
        Assert.Null(type.GetField("_writer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service));
    }
}
