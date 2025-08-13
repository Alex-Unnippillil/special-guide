using NAudio.Wave;

namespace SpecialGuide.Core.Services;

public class AudioService : IDisposable
{
    private WaveInEvent? _waveIn;
    private EventHandler<WaveInEventArgs>? _dataAvailableHandler;
    protected internal virtual MemoryStream? Stream { get; set; }
    protected internal virtual WaveFileWriter? Writer { get; set; }

    public bool IsRecording { get; private set; }

    public void Start()
    {
        if (IsRecording) return;
        _waveIn = new WaveInEvent();
        Stream = new MemoryStream();
        Writer = new WaveFileWriter(Stream, _waveIn.WaveFormat);

        _dataAvailableHandler = (s, a) => Writer?.Write(a.Buffer, 0, a.BytesRecorded);
        _waveIn.DataAvailable += _dataAvailableHandler;

        _waveIn.StartRecording();
        IsRecording = true;
    }

    public byte[] Stop()
    {
        if (!IsRecording) return Array.Empty<byte>();

        if (_waveIn != null && _dataAvailableHandler != null)
        {
            _waveIn.DataAvailable -= _dataAvailableHandler;
            _dataAvailableHandler = null;
        }

        _waveIn?.StopRecording();

        Writer?.Flush();
        var data = Stream?.ToArray() ?? Array.Empty<byte>();
        Dispose();
        IsRecording = false;
        return data;
    }

    public void Dispose()
    {
        if (_waveIn != null && _dataAvailableHandler != null)
        {
            _waveIn.DataAvailable -= _dataAvailableHandler;
            _dataAvailableHandler = null;
        }

        _waveIn?.Dispose();
        _waveIn = null;
        Writer?.Dispose();
        Writer = null;
        Stream?.Dispose();
        Stream = null;
        IsRecording = false;
    }
}
