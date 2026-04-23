using AITranscribe.Core.Api;
using AITranscribe.Core.Configuration;
using AITranscribe.Console.Tui;
using FluentAssertions;

namespace AITranscribe.Integration.Tests;

public class LlmIntegrationTests
{
    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessAsync_OpenRouter_ReturnsProcessedText()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.OpenRouter.ApiKey)) return;

        var sut = new LlmClient();
        var result = await sut.ProcessAsync(
            "Hello world, this is a test transcription with some um filler words",
            LlmClient.SystemPrompt,
            config.OpenRouter.Model,
            "https://openrouter.ai/api/v1",
            config.OpenRouter.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the processed text");
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessAsync_Cohere_ReturnsProcessedText()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.Cohere.ApiKey)) return;

        var sut = new LlmClient();
        var result = await sut.ProcessAsync(
            "Hello world, this is a test transcription with some um filler words",
            LlmClient.SystemPrompt,
            config.Cohere.Model,
            "https://api.cohere.ai/compatibility/v1",
            config.Cohere.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the processed text");
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessAsync_ZAi_ReturnsProcessedText()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.ZAi.ApiKey)) return;

        var sut = new LlmClient();
        var result = await sut.ProcessAsync(
            "Hello world, this is a test transcription with some um filler words",
            LlmClient.SystemPrompt,
            config.ZAi.Model,
            "https://api.z.ai/api/coding/paas/v4",
            config.ZAi.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the processed text");
    }

    [Fact(Skip = LiveTestConfig.SkipReason)]
    public async Task ProcessAsync_OpenRouter_TranslationToGerman()
    {
        if (!LiveTestConfig.IsLiveTest) return;

        var config = LiveTestConfig.LoadConfig();
        if (string.IsNullOrEmpty(config.OpenRouter.ApiKey)) return;

        var sut = new LlmClient();
        var result = await sut.ProcessAsync(
            "The weather is nice today and I would like to go for a walk in the park.",
            Prompts.TranslateToGermanPrompt,
            config.OpenRouter.Model,
            "https://openrouter.ai/api/v1",
            config.OpenRouter.ApiKey);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().NotContain("Here is the translation");
    }
}