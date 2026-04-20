using AITranscribe.Core.Audio;
using AITranscribe.Core.Models;
using AITranscribe.Core.Services;

namespace AITranscribe.Console.Tui;

public class RecordingController
{
    private readonly AudioRecorder _recorder;
    private readonly TranscriptionService _transcriptionService;
    private CancellationTokenSource? _cts;

    public TuiState State { get; private set; } = TuiState.Idle;
    public bool IsProcessing => State == TuiState.Processing;

    public Action<TuiState>? OnStateChanged { get; set; }
    public Action<string, string>? OnFeedback { get; set; }
    public Action<string>? OnTranscriptUpdate { get; set; }
    public Action<Transcription>? OnProcessingComplete { get; set; }
    public Action<string>? OnProcessingFailed { get; set; }
    public Action<Action>? InvokeOnMainThread { get; set; }

    public RecordingController(AudioRecorder recorder, TranscriptionService transcriptionService)
    {
        _recorder = recorder;
        _transcriptionService = transcriptionService;
    }

    public void ToggleRecording()
    {
        if (State == TuiState.Processing)
            return;

        if (State == TuiState.Idle)
        {
            StartRecording();
        }
        else if (State == TuiState.Recording)
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        _recorder.Start();
        State = TuiState.Recording;
        OnStateChanged?.Invoke(State);
    }

    private void StopRecording()
    {
        var audio = _recorder.StopAsync().GetAwaiter().GetResult();

        if (audio == null || audio.Length == 0)
        {
            State = TuiState.Idle;
            OnStateChanged?.Invoke(State);
            OnTranscriptUpdate?.Invoke("No audio recorded.");
            return;
        }

        State = TuiState.Processing;
        OnStateChanged?.Invoke(State);
        OnTranscriptUpdate?.Invoke("Waiting for transcription...");

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var token = _cts.Token;
        _ = Task.Run(() => ProcessAudioWorkerAsync(audio, token), token);
    }

    private async Task ProcessAudioWorkerAsync(byte[] audio, CancellationToken ct)
    {
        void Feedback(string stepId, string status)
        {
            InvokeOnMainThread?.Invoke(() => OnFeedback?.Invoke(stepId, status));
        }

        void TranscriptCallback(string text)
        {
            InvokeOnMainThread?.Invoke(() => OnTranscriptUpdate?.Invoke(text));
        }

        try
        {
            var settings = new TranscriptionSettings(
                PreProcessMode.Raw, "", "", "", "", false, "", null, false);
            var result = await _transcriptionService.ProcessMicAudioAsync(
                audio, settings, Feedback, TranscriptCallback, ct);
            InvokeOnMainThread?.Invoke(() =>
            {
                State = TuiState.Idle;
                OnStateChanged?.Invoke(State);
                OnProcessingComplete?.Invoke(result);
            });
        }
        catch (Exception ex)
        {
            InvokeOnMainThread?.Invoke(() =>
            {
                State = TuiState.Idle;
                OnStateChanged?.Invoke(State);
                OnProcessingFailed?.Invoke(ex.Message);
            });
        }
    }
}
