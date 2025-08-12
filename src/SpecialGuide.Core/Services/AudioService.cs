using NAudio.Wave;

namespace SpecialGuide.Core.Services;

public class AudioService : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _stream;
    private WaveFileWriter? _writer;
    private EventHandler<WaveInEventArgs>? _dataAvailableHandler;

    public void Start()
    {
        _waveIn = new WaveInEvent();
        _stream = new MemoryStream();
        _writer = new WaveFileWriter(_stream, _waveIn.WaveFormat);
        _dataAvailableHandler = (s, a) => _writer.Write(a.Buffer, 0, a.BytesRecorded);
        _waveIn.DataAvailable += _dataAvailableHandler;
        _waveIn.StartRecording();
    }

    public byte[] Stop()
    {
        if (_waveIn != null && _dataAvailableHandler != null)
        {
            _waveIn.DataAvailable -= _dataAvailableHandler;
        }
        _waveIn?.StopRecording();
        _writer?.Flush();
        var data = _stream?.ToArray() ?? Array.Empty<byte>();
        Dispose();
        return data;
    }

    public void Dispose()
    {
        if (_waveIn != null && _dataAvailableHandler != null)
        {
            _waveIn.DataAvailable -= _dataAvailableHandler;
        }
        _waveIn?.Dispose();
        _waveIn = null;
        _dataAvailableHandler = null;
        _writer?.Dispose();
        _writer = null;
        _stream?.Dispose();
        _stream = null;
    }
}
