using Spectre.Console;
using Spectre.Console.Cli;

namespace AITranscribe.Console.Commands;

public class TranscribeSettings : CommandSettings
{
    [CommandOption("--file|-f")]
    public string? File { get; set; }

    [CommandOption("--list|-l")]
    public bool ListPrompts { get; set; }

    [CommandOption("--query|-q")]
    public bool QueryPrompt { get; set; }

    [CommandOption("--remove|-r")]
    public int? RemovePrompt { get; set; }

    [CommandOption("--english|-e")]
    public bool English { get; set; }

    [CommandOption("--llm-model|-m")]
    public string LlmModel { get; set; } = "anthropic/claude-3-haiku";

    [CommandOption("--post-process|-p")]
    public bool PostProcess { get; set; }

    [CommandOption("--stt-model")]
    public string SttModel { get; set; } = "whisper-large-v3-turbo";

    [CommandOption("--verbose|-v")]
    public bool Verbose { get; set; }

    public override ValidationResult Validate()
    {
        if (English && PostProcess)
            return ValidationResult.Error("Options --english and --post-process are mutually exclusive.");

        return base.Validate();
    }

    public bool IsLegacyMode()
    {
        return File is not null
            || ListPrompts
            || QueryPrompt
            || RemovePrompt is not null
            || English
            || PostProcess
            || Verbose
            || SttModel != "whisper-large-v3-turbo"
            || LlmModel != "anthropic/claude-3-haiku";
    }
}
