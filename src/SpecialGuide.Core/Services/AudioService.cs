using NAudio.Wave;

namespace SpecialGuide.Core.Services;

public class AudioService
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
        return _stream?.ToArray() ?? Array.Empty<byte>();
    }
}
