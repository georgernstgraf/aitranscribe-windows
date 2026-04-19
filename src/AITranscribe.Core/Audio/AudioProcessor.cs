using NAudio.Wave;
using NAudio.Lame;

namespace AITranscribe.Core.Audio;

public static class AudioProcessor
{
    public static async Task<string> CompressAsync(string inputPath, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        if (outputPath is null)
        {
            var dir = Path.GetDirectoryName(inputPath)!;
            var stem = Path.GetFileNameWithoutExtension(inputPath);
            outputPath = Path.Combine(dir, $"{stem}_compressed.mp3");
        }

        await Task.Run(() =>
        {
            using var reader = new AudioFileReader(inputPath);
            using var writer = new LameMP3FileWriter(outputPath, reader.WaveFormat, 32);
            reader.CopyTo(writer);
        }, cancellationToken);

        return outputPath;
    }

    public static async Task<TimeSpan> GetDurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var reader = new AudioFileReader(filePath);
            return reader.TotalTime;
        }, cancellationToken);
    }
}
