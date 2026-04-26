using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AITranscribe.Core.Audio;

public class WasapiAudioCapture : IAudioCapture, IDisposable
{
    private WasapiCapture? _capture;
    private readonly MemoryStream _buffer = new();
    private bool _disposed;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public void Start()
    {
        try
        {
            _buffer.SetLength(0);
            _capture = new WasapiCapture();
            _capture.WaveFormat = new WaveFormat(44100, 16, 1);
            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();
            _isRecording = true;
        }
        catch (Exception ex)
        {
            _capture?.Dispose();
            _capture = null;
            throw new InvalidOperationException(
                "Failed to start audio capture. Ensure a microphone is connected and not in use by another application.", ex);
        }
    }

    public void Stop()
    {
        if (_capture is not null)
        {
            _capture.StopRecording();
            _capture.DataAvailable -= OnDataAvailable;
            _capture.Dispose();
            _capture = null;
        }

        _isRecording = false;
    }

    public byte[] GetCapturedData()
    {
        return _buffer.ToArray();
    }

    private void OnDataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            _buffer.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _buffer.Dispose();
            _disposed = true;
        }
    }
}
