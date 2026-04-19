using System.ClientModel;
using OpenAI;

namespace AITranscribe.Core.Api;

public class GroqSttClient : ISttClient
{
    private readonly OpenAIClient _client;

    public GroqSttClient(string apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ApiKey = apiKey;
        _client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.groq.com/openai/v1")
        });
    }

    public string ApiKey { get; }

    public async Task<string> TranscribeAsync(Stream audio, string model, CancellationToken ct = default)
    {
        var audioClient = _client.GetAudioClient(model);
        var transcription = await audioClient.TranscribeAudioAsync(audio, "audio.wav", cancellationToken: ct);
        return transcription.Value.Text;
    }
}
