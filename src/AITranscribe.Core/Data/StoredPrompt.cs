namespace AITranscribe.Core.Data;

public record StoredPrompt(
    long Id,
    string Prompt,
    string Filename,
    DateTime CreatedAt,
    string? Summary
);
