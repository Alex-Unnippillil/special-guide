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

        service.Stream = stream;
        service.Writer = writer;
        typeof(AudioService).GetProperty("IsRecording")?.SetValue(service, true);

        var data = service.Stop();
        Assert.NotNull(data);

        Assert.Null(service.Stream);
        Assert.Null(service.Writer);
    }
}
