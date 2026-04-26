using AITranscribe.Console.Tui;
using AITranscribe.Core.Api;
using AITranscribe.Core.Audio;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AITranscribe.Console.Tests.Tui;

public class TuiWiringTests
{
    private readonly AITranscribeTui _tui;
    private readonly RecordingController _controller;
    private readonly HistoryManager _historyManager;
    private readonly AppConfig _config;
    private readonly Mock<IPromptManager> _promptManagerMock;

    public TuiWiringTests()
    {
        _tui = new AITranscribeTui();

        _promptManagerMock = new Mock<IPromptManager>();
        _historyManager = new HistoryManager(_promptManagerMock.Object, _tui);

        _config = AppConfig.CreateDefault() with
        {
            Groq = new GroqConfig("test-key", "whisper-large-v3-turbo"),
            OpenRouter = new OpenRouterConfig("test-or-key", "anthropic/claude-3-haiku"),
        };

        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.Setup(c => c.GetCapturedData()).Returns(Array.Empty<byte>());
        var recorder = new AudioRecorder(mockCapture.Object);

        var mockStt = new Mock<ISttClient>();
        var mockLlm = new Mock<ILlmClient>();
        var service = new TranscriptionService(mockStt.Object, mockLlm.Object, _promptManagerMock.Object, PromptsConfig.CreateDefault());

        _controller = new RecordingController(recorder, service);
    }

    private IServiceProvider BuildMockProvider()
    {
        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.Setup(c => c.GetCapturedData()).Returns(Array.Empty<byte>());
        var recorder = new AudioRecorder(mockCapture.Object);

        var mockStt = new Mock<ISttClient>();
        var mockLlm = new Mock<ILlmClient>();
        var mockPrompt = new Mock<IPromptManager>();
        var service = new TranscriptionService(mockStt.Object, mockLlm.Object, mockPrompt.Object, PromptsConfig.CreateDefault());

        var services = new ServiceCollection();
        services.AddSingleton<TranscriptionService>(service);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void WireTui_DoesNotThrow()
    {
        var provider = BuildMockProvider();

        var act = () => TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        act.Should().NotThrow();
    }

    [Fact]
    public void WireTui_TuiToggleRecording_ForwardsToController()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _tui.ToggleRecording();

        _controller.State.Should().Be(TuiState.Recording);
    }

    [Fact]
    public void WireTui_ControllerStateChanged_UpdatesTuiState()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _controller.OnStateChanged?.Invoke(TuiState.Recording);

        _tui.CurrentState.Should().Be(TuiState.Recording);
    }

    [Fact]
    public void WireTui_ControllerFeedback_UpdatesTuiFeedback()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _controller.OnFeedback?.Invoke("compress", "active");

        _tui.FeedbackStepLabels[0].Text.Should().Contain("active");
    }

    [Fact]
    public void WireTui_ControllerTranscriptUpdate_UpdatesTuiTranscript()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _controller.OnTranscriptUpdate?.Invoke("Hello world");

        _tui.TranscriptView.Text.ToString().Should().Contain("Hello world");
    }

    [Fact]
    public void WireTui_ControllerProcessingFailed_UpdatesFlashLabel()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _controller.OnProcessingFailed?.Invoke("Something went wrong");

        _tui.FlashLabel.Text.Should().Contain("Something went wrong");
    }

    [Fact]
    public void WireTui_SettingsProvider_ReadsFromTuiFields()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        var settings = _controller.SettingsProvider!();

        settings.SttModel.Should().Be("whisper-large-v3-turbo");
        settings.PreProcessMode.Should().Be(PreProcessMode.English);
    }

    [Fact]
    public void WireTui_SettingsProvider_ReflectsPreprocessRadioChange()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _tui.PreprocessRadioGroup.SelectedItem = 0;
        var settings = _controller.SettingsProvider!();
        settings.PreProcessMode.Should().Be(PreProcessMode.Raw);

        _tui.PreprocessRadioGroup.SelectedItem = 1;
        settings = _controller.SettingsProvider!();
        settings.PreProcessMode.Should().Be(PreProcessMode.Cleanup);

        _tui.PreprocessRadioGroup.SelectedItem = 2;
        settings = _controller.SettingsProvider!();
        settings.PreProcessMode.Should().Be(PreProcessMode.English);
    }

    [Fact]
    public void WireTui_PopulatesConfigFields()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _tui.SttModelField.Text.ToString().Should().Be("whisper-large-v3-turbo");
        _tui.LlmModelField.Text.ToString().Should().Be("anthropic/claude-3-haiku");
    }

    [Fact]
    public void BuildSettings_WithAppendMode_ReflectsHistoryManagerState()
    {
        _historyManager.IsAppendMode = true;

        var settings = TuiOrchestrator.BuildSettings(_tui, _config, _historyManager);

        settings.AppendMode.Should().BeTrue();
    }

    [Fact]
    public void BuildSettings_WithSttModelOverride_UsesOverride()
    {
        _tui.SttModelField.Text = "custom-model";

        var settings = TuiOrchestrator.BuildSettings(_tui, _config, _historyManager);

        settings.SttModel.Should().Be("custom-model");
    }

    [Fact]
    public void ResolveLlmFromConfig_OpenRouter_ReturnsCorrectSettings()
    {
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("openrouter", "test-model"),
            OpenRouter = new OpenRouterConfig("or-key", "test-model"),
        };

        var (model, baseUrl, apiKey) = TuiOrchestrator.ResolveLlmFromConfig(config);

        model.Should().Be("test-model");
        baseUrl.Should().Be("https://openrouter.ai/api/v1");
        apiKey.Should().Be("or-key");
    }

    [Fact]
    public void ResolveLlmFromConfig_ZAi_ReturnsCorrectSettings()
    {
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("z.ai", "glm-5"),
            ZAi = new ZAiConfig("zai-key", "glm-5"),
        };

        var (model, baseUrl, apiKey) = TuiOrchestrator.ResolveLlmFromConfig(config);

        model.Should().Be("glm-5");
        baseUrl.Should().Be("https://api.z.ai/api/coding/paas/v4");
        apiKey.Should().Be("zai-key");
    }

    [Fact]
    public void ResolveLlmSettings_WithOverride_UsesOverride()
    {
        _tui.LlmModelField.Text = "custom-llm-model";
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("openrouter", "default-model"),
            OpenRouter = new OpenRouterConfig("or-key", "default-model"),
        };

        var (model, baseUrl, apiKey) = TuiOrchestrator.ResolveLlmSettings(_tui, config);

        model.Should().Be("custom-llm-model");
        baseUrl.Should().Be("https://openrouter.ai/api/v1");
        apiKey.Should().Be("or-key");
    }

    [Fact]
    public void WireTui_AppendRecording_SetsAppendModeAndStarts()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);

        _tui.AppendRecording();

        _historyManager.IsAppendMode.Should().BeTrue();
        _controller.State.Should().Be(TuiState.Recording);
    }

    [Fact]
    public async Task WireTui_OnSaveTranscriptRequested_CallsHistoryManager()
    {
        var provider = BuildMockProvider();
        TuiOrchestrator.WireTui(_tui, _controller, _historyManager, _config, provider);
        _tui.TranscriptView.Text = "some transcript";

        _promptManagerMock.Setup(pm => pm.AddAsync("some transcript", "", null, default))
            .ReturnsAsync(1L);
        _promptManagerMock.Setup(pm => pm.GetRecentAsync(null, default))
            .ReturnsAsync(new List<StoredPrompt>());

        var result = await _tui.OnSaveTranscriptRequested!("some transcript", "");

        result.Should().Be(1);
    }
}
