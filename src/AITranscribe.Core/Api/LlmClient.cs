using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace AITranscribe.Core.Api;

public class LlmClient : ILlmClient
{
    private readonly string _baseSystemPrompt;

    public LlmClient(string baseSystemPrompt)
    {
        _baseSystemPrompt = baseSystemPrompt ?? throw new ArgumentNullException(nameof(baseSystemPrompt));
    }

    public async Task<string> ProcessAsync(string text, string taskPrompt, string model, string baseUrl, string apiKey, CancellationToken ct = default)
    {
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl)
        });

        var chatClient = client.GetChatClient(model);

        var combinedSystemPrompt = string.IsNullOrWhiteSpace(taskPrompt)
            ? _baseSystemPrompt
            : _baseSystemPrompt + "\n\n" + taskPrompt;

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(combinedSystemPrompt),
            new UserChatMessage($"Here is the transcription:\n\n{text}")
        };

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);

        var content = response.Value.Content[0].Text;
        return content?.Trim() ?? string.Empty;
    }
}
