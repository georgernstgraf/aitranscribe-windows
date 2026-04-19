using System.Text.Json.Serialization;

namespace AITranscribe.Core.Models;

public record Transcription(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("summary")] string? Summary
);
