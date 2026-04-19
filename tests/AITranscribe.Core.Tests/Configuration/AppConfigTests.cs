using AITranscribe.Core.Configuration;
using AITranscribe.Core.Models;
using FluentAssertions;

namespace AITranscribe.Core.Tests.Configuration;

public class AppConfigTests
{
    [Fact]
    public void AppConfig_HasDefaultValues()
    {
        var config = AppConfig.CreateDefault();

        config.Groq.ApiKey.Should().BeEmpty();
        config.Groq.SttModel.Should().Be("whisper-large-v3-turbo");
        config.Llm.Provider.Should().Be("openrouter");
        config.Llm.Model.Should().Be("anthropic/claude-3-haiku");
        config.PreProcessMode.Should().Be(PreProcessMode.English);
        config.InputSource.Should().Be("microphone");
        config.LastFilePath.Should().BeEmpty();
        config.Verbose.Should().BeFalse();
        config.OpenRouter.ApiKey.Should().BeEmpty();
        config.OpenRouter.Model.Should().Be("anthropic/claude-3-haiku");
        config.Cohere.ApiKey.Should().BeEmpty();
        config.Cohere.Model.Should().Be("command-r");
        config.ZAi.ApiKey.Should().BeEmpty();
        config.ZAi.Model.Should().Be("glm-5");
    }

    [Fact]
    public void AppConfig_CanBeCreatedWithAllProperties()
    {
        var config = new AppConfig(
            new GroqConfig("sk-test", "whisper-large-v3-turbo"),
            new LlmConfig("openrouter", "anthropic/claude-3-haiku"),
            PreProcessMode.Cleanup,
            "file",
            @"C:\recordings\test.wav",
            true,
            new OpenRouterConfig("or-key", "anthropic/claude-3-haiku"),
            new CohereConfig("co-key", "command-r"),
            new ZAiConfig("zai-key", "glm-5")
        );

        config.Groq.ApiKey.Should().Be("sk-test");
        config.Llm.Provider.Should().Be("openrouter");
        config.PreProcessMode.Should().Be(PreProcessMode.Cleanup);
        config.InputSource.Should().Be("file");
        config.LastFilePath.Should().Be(@"C:\recordings\test.wav");
        config.Verbose.Should().BeTrue();
        config.OpenRouter.ApiKey.Should().Be("or-key");
        config.Cohere.ApiKey.Should().Be("co-key");
        config.ZAi.ApiKey.Should().Be("zai-key");
    }

    [Fact]
    public void AppConfig_IsRecord_Equality()
    {
        var c1 = AppConfig.CreateDefault();
        var c2 = AppConfig.CreateDefault();

        c1.Should().Be(c2);
    }

    [Fact]
    public void SubConfigs_AreRecords()
    {
        var g1 = new GroqConfig("key", "model");
        var g2 = new GroqConfig("key", "model");
        g1.Should().Be(g2);

        var l1 = new LlmConfig("openrouter", "model");
        var l2 = new LlmConfig("openrouter", "model");
        l1.Should().Be(l2);

        var o1 = new OpenRouterConfig("key", "model");
        var o2 = new OpenRouterConfig("key", "model");
        o1.Should().Be(o2);

        var c1 = new CohereConfig("key", "model");
        var c2 = new CohereConfig("key", "model");
        c1.Should().Be(c2);

        var z1 = new ZAiConfig("key", "model");
        var z2 = new ZAiConfig("key", "model");
        z1.Should().Be(z2);
    }
}
