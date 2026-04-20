using AITranscribe.Console.Commands;
using AITranscribe.Console.Tui;
using AITranscribe.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Terminal.Gui;

namespace AITranscribe.Console;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return RunTui();
        }

        var app = new CommandApp<TranscribeCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("aitranscribe");
            config.ValidateExamples();
        });

        return app.Run(args);
    }

    private static int RunTui()
    {
        var services = CompositionRoot.Build();
        var tui = services.GetRequiredService<AITranscribeTui>();
        var controller = services.GetRequiredService<RecordingController>();
        var historyManager = services.GetRequiredService<HistoryManager>();
        var config = services.GetRequiredService<AppConfig>();

        TuiOrchestrator.WireTui(tui, controller, historyManager, config, services);

        Application.Init();
        Application.Run(tui);
        Application.Shutdown();
        return 0;
    }
}
