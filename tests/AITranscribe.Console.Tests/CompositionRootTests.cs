using AITranscribe.Console.Tui;
using AITranscribe.Core.Api;
using AITranscribe.Core.Audio;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AITranscribe.Console.Tests;

public class CompositionRootTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IServiceProvider _provider;

    public CompositionRootTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aitranscribe_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var configPath = Path.Combine(_tempDir, "config.json");
        var configManager = new ConfigManager(configPath);
        var config = configManager.Load() with
        {
            Groq = configManager.Load().Groq with { ApiKey = "test-groq-key" }
        };
        var dbPath = Path.Combine(_tempDir, "prompts.sqlite");

        _provider = CompositionRoot.Build(configManager, config, dbPath);
    }

    [Fact]
    public void Build_ReturnsNonNullServiceProvider()
    {
        _provider.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesISttClient_AsGroqSttClient()
    {
        var client = _provider.GetService<ISttClient>();
        client.Should().NotBeNull();
        client.Should().BeOfType<GroqSttClient>();
    }

    [Fact]
    public void Build_ResolvesILlmClient_AsLlmClient()
    {
        var client = _provider.GetService<ILlmClient>();
        client.Should().NotBeNull();
        client.Should().BeOfType<LlmClient>();
    }

    [Fact]
    public void Build_ResolvesIPromptManager_AsPromptManager()
    {
        var manager = _provider.GetService<IPromptManager>();
        manager.Should().NotBeNull();
        manager.Should().BeOfType<PromptManager>();
    }

    [Fact]
    public void Build_ResolvesTranscriptionService()
    {
        var service = _provider.GetService<TranscriptionService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesAudioRecorder()
    {
        var recorder = _provider.GetService<AudioRecorder>();
        recorder.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesAITranscribeTui()
    {
        var tui = _provider.GetService<AITranscribeTui>();
        tui.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesRecordingController()
    {
        var controller = _provider.GetService<RecordingController>();
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesHistoryManager()
    {
        var manager = _provider.GetService<HistoryManager>();
        manager.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesAppConfig()
    {
        var config = _provider.GetService<AppConfig>();
        config.Should().NotBeNull();
    }

    [Fact]
    public void Build_ResolvesConfigManager()
    {
        var manager = _provider.GetService<ConfigManager>();
        manager.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithEmptyApiKey_DoesNotThrow()
    {
        var configPath = Path.Combine(_tempDir, "empty_config.json");
        var configManager = new ConfigManager(configPath);
        var config = AppConfig.CreateDefault();
        var dbPath = Path.Combine(_tempDir, "empty.sqlite");

        Action act = () => CompositionRoot.Build(configManager, config, dbPath);
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }
}
