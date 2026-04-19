using AITranscribe.Core.Audio;
using FluentAssertions;
using NAudio.Wave;

namespace AITranscribe.Core.Tests.Audio;

public class AudioChunkerTests
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
    public async Task ChunkAsync_FileUnder25Mb_ReturnsSingleChunk()
    {
        var wavPath = CreateSilentWav();
        try
        {
            var chunks = await AudioChunker.ChunkAsync(wavPath);
            chunks.Should().ContainSingle();
            chunks[0].Should().Be(wavPath);
        }
        finally
        {
            if (File.Exists(wavPath)) File.Delete(wavPath);
        }
    }

    [Fact]
    public async Task ChunkAsync_FileOver25Mb_ReturnsMultipleChunks()
    {
        var wavPath = CreateSilentWav(durationSeconds: 3);
        try
        {
            var chunkPaths = await AudioChunker.ChunkAsync(wavPath, maxSizeMb: 0, chunkLengthMs: 500);
            chunkPaths.Should().HaveCountGreaterThan(1);
            foreach (var chunk in chunkPaths)
            {
                Assert.True(File.Exists(chunk));
            }
        }
        finally
        {
            if (File.Exists(wavPath)) File.Delete(wavPath);
            foreach (var f in Directory.GetFiles(Path.GetTempPath(), "test_chunk*"))
            {
                try { File.Delete(f); } catch { }
            }
        }
    }
}
