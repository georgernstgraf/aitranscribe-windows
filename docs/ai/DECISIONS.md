# Decisions

Architectural and technical decisions made in this project.
Each entry documents WHAT was decided and WHY.

## 2026-04-19: Pre-implementation Architecture Decisions

### D01: TUI Framework — Terminal.Gui (not Spectre.Console)
Spectre.Console is a "render-then-prompt" library without persistent widgets, focus management, or inline editing. The Python TUI uses Textual with full reactive widgets. Terminal.Gui provides the closest equivalent: event-driven widget model, layout containers, key bindings, and focus management. Spectre.Console retained for CLI only (S10).

### D02: Target Framework — net8.0-windows for all projects
NAudio requires Windows P/Invoke. Terminal.Gui also targets Windows. Making Core platform-neutral would add abstraction complexity with no benefit since this is explicitly a Windows-only port.

### D03: MP3 Encoding — NAudio.Lame
NAudio cannot encode MP3 natively. MediaFoundationEncoder fails on Windows N editions. NAudio.Lame wraps libmp3lame and works everywhere. Adds a native DLL dependency but guarantees reliable encoding at 32k bitrate.

### D04: Model Types — record
All data models (Transcription, LlmProvider, AppConfig, TranscriptionSettings) use `record` for value-based equality, immutability, and concise syntax.

### D05: Async API Surface — all async with CancellationToken
Python is synchronous but .NET idioms demand async. The OpenAI .NET SDK is inherently async. All async methods accept CancellationToken for cooperative cancellation. No synchronous wrappers.

### D06: JSON Config — Hierarchical Structure
`{ "Groq": { "ApiKey": "..." }, "Llm": { "Provider": "openrouter" } }` instead of flat key=value. More idiomatic JSON, easier to extend, cleaner nesting.

### D07: LLM Provider Registry — Hardcoded
Match Python: OpenRouter, Cohere, z.ai hardcoded in C#. Not configurable via JSON. Simpler, matches source behavior.

### D08: Database Path — Same as Python
`%APPDATA%\aitranscribe\prompts.sqlite` — enables dual-use with existing Python installation. Migration logic ported from Python.

### D09: Config Migration — Auto-migrate from Python dotenv
If Python `config` dotenv file exists at `%APPDATA%\aitranscribe\config`, auto-import values into new `config.json` on first run.

### D10: API Key Storage — Plaintext
Match Python behavior. Keys stored as plaintext in `%APPDATA%\AITranscribe\config.json`. No DPAPI encryption.

### D11: AudioRecorder — byte[] return, system sample rate
WasapiCapture produces byte[] natively. Use system default sample rate (usually 48000 Hz) instead of forcing 44100 — avoids resampling. Let downstream (Groq) handle any rate.

### D12: STT Response Format — JSON with language detection
Use the OpenAI SDK's default JSON response format (not text). Request input language detection.

### D13: Summary Generation — Separate background method
TranscriptionService exposes `GenerateSummaryAsync()` as a separate method. TUI calls it on a background thread after storing transcription. Not inline in the pipeline.

### D14: Translation — In TranscriptionService
`TranslateAsync()` as a convenience method on TranscriptionService. Keeps all LLM orchestration in one service.

### D15: CLI Structure — Single command
One `TranscribeCommand` with all options (--file, --post-process, --english, etc.) matching Python's flat Typer callback. No subcommands.

### D16: IPromptManager — Extract interface
Interface extracted for Moq-based testing. Standard .NET DI pattern.

### D17: Clipboard — Windows.Forms.Clipboard with [STAThread]
`System.Windows.Forms.Clipboard.SetText()` with `[STAThread]` on Main entry point. No OSC52 fallback needed (Windows-only).

### D18: Keyboard Input — Console.ReadKey loop
Terminal-focused input via Console.ReadKey. No global system-wide hotkeys. Recording toggle works only when terminal is focused.

### D19: NuGet Versions — Float latest
No version pinning. Use current stable versions of all NuGet packages.

### D20: System Prompt — Character-for-character match Python
LLM system prompts (process_with_llm, summary, translation) replicated exactly from Python source. No paraphrasing.
