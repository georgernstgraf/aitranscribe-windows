using AITranscribe.Console.Commands;
using AITranscribe.Core.Api;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spectre.Console.Cli;

namespace AITranscribe.Console.Tests.Integration;

internal sealed class FakeRemainingArguments2 : IRemainingArguments
{
    public ILookup<string, string?> Parsed => Array.Empty<KeyValuePair<string, string?>>()
        .ToLookup(k => k.Key, k => k.Value);
    public IReadOnlyList<string> Raw => [];
}

public class EndToEndTests : IDisposable
{
    private readonly string _tempDir;

    public EndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ait_e2e_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        TranscribeCommand.Services = null;
    }

    public void Dispose()
    {
        TranscribeCommand.Services = null;
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    [Fact]
    public void CompositionRoot_CreatesWorkingTranscriptionService()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        var configManager = new ConfigManager(configPath);
        var config = configManager.Load() with
        {
            Groq = configManager.Load().Groq with { ApiKey = "test-key" }
        };
        var dbPath = Path.Combine(_tempDir, "prompts.sqlite");

        var provider = CompositionRoot.Build(configManager, config, dbPath);

        var svc = provider.GetService<TranscriptionService>();
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task FullPipeline_WithMocks_StoresTranscription()
    {
        var dbPath = Path.Combine(_tempDir, "prompts.sqlite");
        var promptManager = new PromptManager(dbPath);
        await promptManager.InitializeAsync();

        var mockStt = new Mock<ISttClient>();
        mockStt.Setup(x => x.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), default))
            .ReturnsAsync("Hello world");
        var mockLlm = new Mock<ILlmClient>();
        mockLlm.Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync("Hello world processed");

        var svc = new TranscriptionService(mockStt.Object, mockLlm.Object, promptManager, PromptsConfig.CreateDefault());

        var wavPath = CreateMinimalWavFile(_tempDir);
        var settings = new TranscriptionSettings(
            PreProcessMode.English,
            "whisper-large-v3-turbo",
            "anthropic/claude-3-haiku",
            "https://openrouter.ai/api/v1",
            "test-key",
            false, string.Empty, null, false);

        var result = await svc.ProcessFileAsync(wavPath, settings, null, null);

        result.Text.Should().Be("Hello world processed");
        result.Id.Should().BeGreaterThan(0);

        var stored = await promptManager.GetByIdAsync(result.Id);
        stored.Should().NotBeNull();
        stored!.Prompt.Should().Be("Hello world processed");
    }

    [Fact]
    public async Task ListPrompts_WithPopulatedDB_ReturnsResults()
    {
        var dbPath = Path.Combine(_tempDir, "prompts.sqlite");
        var promptManager = new PromptManager(dbPath);
        await promptManager.InitializeAsync();
        await promptManager.AddAsync("Test prompt 1", "file1.wav", null);
        await promptManager.AddAsync("Test prompt 2", "file2.wav", "Summary 2");

        var mockStt = new Mock<ISttClient>();
        var mockLlm = new Mock<ILlmClient>();

        var services = new ServiceCollection();
        services.AddSingleton<IPromptManager>(promptManager);
        services.AddSingleton(mockStt.Object);
        services.AddSingleton(mockLlm.Object);
        services.AddSingleton(AppConfig.CreateDefault());
        services.AddSingleton(PromptsConfig.CreateDefault());
        services.AddSingleton<TranscriptionService>();
        using var sp = services.BuildServiceProvider();
        TranscribeCommand.Services = sp;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { ListPrompts = true };

        var result = cmd.Execute(new CommandContext([], new FakeRemainingArguments2(), "", null), settings);

        result.Should().Be(0);
    }

    [Fact]
    public async Task FileTranscription_WithMocks_ProducesStoredTranscription()
    {
        var dbPath = Path.Combine(_tempDir, "prompts.sqlite");
        var promptManager = new PromptManager(dbPath);
        await promptManager.InitializeAsync();

        var mockStt = new Mock<ISttClient>();
        mockStt.Setup(x => x.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), default))
            .ReturnsAsync("Transcribed text");
        var mockLlm = new Mock<ILlmClient>();
        mockLlm.Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync("Processed text");

        var config = AppConfig.CreateDefault() with
        {
            Groq = new GroqConfig("groq-key", "whisper-large-v3-turbo"),
            OpenRouter = new OpenRouterConfig("llm-key", "anthropic/claude-3-haiku")
        };

        var services = new ServiceCollection();
        services.AddSingleton<IPromptManager>(promptManager);
        services.AddSingleton(mockStt.Object);
        services.AddSingleton(mockLlm.Object);
        services.AddSingleton(config);
        services.AddSingleton(PromptsConfig.CreateDefault());
        services.AddSingleton<TranscriptionService>();
        using var sp = services.BuildServiceProvider();
        TranscribeCommand.Services = sp;

        var wavPath = CreateMinimalWavFile(_tempDir);
        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { File = wavPath, Verbose = false };

        var result = cmd.Execute(new CommandContext([], new FakeRemainingArguments2(), "", null), settings);

        result.Should().Be(0);
        var all = await promptManager.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Prompt.Should().Be("Processed text");
    }

    private static string CreateMinimalWavFile(string dir)
    {
        var path = Path.Combine(dir, "test.wav");
        var dataSize = 1000;
        var header = new byte[44];
        header[0] = 0x52; header[1] = 0x49; header[2] = 0x46; header[3] = 0x46;
        BitConverter.TryWriteBytes(header.AsSpan(4), 36 + dataSize);
        header[8] = 0x57; header[9] = 0x41; header[10] = 0x56; header[11] = 0x45;
        header[12] = 0x66; header[13] = 0x6D; header[14] = 0x74; header[15] = 0x20;
        BitConverter.TryWriteBytes(header.AsSpan(16), 16);
        BitConverter.TryWriteBytes(header.AsSpan(20), (short)1);
        BitConverter.TryWriteBytes(header.AsSpan(22), (short)1);
        BitConverter.TryWriteBytes(header.AsSpan(24), 44100);
        BitConverter.TryWriteBytes(header.AsSpan(28), 88200);
        BitConverter.TryWriteBytes(header.AsSpan(32), (short)2);
        BitConverter.TryWriteBytes(header.AsSpan(34), (short)16);
        header[36] = 0x64; header[37] = 0x61; header[38] = 0x74; header[39] = 0x61;
        BitConverter.TryWriteBytes(header.AsSpan(40), dataSize);
        var data = new byte[dataSize];
        File.WriteAllBytes(path, header.Concat(data).ToArray());
        return path;
    }
}
