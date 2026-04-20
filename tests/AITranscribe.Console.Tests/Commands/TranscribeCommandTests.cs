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

namespace AITranscribe.Console.Tests.Commands;

internal sealed class FakeRemainingArguments : IRemainingArguments
{
    public ILookup<string, string?> Parsed => Array.Empty<KeyValuePair<string, string?>>()
        .ToLookup(k => k.Key, k => k.Value);
    public IReadOnlyList<string> Raw => [];
}

public class TranscribeCommandTests
{
    [Fact]
    public void TranscribeSettings_HasAllOptions()
    {
        var settings = new TranscribeSettings();

        var fileProp = typeof(TranscribeSettings).GetProperty("File");
        var listProp = typeof(TranscribeSettings).GetProperty("ListPrompts");
        var queryProp = typeof(TranscribeSettings).GetProperty("QueryPrompt");
        var removeProp = typeof(TranscribeSettings).GetProperty("RemovePrompt");
        var englishProp = typeof(TranscribeSettings).GetProperty("English");
        var llmModelProp = typeof(TranscribeSettings).GetProperty("LlmModel");
        var postProcessProp = typeof(TranscribeSettings).GetProperty("PostProcess");
        var sttModelProp = typeof(TranscribeSettings).GetProperty("SttModel");
        var verboseProp = typeof(TranscribeSettings).GetProperty("Verbose");

        fileProp.Should().NotBeNull();
        listProp.Should().NotBeNull();
        queryProp.Should().NotBeNull();
        removeProp.Should().NotBeNull();
        englishProp.Should().NotBeNull();
        llmModelProp.Should().NotBeNull();
        postProcessProp.Should().NotBeNull();
        sttModelProp.Should().NotBeNull();
        verboseProp.Should().NotBeNull();
    }

    [Fact]
    public void TranscribeSettings_DefaultValues()
    {
        var settings = new TranscribeSettings();

        settings.File.Should().BeNull();
        settings.ListPrompts.Should().BeFalse();
        settings.QueryPrompt.Should().BeFalse();
        settings.RemovePrompt.Should().BeNull();
        settings.English.Should().BeFalse();
        settings.LlmModel.Should().Be("anthropic/claude-3-haiku");
        settings.PostProcess.Should().BeFalse();
        settings.SttModel.Should().Be("whisper-large-v3-turbo");
        settings.Verbose.Should().BeFalse();
    }

    [Fact]
    public void TranscribeSettings_EnglishAndPostProcess_AreMutuallyExclusive()
    {
        var settings = new TranscribeSettings { English = true, PostProcess = true };

        var result = settings.Validate();

        result.Successful.Should().BeFalse();
    }

