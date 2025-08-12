using NAudio.Wave;

namespace SpecialGuide.Core.Services;

public class AudioService : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _stream;
    private WaveFileWriter? _writer;

    public void Start()
    {
        _waveIn = new WaveInEvent();
        _stream = new MemoryStream();
        _writer = new WaveFileWriter(_stream, _waveIn.WaveFormat);
        _waveIn.DataAvailable += (s, a) => _writer.Write(a.Buffer, 0, a.BytesRecorded);
        _waveIn.StartRecording();
    }

    public byte[] Stop()
    {
        _waveIn?.StopRecording();
        _writer?.Flush();
        var data = _stream?.ToArray() ?? Array.Empty<byte>();
        Dispose();
        return data;
    }

    public void Dispose()
    {
        _waveIn?.Dispose();
        _waveIn = null;
        _writer?.Dispose();
        _writer = null;
        _stream?.Dispose();
        _stream = null;
    }
}
