using System.Text.Json.Serialization;

namespace AITranscribe.Core.Models;

public record LlmProvider(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("base_url")] string BaseUrl,
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("default_model")] string DefaultModel,
    [property: JsonPropertyName("env_key")] string EnvKey,
    [property: JsonPropertyName("env_model")] string EnvModel
);
