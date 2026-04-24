using AITranscribe.Core.Api;
using FluentAssertions;

namespace AITranscribe.Integration.Tests;

public class SttIntegrationTests
{
    [Fact]
    public async Task TranscribeAsync_WhisperLargeV3_ReturnsNonEmptyText()
    {
        Assert.SkipUnless(LiveTestConfig.IsLiveTest, LiveTestConfig.SkipReason);

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key must be configured");

        var sut = new GroqSttClient(config.Groq.ApiKey);

        await using var audioStream = System.IO.File.OpenRead(TestFixturePaths.TrumpMp3);
        var result = await sut.TranscribeAsync(audioStream, "whisper-large-v3");

        result.Should().NotBeNullOrWhiteSpace();
        result.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task TranscribeAsync_WhisperLargeV3Turbo_ReturnsNonEmptyText()
    {
        Assert.SkipUnless(LiveTestConfig.IsLiveTest, LiveTestConfig.SkipReason);

        var config = LiveTestConfig.LoadConfig();
        LiveTestConfig.HasRequiredApiKeys(config).Should().BeTrue("Groq API key must be configured");

        var sut = new GroqSttClient(config.Groq.ApiKey);

        await using var audioStream = System.IO.File.OpenRead(TestFixturePaths.TrumpMp3);
        var result = await sut.TranscribeAsync(audioStream, "whisper-large-v3-turbo");

        result.Should().NotBeNullOrWhiteSpace();
        result.Length.Should().BeGreaterThan(10);
    }
}