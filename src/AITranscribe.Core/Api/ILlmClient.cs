namespace AITranscribe.Core.Api;

public interface ILlmClient
{
    Task<string> ProcessAsync(string text, string systemPrompt, string model, string baseUrl, string apiKey, CancellationToken ct = default);
}
