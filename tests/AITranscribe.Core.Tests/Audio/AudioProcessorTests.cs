using AITranscribe.Core.Audio;
using FluentAssertions;
using NAudio.Wave;

namespace AITranscribe.Core.Tests.Audio;

public class AudioProcessorTests
{
    private static string CreateSilentWav(int durationSeconds = 1, int sampleRate = 16000, int channels = 1)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.wav");
        var samples = sampleRate * durationSeconds * channels;
        var buffer = new short[samples];
        using var writer = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, channels));
        for (var i = 0; i < samples; i++)
        {
            var bytes = BitConverter.GetBytes(buffer[i]);
            writer.Write(bytes, 0, bytes.Length);
        }
        return path;
    }

    [Fact]
    public async Task CompressAsync_WavToMp3_ProducesMp3File()
    {
        var wavPath = CreateSilentWav();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.mp3");
        try
        {
            var result = await AudioProcessor.CompressAsync(wavPath, outputPath);
            Assert.True(File.Exists(result));
            Assert.EndsWith(".mp3", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(wavPath)) File.Delete(wavPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task CompressAsync_SmallFile_ProducesNonEmptyOutput()
    {
        var wavPath = CreateSilentWav();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.mp3");
        try
        {
            var result = await AudioProcessor.CompressAsync(wavPath, outputPath);
            var fileInfo = new FileInfo(result);
            fileInfo.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(wavPath)) File.Delete(wavPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GetDurationAsync_ValidWavFile_ReturnsPositiveDuration()
    {
        var wavPath = CreateSilentWav(durationSeconds: 2);
        try
        {
            var duration = await AudioProcessor.GetDurationAsync(wavPath);
            duration.Should().BeGreaterThan(TimeSpan.Zero);
            duration.Should().BeCloseTo(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
        }
        finally
        {
            if (File.Exists(wavPath)) File.Delete(wavPath);
        }
    }
}
