using Spectre.Console;
using Spectre.Console.Cli;

namespace AITranscribe.Console.Commands;

public class TranscribeCommand : Command<TranscribeSettings>
{
    public override int Execute(CommandContext context, TranscribeSettings settings)
    {
        if (!settings.IsLegacyMode())
        {
            AnsiConsole.MarkupLine("[yellow]TUI not yet implemented.[/]");
            return 0;
        }

        if (settings.ListPrompts)
        {
            AnsiConsole.MarkupLine("[yellow]Prompt listing not yet implemented.[/]");
            return 0;
        }

        if (settings.RemovePrompt is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]Remove prompt {settings.RemovePrompt} not yet implemented.[/]");
            return 0;
        }

        if (settings.QueryPrompt)
        {
            AnsiConsole.MarkupLine("[yellow]Query prompt not yet implemented.[/]");
            return 0;
        }

        if (settings.File is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]File transcription not yet implemented: {settings.File}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[yellow]Microphone recording not yet implemented.[/]");
        return 0;
    }
}
