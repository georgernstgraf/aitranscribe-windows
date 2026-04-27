using AITranscribe.Core.Api;
using AITranscribe.Core.Audio;
using AITranscribe.Core.Configuration;
using AITranscribe.Core.Data;
using AITranscribe.Core.Models;
using NAudio.Wave;

namespace AITranscribe.Core.Services;

public class TranscriptionService
{
    private readonly ISttClient _sttClient;
    private readonly ILlmClient _llmClient;
    private readonly IPromptManager _promptManager;
    private readonly PromptsConfig _prompts;

    public TranscriptionService(ISttClient sttClient, ILlmClient llmClient, IPromptManager promptManager, PromptsConfig prompts)
    {
        _sttClient = sttClient ?? throw new ArgumentNullException(nameof(sttClient));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _promptManager = promptManager ?? throw new ArgumentNullException(nameof(promptManager));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
    }

    public async Task<Transcription> ProcessMicAudioAsync(
        byte[] audio,
        TranscriptionSettings settings,
        Action<string, string>? feedbackCallback,
        Action<string>? transcriptCallback,
        CancellationToken ct = default)
    {
        var tempDir = Path.GetTempPath();
        var version = GetNextRecordingVersion(tempDir);
        var wavPath = Path.Combine(tempDir, ".aitranscribe_raw.wav");
        var mp3Path = Path.Combine(tempDir, $"aitranscribe_record_v{version:D3}.mp3");

        try
        {
            WritePcmAsWav(audio, wavPath);

            feedbackCallback?.Invoke("compress", "active");
            await AudioProcessor.CompressAsync(wavPath, mp3Path, ct);
            feedbackCallback?.Invoke("compress", "done");

            feedbackCallback?.Invoke("transcribe", "active");
            var rawText = await TranscribeFileAsync(mp3Path, settings.SttModel, ct);
            transcriptCallback?.Invoke(rawText);
            feedbackCallback?.Invoke("transcribe", "done");

            var finalText = await PostProcessAsync(rawText, settings, feedbackCallback, ct);
            transcriptCallback?.Invoke(finalText);

            long? promptId = null;
            if (!settings.AppendMode)
            {
                promptId = await _promptManager.AddAsync(finalText, mp3Path, null, ct);
            }

            return new Transcription(promptId ?? 0, finalText, mp3Path, DateTime.UtcNow, null);
        }
        finally
        {
            TryDelete(wavPath);
        }
    }

    public async Task<Transcription> ProcessFileAsync(
        string filePath,
        TranscriptionSettings settings,
        Action<string, string>? feedbackCallback,
        Action<string>? transcriptCallback,
        CancellationToken ct = default)
    {
        var sourceFile = filePath.Trim();
        if (string.IsNullOrWhiteSpace(sourceFile))
            throw new ArgumentException("File path is required.");
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException($"File not found: {sourceFile}");

        feedbackCallback?.Invoke("compress", "done");
        feedbackCallback?.Invoke("transcribe", "active");

        var chunks = await AudioChunker.ChunkAsync(sourceFile, cancellationToken: ct);
        var transcripts = new List<string>();
        foreach (var chunkPath in chunks)
        {
            ct.ThrowIfCancellationRequested();
            var text = await TranscribeFileAsync(chunkPath, settings.SttModel, ct);
            transcripts.Add(text);
            if (chunkPath != sourceFile)
                TryDelete(chunkPath);
        }

        var rawText = string.Join(" ", transcripts).Trim();
        transcriptCallback?.Invoke(rawText);
        feedbackCallback?.Invoke("transcribe", "done");

        var finalText = await PostProcessAsync(rawText, settings, feedbackCallback, ct);
        transcriptCallback?.Invoke(finalText);

        long? promptId = null;
        if (!settings.AppendMode)
        {
            promptId = await _promptManager.AddAsync(finalText, sourceFile, null, ct);
        }

        return new Transcription(promptId ?? 0, finalText, sourceFile, DateTime.UtcNow, null);
    }

    public async Task<string> GenerateSummaryAsync(
        long promptId,
        string model,
        string baseUrl,
        string apiKey,
        CancellationToken ct = default)
    {
        var stored = await _promptManager.GetByIdAsync(promptId, ct);
        if (stored is null)
            throw new InvalidOperationException($"Prompt with id {promptId} not found.");

        var text = stored.Prompt.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Cannot generate summary for empty text.");

        var summary = await _llmClient.ProcessAsync(text, _prompts.SummaryPrompt, model, baseUrl, apiKey, ct);
        summary = summary.Trim();

        await _promptManager.UpdateSummaryAsync(promptId, summary, ct);
        return summary;
    }

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        string model,
        string baseUrl,
        string apiKey,
        CancellationToken ct = default)
    {
        var cleaned = text.Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            throw new ArgumentException("Text to translate cannot be empty.");

        var prompt = targetLanguage.Equals("german", StringComparison.OrdinalIgnoreCase)
            ? _prompts.TranslateToGermanPrompt
            : _prompts.TranslateToEnglishPrompt;

        var translated = await _llmClient.ProcessAsync(cleaned, prompt, model, baseUrl, apiKey, ct);
        return translated.Trim();
    }

    private async Task<string> TranscribeFileAsync(string filePath, string model, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        return await _sttClient.TranscribeAsync(stream, model, ct);
    }

    private async Task<string> PostProcessAsync(
        string rawText,
        TranscriptionSettings settings,
        Action<string, string>? feedbackCallback,
        CancellationToken ct)
    {
        if (settings.PreProcessMode == PreProcessMode.Raw)
        {
            feedbackCallback?.Invoke("post_process", "done");
            return rawText;
        }

        feedbackCallback?.Invoke("post_process", "active");
        var taskPrompt = settings.PreProcessMode == PreProcessMode.Cleanup
            ? _prompts.CleanupPrompt
            : _prompts.EnglishPrompt;

        var result = await _llmClient.ProcessAsync(
            rawText, taskPrompt, settings.LlmModel, settings.LlmBaseUrl, settings.LlmApiKey, ct);

        feedbackCallback?.Invoke("post_process", "done");
        return result;
    }

    private static int GetNextRecordingVersion(string tempDir)
    {
        var pattern = $"aitranscribe_record_v";
        var maxV = 0;
        try
        {
            foreach (var fname in Directory.GetFiles(tempDir, $"{pattern}*"))
            {
                var fileName = Path.GetFileName(fname);
                var numPart = fileName.Substring(pattern.Length, 3);
                if (int.TryParse(numPart, out var v) && v > maxV)
                    maxV = v;
            }
        }
        catch (DirectoryNotFoundException) { }
        return maxV + 1;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private static void WritePcmAsWav(byte[] pcmData, string outputPath)
    {
        var waveFormat = new WaveFormat(44100, 16, 1);
        using var writer = new WaveFileWriter(outputPath, waveFormat);
        writer.Write(pcmData, 0, pcmData.Length);
    }
}
