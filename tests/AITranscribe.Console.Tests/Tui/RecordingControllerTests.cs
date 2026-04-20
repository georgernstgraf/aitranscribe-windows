using AITranscribe.Console.Tui;
using AITranscribe.Core.Api;
using AITranscribe.Core.Audio;
using AITranscribe.Core.Data;
using AITranscribe.Core.Services;
using FluentAssertions;
using Moq;

namespace AITranscribe.Console.Tests.Tui;

public class RecordingControllerTests
{
    private readonly Mock<IAudioCapture> _mockCapture;
    private readonly AudioRecorder _recorder;
    private readonly RecordingController _controller;

    public RecordingControllerTests()
    {
        _mockCapture = new Mock<IAudioCapture>();
        _mockCapture.Setup(c => c.GetCapturedData()).Returns(Array.Empty<byte>());
        _recorder = new AudioRecorder(_mockCapture.Object);

        var mockStt = new Mock<ISttClient>();
        var mockLlm = new Mock<ILlmClient>();
        var mockPrompt = new Mock<IPromptManager>();
        var service = new TranscriptionService(mockStt.Object, mockLlm.Object, mockPrompt.Object);

        _controller = new RecordingController(_recorder, service);
    }

    [Fact]
    public void Constructor_SetsInitialState()
    {
        _controller.State.Should().Be(TuiState.Idle);
    }

    [Fact]
    public void ToggleRecording_WhenIdle_StartsRecording()
    {
        _controller.ToggleRecording();

        _controller.State.Should().Be(TuiState.Recording);
        _mockCapture.Verify(c => c.Start(), Times.Once);
    }

    [Fact]
    public void ToggleRecording_WhenRecording_StopsRecording()
    {
        _controller.ToggleRecording();

        _controller.ToggleRecording();

        _mockCapture.Verify(c => c.Stop(), Times.Once);
        _mockCapture.Verify(c => c.GetCapturedData(), Times.Once);
        _controller.State.Should().Be(TuiState.Idle);
    }

    [Fact]
    public void ToggleRecording_WhenProcessing_DoesNothing()
    {
        _mockCapture.Setup(c => c.GetCapturedData()).Returns(new byte[] { 1, 2, 3 });

        _controller.ToggleRecording();
        _controller.State.Should().Be(TuiState.Recording);

        _controller.ToggleRecording();
        _controller.State.Should().Be(TuiState.Processing);

        _controller.ToggleRecording();
        _controller.State.Should().Be(TuiState.Processing);
    }

    [Fact]
    public void IsProcessing_InitiallyFalse()
    {
        _controller.IsProcessing.Should().BeFalse();
    }
}
