namespace AITranscribe.Core.Api;

public interface ISttClient
{
    Task<string> TranscribeAsync(Stream audio, string model, CancellationToken ct = default);
}
