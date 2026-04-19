namespace AITranscribe.Core.Audio;

public interface IAudioCapture
{
    bool IsRecording { get; }
    void Start();
    void Stop();
    byte[] GetCapturedData();
}
