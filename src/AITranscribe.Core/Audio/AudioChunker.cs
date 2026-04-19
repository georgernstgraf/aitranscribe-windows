using NAudio.Wave;
using NAudio.Lame;

namespace AITranscribe.Core.Audio;

public static class AudioChunker
{
    public static async Task<List<string>> ChunkAsync(string filePath, int maxSizeMb = 25, int chunkLengthMs = 600_000, CancellationToken cancellationToken = default)
    {
        var fileSizeMb = new FileInfo(filePath).Length / (1024.0 * 1024.0);
        if (fileSizeMb <= maxSizeMb)
        {
            return [filePath];
        }

        return await Task.Run(() =>
        {
            var chunks = new List<string>();
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileExt = Path.GetExtension(filePath);
            var outputDir = Path.GetDirectoryName(filePath)!;

            using var reader = new AudioFileReader(filePath);
            var totalMs = reader.TotalTime.TotalMilliseconds;
            var chunkMs = (double)chunkLengthMs;

            for (var i = 0; i * chunkMs < totalMs; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var startMs = i * chunkMs;
                var lengthMs = Math.Min(chunkMs, totalMs - startMs);

                var chunkPath = Path.Combine(outputDir, $"{fileName}_chunk{i}{fileExt}");
                WriteChunk(reader, startMs, lengthMs, chunkPath);
                chunks.Add(chunkPath);
            }

            return chunks;
        }, cancellationToken);
    }

    private static void WriteChunk(AudioFileReader reader, double startMs, double lengthMs, string outputPath)
    {
        var format = reader.WaveFormat;
        var startPos = (long)(startMs / 1000.0 * format.AverageBytesPerSecond);
        var lengthBytes = (long)(lengthMs / 1000.0 * format.AverageBytesPerSecond);

        startPos = startPos - (startPos % format.BlockAlign);
        lengthBytes = lengthBytes - (lengthBytes % format.BlockAlign);

        reader.Position = startPos;

        if (outputPath.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ||
            outputPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            using var writer = new LameMP3FileWriter(outputPath, format, 32);
            var buffer = new byte[format.AverageBytesPerSecond];
            var remaining = lengthBytes;
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = reader.Read(buffer, 0, toRead);
                if (read == 0) break;
                writer.Write(buffer, 0, read);
                remaining -= read;
            }
        }
        else
        {
            using var writer = new WaveFileWriter(outputPath, format);
            var buffer = new byte[format.AverageBytesPerSecond];
            var remaining = lengthBytes;
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = reader.Read(buffer, 0, toRead);
                if (read == 0) break;
                writer.Write(buffer, 0, read);
                remaining -= read;
            }
        }
    }
}
