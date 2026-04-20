using AITranscribe.Core.Api;
using AITranscribe.Core.Audio;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.DependencyInjection;

namespace AITranscribe.Console.Commands;

public class TranscribeCommand : Command<TranscribeSettings>
{
    public static IServiceProvider? Services { get; set; }

    public override int Execute(CommandContext context, TranscribeSettings settings)
    {
        if (!settings.IsLegacyMode())
        {
            AnsiConsole.MarkupLine("[yellow]TUI not yet implemented.[/]");
            return 0;
        }

        if (settings.ListPrompts)
            return ExecuteList();

        if (settings.RemovePrompt is not null)
            return ExecuteRemove(settings.RemovePrompt.Value);

        if (settings.QueryPrompt)
            return ExecuteQuery();

        if (settings.File is not null)
            return ExecuteFile(settings).GetAwaiter().GetResult();

        return ExecuteMic(settings).GetAwaiter().GetResult();
    }

    private int ExecuteList()
    {
        var promptManager = Services!.GetRequiredService<IPromptManager>();
        var prompts = promptManager.GetAllAsync().GetAwaiter().GetResult();

        if (prompts.Count == 0)
        {
            AnsiConsole.WriteLine("No prompts stored yet.");
            return 0;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Summary");
        table.AddColumn("CreatedAt");

        foreach (var p in prompts)
        {
            var summary = p.Summary ?? Truncate(p.Prompt, 40);
            table.AddRow(p.Id.ToString(), summary, p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private int ExecuteQuery()
    {
        var promptManager = Services!.GetRequiredService<IPromptManager>();
        var prompts = promptManager.GetAllAsync().GetAwaiter().GetResult();

        if (prompts.Count == 0)
        {
            AnsiConsole.WriteLine("No prompts in queue.");
            return 0;
        }

        var oldest = prompts[^1];
        AnsiConsole.WriteLine(oldest.Prompt);
        promptManager.RemoveByIdAsync(oldest.Id).GetAwaiter().GetResult();
        return 0;
    }

    private int ExecuteRemove(int index)
    {
        var promptManager = Services!.GetRequiredService<IPromptManager>();
        var prompts = promptManager.GetAllAsync().GetAwaiter().GetResult();

        if (index < 1 || index > prompts.Count)
        {
            AnsiConsole.MarkupLine($"[red]Invalid index {index}. Valid range: 1-{prompts.Count}[/]");
            return 1;
        }

        var target = prompts[index - 1];
        var preview = Truncate(target.Prompt, 50);
        promptManager.RemoveByIdAsync(target.Id).GetAwaiter().GetResult();
        AnsiConsole.WriteLine($"Removed prompt {index}: {preview}");
        return 0;
    }

    private async Task<int> ExecuteFile(TranscribeSettings settings)
    {
        var config = Services!.GetRequiredService<AppConfig>();

        if (string.IsNullOrWhiteSpace(config.Groq.ApiKey))
        {
            AnsiConsole.MarkupLine("[red]Error: No Groq API key configured. Set it via config or GROQ_API_KEY environment variable.[/]");
            return 1;
        }

        var preProcessMode = ResolvePreProcessMode(settings, config);
        var llmConfig = GetLlmConfig(config, settings);

        if (preProcessMode != PreProcessMode.Raw && string.IsNullOrWhiteSpace(llmConfig.ApiKey))
        {
            AnsiConsole.MarkupLine($"[red]Error: No LLM API key configured for provider '{config.Llm.Provider}'. Set it via config or environment variable.[/]");
            return 1;
        }

        var transcriptionSettings = new TranscriptionSettings(
            preProcessMode,
            settings.SttModel,
            llmConfig.Model,
            llmConfig.BaseUrl,
            llmConfig.ApiKey,
            false, string.Empty, null, settings.Verbose);

        var transcriptionService = Services.GetRequiredService<TranscriptionService>();

        try
        {
            Transcription result;
            if (settings.Verbose)
            {
                result = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Processing file...", async ctx =>
                    {
                        ctx.Status("Transcribing audio...");
                        var r = await transcriptionService.ProcessFileAsync(
                            settings.File!,
                            transcriptionSettings,
                            (step, status) =>
                            {
                                if (status == "active") ctx.Status($"{step}...");
                            },
                            text => ctx.Status($"Got transcript: {Truncate(text, 60)}"),
                            CancellationToken.None);
                        return r;
                    });
            }
            else
            {
                result = await transcriptionService.ProcessFileAsync(
                    settings.File!,
                    transcriptionSettings,
                    null, null, CancellationToken.None);
            }

            AnsiConsole.WriteLine(result.Text);
            return 0;
        }
        catch (Exception ex)
        {
            if (settings.Verbose)
                AnsiConsole.WriteException(ex);
            else
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<int> ExecuteMic(TranscribeSettings settings)
    {
        var config = Services!.GetRequiredService<AppConfig>();

        if (string.IsNullOrWhiteSpace(config.Groq.ApiKey))
        {
            AnsiConsole.MarkupLine("[red]Error: No Groq API key configured. Set it via config or GROQ_API_KEY environment variable.[/]");
            return 1;
        }

        var preProcessMode = ResolvePreProcessMode(settings, config);
        var llmConfig = GetLlmConfig(config, settings);

        if (preProcessMode != PreProcessMode.Raw && string.IsNullOrWhiteSpace(llmConfig.ApiKey))
        {
            AnsiConsole.MarkupLine($"[red]Error: No LLM API key configured for provider '{config.Llm.Provider}'. Set it via config or environment variable.[/]");
            return 1;
        }

        var transcriptionSettings = new TranscriptionSettings(
            preProcessMode,
            settings.SttModel,
            llmConfig.Model,
            llmConfig.BaseUrl,
            llmConfig.ApiKey,
            false, string.Empty, null, settings.Verbose);

        var recorder = Services.GetRequiredService<AudioRecorder>();

        if (settings.Verbose)
            AnsiConsole.WriteLine("Starting recording...");

        recorder.Start();
        AnsiConsole.WriteLine("Recording... Press any key to stop.");
        System.Console.ReadKey(true);

        if (settings.Verbose)
            AnsiConsole.WriteLine("Stopping recording...");

        var audio = await recorder.StopAsync(CancellationToken.None);

        if (settings.Verbose)
            AnsiConsole.WriteLine($"Captured {audio.Length} bytes of audio data.");

        var transcriptionService = Services.GetRequiredService<TranscriptionService>();

        try
        {
            Transcription result;
            if (settings.Verbose)
            {
                result = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Processing recording...", async ctx =>
                    {
                        ctx.Status("Transcribing audio...");
                        var r = await transcriptionService.ProcessMicAudioAsync(
                            audio,
                            transcriptionSettings,
                            (step, status) =>
                            {
                                if (status == "active") ctx.Status($"{step}...");
                            },
                            text => ctx.Status($"Got transcript: {Truncate(text, 60)}"),
                            CancellationToken.None);
                        return r;
                    });
            }
            else
            {
                result = await transcriptionService.ProcessMicAudioAsync(
                    audio,
                    transcriptionSettings,
                    null, null, CancellationToken.None);
            }

            AnsiConsole.WriteLine(result.Text);
            return 0;
        }
        catch (Exception ex)
        {
            if (settings.Verbose)
                AnsiConsole.WriteException(ex);
            else
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    internal static PreProcessMode ResolvePreProcessMode(TranscribeSettings settings, AppConfig config)
    {
        if (settings.English) return PreProcessMode.English;
        if (settings.PostProcess) return PreProcessMode.Cleanup;
        return config.PreProcessMode;
    }

    internal static (string BaseUrl, string Model, string ApiKey) GetLlmConfig(AppConfig config, TranscribeSettings settings)
    {
        var provider = config.Llm.Provider;
        LlmProviders.Providers.TryGetValue(provider, out var providerInfo);
        var apiKey = provider switch
        {
            "openrouter" => config.OpenRouter.ApiKey,
            "cohere" => config.Cohere.ApiKey,
            "z.ai" => config.ZAi.ApiKey,
            _ => ""
        };
        var baseUrl = providerInfo?.BaseUrl ?? "";
        var model = settings.LlmModel ?? config.Llm.Model;
        return (baseUrl, model, apiKey);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}
