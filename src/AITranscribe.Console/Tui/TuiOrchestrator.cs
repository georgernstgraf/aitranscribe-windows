using AITranscribe.Core.Api;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace AITranscribe.Console.Tui;

public static class TuiOrchestrator
{
    public static void WireTui(AITranscribeTui tui, RecordingController controller, HistoryManager historyManager, AppConfig config, IServiceProvider services, IApplication app)
    {
        PopulateConfigFields(tui, config);

        controller.InvokeOnMainThread = action => app.Invoke(action);

        controller.OnStateChanged = state => tui.SetState(state);

        controller.OnFeedback = (stepId, status) =>
        {
            tui.SetFeedbackStep(stepId, status);
        };

        controller.OnTranscriptUpdate = text =>
        {
            tui.TranscriptView.Text = text;
        };

        controller.OnProcessingComplete = result =>
        {
            tui.SetFeedbackStep("summary", "active");

            _ = Task.Run(async () =>
            {
                try
                {
                    var (llmModel, llmBaseUrl, llmApiKey) = ResolveLlmSettings(tui, config);
                    var transcriptionService = services.GetRequiredService<TranscriptionService>();
                    if (result.Id > 0)
                    {
                        await transcriptionService.GenerateSummaryAsync(result.Id, llmModel, llmBaseUrl, llmApiKey);
                    }
                    controller.InvokeOnMainThread?.Invoke(() =>
                    {
                        tui.SetFeedbackStep("summary", "done");
                        tui.FlashLabel.Text = "Processing complete.";
                    });
                    await historyManager.RefreshHistoryAsync();
                }
                catch
                {
                    controller.InvokeOnMainThread?.Invoke(() =>
                    {
                        tui.SetFeedbackStep("summary", "failed");
                        tui.FlashLabel.Text = "Summary generation failed.";
                    });
                }
            });
        };

        controller.OnProcessingFailed = error =>
        {
            tui.FlashLabel.Text = $"Error: {error}";
        };

        controller.SettingsProvider = () => BuildSettings(tui, config, historyManager);

        tui.OnToggleRecordingRequested = () =>
        {
            tui.ResetFeedbackSteps();
            controller.ToggleRecording();
        };

        tui.OnAppendRecordingRequested = () =>
        {
            tui.ResetFeedbackSteps();
            historyManager.IsAppendMode = true;
            controller.ToggleRecording();
        };

        tui.OnResized = () =>
        {
            _ = Task.Run(async () =>
            {
                try { await historyManager.RefreshHistoryAsync(); } catch { }
            });
        };

        tui.OnSaveTranscriptRequested = (text, _) => historyManager.SaveTranscriptAsync(text, "");

        tui.HistoryList.Activated += (_, args) =>
        {
            if (tui.HistoryList.SelectedItem is int idx)
                historyManager.SelectHistoryItem(idx);
        };

        tui.StartClock(app);

        _ = Task.Run(async () =>
        {
            try { await historyManager.RefreshHistoryAsync(); } catch { }
        });
    }

    internal static TranscriptionSettings BuildSettings(AITranscribeTui tui, AppConfig config, HistoryManager historyManager)
    {
        var preProcessMode = (tui.PreprocessRadioGroup.Value ?? 2) switch
        {
            0 => PreProcessMode.Raw,
            1 => PreProcessMode.Cleanup,
            _ => PreProcessMode.English,
        };

        var (llmModel, llmBaseUrl, llmApiKey) = ResolveLlmSettings(tui, config);
        var sttModel = tui.SttModelField.Text.ToString() ?? config.Groq.SttModel;

        return new TranscriptionSettings(
            preProcessMode,
            sttModel,
            llmModel,
            llmBaseUrl,
            llmApiKey,
            historyManager.IsAppendMode,
            historyManager.SelectedHistoryText,
            historyManager.SelectedHistoryId,
            config.Verbose
        );
    }

    internal static (string Model, string BaseUrl, string ApiKey) ResolveLlmSettings(AITranscribeTui tui, AppConfig config)
    {
        var llmModelOverride = tui.LlmModelField.Text.ToString();
        if (!string.IsNullOrWhiteSpace(llmModelOverride))
        {
            var provider = config.Llm.Provider.ToLowerInvariant();
            var (baseUrl, _) = LlmProviders.Providers.TryGetValue(provider, out var info)
                ? (info.BaseUrl, info.DefaultModel)
                : ("", "");
            var apiKey = provider switch
            {
                "openrouter" => config.OpenRouter.ApiKey,
                "cohere" => config.Cohere.ApiKey,
                "z.ai" => config.ZAi.ApiKey,
                _ => ""
            };
            return (llmModelOverride, baseUrl, apiKey);
        }

        return ResolveLlmFromConfig(config);
    }

    internal static (string Model, string BaseUrl, string ApiKey) ResolveLlmFromConfig(AppConfig config)
    {
        var provider = config.Llm.Provider.ToLowerInvariant();
        var (baseUrl, defaultModel) = LlmProviders.Providers.TryGetValue(provider, out var info)
            ? (info.BaseUrl, info.DefaultModel)
            : ("", "");

        var (model, apiKey) = provider switch
        {
            "openrouter" => (config.OpenRouter.Model, config.OpenRouter.ApiKey),
            "cohere" => (config.Cohere.Model, config.Cohere.ApiKey),
            "z.ai" => (config.ZAi.Model, config.ZAi.ApiKey),
            _ => (defaultModel, "")
        };

        return (model, baseUrl, apiKey);
    }

    private static void PopulateConfigFields(AITranscribeTui tui, AppConfig config)
    {
        tui.SttModelField.Text = config.Groq.SttModel;
        tui.LlmModelField.Text = config.Llm.Model;
        tui.FilePathField.Text = config.LastFilePath;
        tui.SourceRadioGroup.Value = config.InputSource == "file" ? 1 : 0;
        tui.PreprocessRadioGroup.Value = (int)config.PreProcessMode;
    }
}
