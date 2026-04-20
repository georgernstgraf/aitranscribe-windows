using System.ClientModel;
using OpenAI;

namespace AITranscribe.Core.Api;

public class GroqSttClient : ISttClient
{
    public string ApiKey { get; }

    public GroqSttClient(string apiKey)
    {
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    public async Task<string> TranscribeAsync(Stream audio, string model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("Groq API key is not configured. Set Groq.ApiKey in config.json.");

        var client = new OpenAIClient(new ApiKeyCredential(ApiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.groq.com/openai/v1")
        });

        var audioClient = client.GetAudioClient(model);
        var transcription = await audioClient.TranscribeAudioAsync(audio, "audio.wav", cancellationToken: ct);
        return transcription.Value.Text;
    }
}
