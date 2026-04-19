using AITranscribe.Core.Audio;
using FluentAssertions;
using Moq;

namespace AITranscribe.Core.Tests.Audio;

public class AudioRecorderTests
{
    [Fact]
    public void IsRecording_InitiallyFalse()
    {
        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.SetupGet(c => c.IsRecording).Returns(false);
        var recorder = new AudioRecorder(mockCapture.Object);

        recorder.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void Start_SetsIsRecordingTrue()
    {
        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.SetupGet(c => c.IsRecording).Returns(true);
        var recorder = new AudioRecorder(mockCapture.Object);

        recorder.Start();

        mockCapture.Verify(c => c.Start(), Times.Once);
        recorder.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_ReturnsCapturedBytes()
    {
        var expected = new byte[] { 1, 2, 3, 4 };
        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.Setup(c => c.GetCapturedData()).Returns(expected);
        var recorder = new AudioRecorder(mockCapture.Object);

        var result = await recorder.StopAsync();

        result.Should().Equal(expected);
        mockCapture.Verify(c => c.Stop(), Times.Once);
        mockCapture.Verify(c => c.GetCapturedData(), Times.Once);
    }

    [Fact]
    public async Task Stop_WhenNoData_ReturnsEmptyArray()
    {
        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.Setup(c => c.GetCapturedData()).Returns(Array.Empty<byte>());
        var recorder = new AudioRecorder(mockCapture.Object);

        var result = await recorder.StopAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Stop_SetsIsRecordingFalse()
    {
        var mockCapture = new Mock<IAudioCapture>();
        mockCapture.Setup(c => c.GetCapturedData()).Returns(Array.Empty<byte>());
        mockCapture.SetupGet(c => c.IsRecording).Returns(false);
        var recorder = new AudioRecorder(mockCapture.Object);

        await recorder.StopAsync();

        mockCapture.Verify(c => c.Stop(), Times.Once);
        recorder.IsRecording.Should().BeFalse();
    }
}
