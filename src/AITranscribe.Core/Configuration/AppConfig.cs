using System.Text.Json.Serialization;
using AITranscribe.Core.Models;

namespace AITranscribe.Core.Configuration;

public record GroqConfig(
    [property: JsonPropertyName("ApiKey")] string ApiKey,
    [property: JsonPropertyName("SttModel")] string SttModel
);

public record LlmConfig(
    [property: JsonPropertyName("Provider")] string Provider,
    [property: JsonPropertyName("Model")] string Model
);

public record OpenRouterConfig(
    [property: JsonPropertyName("ApiKey")] string ApiKey,
    [property: JsonPropertyName("Model")] string Model
);

public record CohereConfig(
    [property: JsonPropertyName("ApiKey")] string ApiKey,
    [property: JsonPropertyName("Model")] string Model
);

public record ZAiConfig(
    [property: JsonPropertyName("ApiKey")] string ApiKey,
    [property: JsonPropertyName("Model")] string Model
);

public record AppConfig(
    [property: JsonPropertyName("Groq")] GroqConfig Groq,
    [property: JsonPropertyName("Llm")] LlmConfig Llm,
    [property: JsonPropertyName("PreProcessMode")] PreProcessMode PreProcessMode,
    [property: JsonPropertyName("InputSource")] string InputSource,
    [property: JsonPropertyName("LastFilePath")] string LastFilePath,
    [property: JsonPropertyName("Verbose")] bool Verbose,
    [property: JsonPropertyName("OpenRouter")] OpenRouterConfig OpenRouter,
    [property: JsonPropertyName("Cohere")] CohereConfig Cohere,
    [property: JsonPropertyName("ZAi")] ZAiConfig ZAi
)
{
    public static AppConfig CreateDefault() => new(
        new GroqConfig("", "whisper-large-v3-turbo"),
        new LlmConfig("openrouter", "anthropic/claude-3-haiku"),
        PreProcessMode.English,
        "microphone",
        "",
        false,
        new OpenRouterConfig("", "anthropic/claude-3-haiku"),
        new CohereConfig("", "command-r"),
        new ZAiConfig("", "glm-5")
    );
}
