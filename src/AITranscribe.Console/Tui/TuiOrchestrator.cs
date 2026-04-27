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
    public static void WireTui(AITranscribeTui tui, RecordingController controller, HistoryManager historyManager, AppConfig config, ConfigManager? configManager, IServiceProvider services, IApplication app)
    {
        PopulateConfigFields(tui, config);

        if (configManager is not null)
        {
            tui.OnConfigChanged = newConfig => configManager.Save(newConfig);
            tui.WireConfigPersistence(config);
        }

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

        tui.OnTranslateRequested = language =>
        {
            var text = tui.TranscriptView.Text.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                tui.FlashLabel.Text = "No text to translate.";
                return;
            }

            var (llmModel, llmBaseUrl, llmApiKey) = ResolveLlmSettings(tui, config);
            if (string.IsNullOrWhiteSpace(llmModel))
            {
                tui.FlashLabel.Text = "No LLM model configured.";
                return;
            }

            tui.FlashLabel.Text = $"Translating to {language}...";
            _ = Task.Run(async () =>
            {
                try
                {
                    var transcriptionService = services.GetRequiredService<TranscriptionService>();
                    var translated = await transcriptionService.TranslateAsync(text, language, llmModel, llmBaseUrl, llmApiKey);
                    controller.InvokeOnMainThread?.Invoke(() =>
                    {
                        tui.TranscriptView.Text = translated;
                        tui.FlashLabel.Text = $"Translated to {language}.";
                    });
                }
                catch (Exception ex)
                {
                    controller.InvokeOnMainThread?.Invoke(() =>
                    {
                        tui.FlashLabel.Text = $"Translation failed: {ex.Message}";
                    });
                }
            });
        };

        tui.OnWriteIssueRequested = () =>
        {
            var text = tui.TranscriptView.Text.ToString() ?? "";
            var selectedId = historyManager.SelectedHistoryId;
            string? summary = null;
            if (selectedId.HasValue)
            {
                foreach (var prompt in historyManager.Prompts)
                {
                    if (prompt.Id == selectedId.Value)
                    {
                        summary = prompt.Summary;
                        break;
                    }
                }
            }

            var issuePath = Path.Combine(Path.GetTempPath(), "issue.md");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine(!string.IsNullOrWhiteSpace(summary) ? summary : "No summary available.");
            sb.AppendLine();
            sb.AppendLine("## Full Transcript");
            sb.AppendLine();
            sb.AppendLine(text);
            sb.AppendLine();

            try
            {
                File.WriteAllText(issuePath, sb.ToString());
                tui.FlashLabel.Text = $"Issue written to {issuePath}.";
            }
            catch (Exception ex)
            {
                tui.FlashLabel.Text = $"Failed to write issue: {ex.Message}";
            }
        };

        tui.OnDeleteRequested = () =>
        {
            if (!tui.HistoryList.HasFocus)
            {
                tui.FlashLabel.Text = "Press Delete in history list to delete.";
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await historyManager.DeleteSelectedAsync();
                    controller.InvokeOnMainThread?.Invoke(() =>
                    {
                        tui.FlashLabel.Text = result ? "Deleted selected transcript." : "Nothing to delete.";
                    });
                }
                catch (Exception ex)
                {
                    controller.InvokeOnMainThread?.Invoke(() =>
                    {
                        tui.FlashLabel.Text = $"Delete failed: {ex.Message}";
                    });
                }
            });
        };

        tui.HistoryList.Activated += (_, args) =>
        {
            if (tui.HistoryList.SelectedItem is int idx)
                historyManager.SelectHistoryItem(idx);
        };

        tui.StartClock(app);
        tui.TranscriptView.SetFocus();

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
