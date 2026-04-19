namespace AITranscribe.Core.Api;

public record LlmProviderInfo(string BaseUrl, string DefaultModel);

public static class LlmProviders
{
    public static readonly Dictionary<string, LlmProviderInfo> Providers = new()
    {
        ["openrouter"] = new("https://openrouter.ai/api/v1", "anthropic/claude-3-haiku"),
        ["cohere"] = new("https://api.cohere.ai/compatibility/v1", "command-r"),
        ["z.ai"] = new("https://api.z.ai/api/coding/paas/v4", "glm-5"),
    };
}