    [Fact]
    public void TranscribeSettings_EnglishOnly_IsValid()
    {
        var settings = new TranscribeSettings { English = true };

        var result = settings.Validate();

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_PostProcessOnly_IsValid()
    {
        var settings = new TranscribeSettings { PostProcess = true };

        var result = settings.Validate();

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_NeitherEnglishNorPostProcess_IsValid()
    {
        var settings = new TranscribeSettings();

        var result = settings.Validate();

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenAllDefaults()
    {
        var settings = new TranscribeSettings();

        settings.IsLegacyMode().Should().BeFalse();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenFileSet()
    {
        var settings = new TranscribeSettings { File = "test.wav" };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenVerboseSet()
    {
        var settings = new TranscribeSettings { Verbose = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenEnglishSet()
    {
        var settings = new TranscribeSettings { English = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenListPromptsSet()
    {
        var settings = new TranscribeSettings { ListPrompts = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenQueryPromptSet()
    {
        var settings = new TranscribeSettings { QueryPrompt = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenRemovePromptSet()
    {
        var settings = new TranscribeSettings { RemovePrompt = 1 };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenPostProcessSet()
    {
        var settings = new TranscribeSettings { PostProcess = true };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenSttModelNonDefault()
    {
        var settings = new TranscribeSettings { SttModel = "other-model" };

        settings.IsLegacyMode().Should().BeTrue();
    }

    [Fact]
    public void TranscribeSettings_IsLegacyMode_WhenLlmModelNonDefault()
    {
        var settings = new TranscribeSettings { LlmModel = "other-model" };

        settings.IsLegacyMode().Should().BeTrue();
    }
}

public class TranscribeCommandHelperTests
{
    [Fact]
    public void ResolvePreProcessMode_WithEnglishFlag_ReturnsEnglish()
    {
        var settings = new TranscribeSettings { English = true };
        var config = AppConfig.CreateDefault();

        var result = TranscribeCommand.ResolvePreProcessMode(settings, config);

        result.Should().Be(PreProcessMode.English);
    }

    [Fact]
    public void ResolvePreProcessMode_WithPostProcessFlag_ReturnsCleanup()
    {
        var settings = new TranscribeSettings { PostProcess = true };
        var config = AppConfig.CreateDefault();

        var result = TranscribeCommand.ResolvePreProcessMode(settings, config);

        result.Should().Be(PreProcessMode.Cleanup);
    }

    [Fact]
    public void ResolvePreProcessMode_WithNeither_ReturnsConfigDefault()
    {
        var settings = new TranscribeSettings();
        var config = AppConfig.CreateDefault() with { PreProcessMode = PreProcessMode.Raw };

        var result = TranscribeCommand.ResolvePreProcessMode(settings, config);

        result.Should().Be(PreProcessMode.Raw);
    }

    [Fact]
    public void GetLlmConfig_ForOpenRouter_ReturnsCorrectValues()
    {
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("openrouter", "anthropic/claude-3-haiku"),
            OpenRouter = new OpenRouterConfig("sk-or-test", "anthropic/claude-3-haiku")
        };
        var settings = new TranscribeSettings();

        var (baseUrl, model, apiKey) = TranscribeCommand.GetLlmConfig(config, settings);

        baseUrl.Should().Be("https://openrouter.ai/api/v1");
        model.Should().Be("anthropic/claude-3-haiku");
        apiKey.Should().Be("sk-or-test");
    }

    [Fact]
    public void GetLlmConfig_ForCohere_ReturnsCorrectValues()
    {
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("cohere", "command-r"),
            Cohere = new CohereConfig("cohere-key", "command-r")
        };
        var settings = new TranscribeSettings { LlmModel = "command-r" };

        var (baseUrl, model, apiKey) = TranscribeCommand.GetLlmConfig(config, settings);

        baseUrl.Should().Be("https://api.cohere.ai/compatibility/v1");
        model.Should().Be("command-r");
        apiKey.Should().Be("cohere-key");
    }

    [Fact]
    public void GetLlmConfig_ForZAi_ReturnsCorrectValues()
    {
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("z.ai", "glm-5"),
            ZAi = new ZAiConfig("zai-key", "glm-5")
        };
        var settings = new TranscribeSettings { LlmModel = "glm-5" };

        var (baseUrl, model, apiKey) = TranscribeCommand.GetLlmConfig(config, settings);

        baseUrl.Should().Be("https://api.z.ai/api/coding/paas/v4");
        model.Should().Be("glm-5");
        apiKey.Should().Be("zai-key");
    }

    [Fact]
    public void GetLlmConfig_OverridesModelFromSettings()
    {
        var config = AppConfig.CreateDefault() with
        {
            Llm = new LlmConfig("openrouter", "anthropic/claude-3-haiku"),
            OpenRouter = new OpenRouterConfig("key", "anthropic/claude-3-haiku")
        };
        var settings = new TranscribeSettings { LlmModel = "custom/model" };

        var (_, model, _) = TranscribeCommand.GetLlmConfig(config, settings);

        model.Should().Be("custom/model");
    }
}

public class TranscribeCommandExecutionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public TranscribeCommandExecutionTests()
    {
        var services = new ServiceCollection();
        var mockPromptManager = new Mock<IPromptManager>();
        mockPromptManager.Setup(x => x.InitializeAsync(default)).Returns(Task.CompletedTask);
        mockPromptManager.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new List<StoredPrompt>());
        services.AddSingleton<IPromptManager>(mockPromptManager.Object);
        services.AddSingleton(AppConfig.CreateDefault());
        _serviceProvider = services.BuildServiceProvider();
        TranscribeCommand.Services = _serviceProvider;
    }

    public void Dispose()
    {
        TranscribeCommand.Services = null;
        _serviceProvider.Dispose();
    }

    private static CommandContext MakeContext() => new([], new FakeRemainingArguments(), "", null);

    [Fact]
    public void ExecuteList_WithNoPrompts_ReturnsZero()
    {
        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { ListPrompts = true };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteList_WithPrompts_ReturnsZero()
    {
        var prompts = new List<StoredPrompt>
        {
            new(1, "Hello world", "test.wav", DateTime.Now, "Greeting")
        };
        var mockPm = new Mock<IPromptManager>();
        mockPm.Setup(x => x.InitializeAsync(default)).Returns(Task.CompletedTask);
        mockPm.Setup(x => x.GetAllAsync(default)).ReturnsAsync(prompts);

        var services = new ServiceCollection();
        services.AddSingleton(mockPm.Object);
        services.AddSingleton(AppConfig.CreateDefault());
        using var sp = services.BuildServiceProvider();
        TranscribeCommand.Services = sp;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { ListPrompts = true };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteQuery_WithNoPrompts_ReturnsZero()
    {
        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { QueryPrompt = true };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteQuery_WithPrompts_ReturnsZeroAndRemovesOldest()
    {
        var oldest = new StoredPrompt(2, "Oldest prompt", "test.wav", DateTime.Now, null);
        var prompts = new List<StoredPrompt>
        {
            new(1, "Newer", "test.wav", DateTime.Now, null),
            oldest
        };
        var mockPm = new Mock<IPromptManager>();
        mockPm.Setup(x => x.InitializeAsync(default)).Returns(Task.CompletedTask);
        mockPm.Setup(x => x.GetAllAsync(default)).ReturnsAsync(prompts);
        mockPm.Setup(x => x.RemoveByIdAsync(oldest.Id, default))
            .ReturnsAsync(true);

        var services = new ServiceCollection();
        services.AddSingleton(mockPm.Object);
        services.AddSingleton(AppConfig.CreateDefault());
        using var sp = services.BuildServiceProvider();
        TranscribeCommand.Services = sp;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { QueryPrompt = true };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(0);
        mockPm.Verify(x => x.RemoveByIdAsync(oldest.Id, default), Times.Once);
    }

    [Fact]
    public void ExecuteRemove_WithValidIndex_ReturnsZero()
    {
        var prompts = new List<StoredPrompt>
        {
            new(1, "Prompt one", "test.wav", DateTime.Now, null)
        };
        var mockPm = new Mock<IPromptManager>();
        mockPm.Setup(x => x.InitializeAsync(default)).Returns(Task.CompletedTask);
        mockPm.Setup(x => x.GetAllAsync(default)).ReturnsAsync(prompts);
        mockPm.Setup(x => x.RemoveByIdAsync(1, default)).ReturnsAsync(true);

        var services = new ServiceCollection();
        services.AddSingleton(mockPm.Object);
        services.AddSingleton(AppConfig.CreateDefault());
        using var sp = services.BuildServiceProvider();
        TranscribeCommand.Services = sp;

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { RemovePrompt = 1 };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(0);
        mockPm.Verify(x => x.RemoveByIdAsync(1, default), Times.Once);
    }

    [Fact]
    public void ExecuteRemove_WithInvalidIndex_ReturnsOne()
    {
        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { RemovePrompt = 5 };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(1);
    }

    [Fact]
    public void ExecuteFile_NoApiKey_ReturnsOne()
    {
        var services = new ServiceCollection();
        services.AddSingleton(AppConfig.CreateDefault());
        services.AddSingleton<IPromptManager>(new Mock<IPromptManager>().Object);
        TranscribeCommand.Services = services.BuildServiceProvider();

        var cmd = new TranscribeCommand();
        var settings = new TranscribeSettings { File = "test.wav" };

        var result = cmd.Execute(MakeContext(), settings);

        result.Should().Be(1);
    }

    [Fact]
    public void ExecuteFile_WithApiKey_ProcessesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ait_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var wavPath = CreateMinimalWavFile(tempDir);
            var mockStt = new Mock<ISttClient>();
            mockStt.Setup(x => x.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), default))
                .ReturnsAsync("Hello world");
            var mockLlm = new Mock<ILlmClient>();
            mockLlm.Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync("Hello world processed");
            var dbPath = Path.Combine(tempDir, "prompts.sqlite");
            var promptManager = new PromptManager(dbPath);
            promptManager.InitializeAsync().GetAwaiter().GetResult();

            var config = AppConfig.CreateDefault() with
            {
                Groq = new GroqConfig("test-key", "whisper-large-v3-turbo"),
                OpenRouter = new OpenRouterConfig("llm-key", "anthropic/claude-3-haiku")
            };

            var services = new ServiceCollection();
            services.AddSingleton<IPromptManager>(promptManager);
            services.AddSingleton(mockStt.Object);
            services.AddSingleton(mockLlm.Object);
            services.AddSingleton(config);
            services.AddSingleton<TranscriptionService>();
            using var sp = services.BuildServiceProvider();
            TranscribeCommand.Services = sp;

            var cmd = new TranscribeCommand();
            var settings = new TranscribeSettings { File = wavPath, Verbose = false };

            var result = cmd.Execute(MakeContext(), settings);

            result.Should().Be(0);
            mockStt.Verify(x => x.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), default), Times.Once);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
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
