using AITranscribe.Console.Commands;
using Spectre.Console.Cli;

namespace AITranscribe.Console;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new CommandApp<TranscribeCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("aitranscribe");
            config.ValidateExamples();
        });

        if (args.Length == 0)
            args = ["--help"];

        return app.Run(args);
    }
}
