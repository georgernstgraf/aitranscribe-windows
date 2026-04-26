using AITranscribe.Core.Api;
using AITranscribe.Core.Configuration;
using AITranscribe.Console.Tui;
using FluentAssertions;

namespace AITranscribe.Integration.Tests;

public class LlmIntegrationTests
{
    [Fact]
    public async Task ProcessAsync_OpenRouter_ReturnsProcessedText()
    {
        Assert.SkipUnless(LiveTestConfig.IsLiveTest, LiveTestConfig.SkipReason);

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.OpenRouter.ApiKey)) return;

        var sut = new LlmClient(config.Prompts.SystemPrompt);
        var result = await sut.ProcessAsync(
            "Hello world, this is a test transcription with some um filler words",
            config.Prompts.CleanupPrompt,
            config.OpenRouter.Model,
            "https://openrouter.ai/api/v1",
            config.OpenRouter.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the processed text");
    }

    [Fact]
    public async Task ProcessAsync_Cohere_ReturnsProcessedText()
    {
        Assert.SkipUnless(LiveTestConfig.IsLiveTest, LiveTestConfig.SkipReason);

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.Cohere.ApiKey)) return;

        var sut = new LlmClient(config.Prompts.SystemPrompt);
        var result = await sut.ProcessAsync(
            "Hello world, this is a test transcription with some um filler words",
            config.Prompts.CleanupPrompt,
            config.Cohere.Model,
            "https://api.cohere.ai/compatibility/v1",
            config.Cohere.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the processed text");
    }

    [Fact]
    public async Task ProcessAsync_ZAi_ReturnsProcessedText()
    {
        Assert.SkipUnless(LiveTestConfig.IsLiveTest, LiveTestConfig.SkipReason);

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.ZAi.ApiKey)) return;

        var sut = new LlmClient(config.Prompts.SystemPrompt);
        var result = await sut.ProcessAsync(
            "Hello world, this is a test transcription with some um filler words",
            config.Prompts.CleanupPrompt,
            config.ZAi.Model,
            "https://api.z.ai/api/coding/paas/v4",
            config.ZAi.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the processed text");
    }

    [Fact]
    public async Task ProcessAsync_OpenRouter_TranslationToGerman()
    {
        Assert.SkipUnless(LiveTestConfig.IsLiveTest, LiveTestConfig.SkipReason);

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.OpenRouter.ApiKey)) return;

        var sut = new LlmClient(config.Prompts.SystemPrompt);
        var result = await sut.ProcessAsync(
            "The weather is nice today and I would like to go for a walk in the park.",
            config.Prompts.TranslateToGermanPrompt,
            config.OpenRouter.Model,
            "https://openrouter.ai/api/v1",
            config.OpenRouter.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the translation");
    }
}