using AITranscribe.Core.Models;

namespace AITranscribe.Core.Services;

public record TranscriptionSettings(
    PreProcessMode PreProcessMode,
    string SttModel,
    string LlmModel,
    string LlmBaseUrl,
    string LlmApiKey,
    bool AppendMode,
    string AppendBaseText,
    long? AppendTargetId,
    bool Verbose
);
