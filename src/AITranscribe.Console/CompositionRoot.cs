using AITranscribe.Core.Api;
using AITranscribe.Core.Audio;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Services;
using AITranscribe.Console.Tui;
using Microsoft.Extensions.DependencyInjection;

namespace AITranscribe.Console;

public static class CompositionRoot
{
    public static IServiceProvider Build()
    {
        var configManager = ConfigManager.CreateDefault();
        var config = configManager.Load();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = Path.Combine(appData, "aitranscribe", "prompts.sqlite");

        return Build(configManager, config, dbPath);
    }

    public static IServiceProvider Build(ConfigManager configManager, AppConfig config, string dbPath)
    {
        var services = new ServiceCollection();

        services.AddSingleton(configManager);
        services.AddSingleton(config);

        services.AddSingleton<ISttClient>(sp => new GroqSttClient(config.Groq.ApiKey));
        services.AddSingleton<ILlmClient>(sp => new LlmClient(config.Prompts.SystemPrompt));
        services.AddSingleton<IPromptManager>(sp => new PromptManager(dbPath));
        services.AddSingleton(sp => new TranscriptionService(
            sp.GetRequiredService<ISttClient>(),
            sp.GetRequiredService<ILlmClient>(),
            sp.GetRequiredService<IPromptManager>(),
            config.Prompts));
        services.AddSingleton<IAudioCapture, WasapiAudioCapture>();
        services.AddSingleton<AudioRecorder>();
        services.AddSingleton<AITranscribeTui>();
        services.AddSingleton<HistoryManager>();
        services.AddSingleton<RecordingController>();

        return services.BuildServiceProvider();
    }
}
