using AITranscribe.Console.Commands;
using AITranscribe.Console.Tui;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Terminal.Gui.App;
using Terminal.Gui.Views;

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

        TranscribeCommand.Services = CompositionRoot.Build();
        InitializeDatabase(TranscribeCommand.Services);

        return app.Run(args);
    }

    private static int RunTui()
    {
        var services = CompositionRoot.Build();
        InitializeDatabase(services);

        var tui = services.GetRequiredService<AITranscribeTui>();
        var controller = services.GetRequiredService<RecordingController>();
        var historyManager = services.GetRequiredService<HistoryManager>();
        var config = services.GetRequiredService<AppConfig>();

        IApplication app = Application.Create().Init();
        TuiOrchestrator.WireTui(tui, controller, historyManager, config, services, app);
        app.Run(tui);
        tui.Dispose();
        app.Dispose();
        return 0;
    }

    private static void InitializeDatabase(IServiceProvider services)
    {
        var promptManager = services.GetRequiredService<IPromptManager>();
        promptManager.InitializeAsync().GetAwaiter().GetResult();
    }
}
