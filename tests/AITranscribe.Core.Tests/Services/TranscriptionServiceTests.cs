using AITranscribe.Core.Api;
using AITranscribe.Core.Data;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;
using FluentAssertions;
using Moq;

namespace AITranscribe.Core.Tests.Services;

public class TranscriptionServiceTests
{
    private readonly Mock<ISttClient> _sttClient;
    private readonly Mock<ILlmClient> _llmClient;
    private readonly Mock<IPromptManager> _promptManager;
    private readonly TranscriptionService _service;

    public TranscriptionServiceTests()
    {
        _sttClient = new Mock<ISttClient>();
        _llmClient = new Mock<ILlmClient>();
        _promptManager = new Mock<IPromptManager>();
        _service = new TranscriptionService(_sttClient.Object, _llmClient.Object, _promptManager.Object);
    }

    private TranscriptionSettings DefaultSettings(PreProcessMode mode = PreProcessMode.English) => new(
        PreProcessMode: mode,
        SttModel: "whisper-large-v3-turbo",
        LlmModel: "test-model",
        LlmBaseUrl: "https://api.test.com/v1",
        LlmApiKey: "test-key",
        AppendMode: false,
        AppendBaseText: "",
        AppendTargetId: null,
        Verbose: false
    );

    [Fact]
    public async Task ProcessMicAudioAsync_CompressesAndTranscribes()
    {
        var audio = CreateMinimalWav();
        var settings = DefaultSettings(PreProcessMode.Raw);
        _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), settings.SttModel, It.IsAny<CancellationToken>()))
            .ReturnsAsync("hello world");
        _promptManager.Setup(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await _service.ProcessMicAudioAsync(audio, settings, null, null);

        result.Text.Should().Be("hello world");
        _sttClient.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), settings.SttModel, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMicAudioAsync_WithPostProcess_CallsLlm()
    {
        var audio = CreateMinimalWav();
        var settings = DefaultSettings(PreProcessMode.English);
        _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("raw transcript");
        _llmClient.Setup(l => l.ProcessAsync("raw transcript", It.IsAny<string>(), settings.LlmModel, settings.LlmBaseUrl, settings.LlmApiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("processed text");
        _promptManager.Setup(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await _service.ProcessMicAudioAsync(audio, settings, null, null);

        result.Text.Should().Be("processed text");
        _llmClient.Verify(l => l.ProcessAsync("raw transcript", It.IsAny<string>(), settings.LlmModel, settings.LlmBaseUrl, settings.LlmApiKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMicAudioAsync_RawMode_SkipsLlm()
    {
        var audio = CreateMinimalWav();
        var settings = DefaultSettings(PreProcessMode.Raw);
        _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("raw only");
        _promptManager.Setup(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await _service.ProcessMicAudioAsync(audio, settings, null, null);

        result.Text.Should().Be("raw only");
        _llmClient.Verify(l => l.ProcessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMicAudioAsync_StoresInPromptManager()
    {
        var audio = CreateMinimalWav();
        var settings = DefaultSettings(PreProcessMode.Raw);
        _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored text");
        _promptManager.Setup(p => p.AddAsync("stored text", It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);

        var result = await _service.ProcessMicAudioAsync(audio, settings, null, null);

        _promptManager.Verify(p => p.AddAsync("stored text", It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
        result.Id.Should().Be(42);
    }

    [Fact]
    public async Task ProcessMicAudioAsync_AppendMode_ConcatenatesText()
    {
        var audio = CreateMinimalWav();
        var settings = DefaultSettings(PreProcessMode.Raw) with { AppendMode = true };
        _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new text");

        var result = await _service.ProcessMicAudioAsync(audio, settings, null, null);

        _promptManager.Verify(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Text.Should().Be("new text");
    }

    [Fact]
    public async Task ProcessFileAsync_ChunksLargeFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "dummy audio content");
            var settings = DefaultSettings(PreProcessMode.Raw);
            _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("chunk text");
            _promptManager.Setup(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1L);

            var result = await _service.ProcessFileAsync(tempFile, settings, null, null);

            result.Text.Should().Contain("chunk text");
        }
        finally
        {
            TryDelete(tempFile);
        }
    }

    [Fact]
    public async Task ProcessFileAsync_SmallFile_NoChunking()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "small");
            var settings = DefaultSettings(PreProcessMode.Raw);
            _sttClient.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("transcribed small file");
            _promptManager.Setup(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1L);

            var result = await _service.ProcessFileAsync(tempFile, settings, null, null);

            result.Text.Should().Be("transcribed small file");
            _sttClient.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            TryDelete(tempFile);
        }
    }

    [Fact]
    public async Task GenerateSummaryAsync_CallsLlmAndUpdatesSummary()
    {
        var prompt = new StoredPrompt(1, "some long transcription text", "file.mp3", DateTime.UtcNow, null);
        _promptManager.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);
        _llmClient.Setup(l => l.ProcessAsync("some long transcription text", It.IsAny<string>(), "model", "https://api.test.com/v1", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("summary text");
        _promptManager.Setup(p => p.UpdateSummaryAsync(1, "summary text", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.GenerateSummaryAsync(1, "model", "https://api.test.com/v1", "key");

        result.Should().Be("summary text");
        _llmClient.Verify(l => l.ProcessAsync("some long transcription text", It.IsAny<string>(), "model", "https://api.test.com/v1", "key", It.IsAny<CancellationToken>()), Times.Once);
        _promptManager.Verify(p => p.UpdateSummaryAsync(1, "summary text", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_CallsLlmWithLanguagePrompt()
    {
        _llmClient.Setup(l => l.ProcessAsync("hello world", It.IsAny<string>(), "model", "https://api.test.com/v1", "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("hallo welt");

        var result = await _service.TranslateAsync("hello world", "german", "model", "https://api.test.com/v1", "key");

        result.Should().Be("hallo welt");
        _llmClient.Verify(l => l.ProcessAsync("hello world", It.Is<string>(p => p.Contains("German")), "model", "https://api.test.com/v1", "key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullDependencies_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TranscriptionService(null!, _llmClient.Object, _promptManager.Object));
        Assert.Throws<ArgumentNullException>(() => new TranscriptionService(_sttClient.Object, null!, _promptManager.Object));
        Assert.Throws<ArgumentNullException>(() => new TranscriptionService(_sttClient.Object, _llmClient.Object, null!));
    }

    private static byte[] CreateMinimalWav(int sampleCount = 100)
    {
        return new byte[sampleCount * 2];
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}
