namespace AITranscribe.Core.Audio;

public class AudioRecorder
{
    private readonly IAudioCapture _capture;

    public bool IsRecording => _capture.IsRecording;

    public AudioRecorder(IAudioCapture capture)
    {
        _capture = capture;
    }

    public void Start()
    {
        _capture.Start();
    }

    public Task<byte[]> StopAsync(CancellationToken cancellationToken = default)
    {
        _capture.Stop();
        var data = _capture.GetCapturedData();
        return Task.FromResult(data);
    }
}
