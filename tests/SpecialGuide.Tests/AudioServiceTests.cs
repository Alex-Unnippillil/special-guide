using System;
using System.IO;
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

    private class TestAudioService : AudioService
    {
        private class StubWaveIn : IWaveIn
        {
            public WaveFormat WaveFormat { get; set; } = new WaveFormat(8000, 16, 1);
            public event EventHandler<WaveInEventArgs>? DataAvailable;
            public event EventHandler<StoppedEventArgs>? RecordingStopped;
            public void StartRecording() { }
            public void StopRecording() { }
            public void Dispose() { }
        }

        public MemoryStream? TestStream => Stream;
        public WaveFileWriter? TestWriter => Writer;
        protected internal override IWaveIn CreateWaveInEvent() => new StubWaveIn();
    }

    [Fact]
    public void StartStop_Can_Be_Called_Multiple_Times()
    {
        var service = new TestAudioService();

        service.Start();
        var data1 = service.Stop();
        Assert.NotNull(data1);
        Assert.Null(service.TestStream);
        Assert.Null(service.TestWriter);

        var empty = service.Stop();
        Assert.Empty(empty);

        service.Start();
        service.Start(); // should be no-op
        var data2 = service.Stop();
        Assert.NotNull(data2);
        Assert.Null(service.TestStream);
        Assert.Null(service.TestWriter);
    }
}
