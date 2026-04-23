# Project State

Current status as of 2026-04-20.

## Current Focus

All 15 TDD sub-issues (S1-S15) completed and closed. Full DI wiring, CLI, and TUI paths implemented. 160 tests green.

## Completed

- [x] S1: Solution scaffolding (4 projects, NuGet packages, net8.0-windows)
- [x] S2: Models (Transcription, LlmProvider, PreProcessMode as records)
- [x] S3: Configuration (AppConfig + ConfigManager, JSON, Python dotenv migration)
- [x] S4: Data (IPromptManager + PromptManager, SQLite CRUD, migration)
- [x] S5: Audio Processing (AudioProcessor + AudioChunker, NAudio.Lame MP3 encoding)
- [x] S6: Audio Recording (AudioRecorder + IAudioCapture + WasapiAudioCapture)
- [x] S7: STT Client (ISttClient + GroqSttClient, OpenAI SDK)
- [x] S8: LLM Client (ILlmClient + LlmClient + LlmProviders, hardcoded registry)
- [x] S9: Processing Pipeline (TranscriptionService, 4-phase, summary, translation)
- [x] S10: CLI Commands (TranscribeSettings + TranscribeCommand, Spectre.Console.Cli)
- [x] S11: TUI Layout (AITranscribeTui, Terminal.Gui, two-column layout)
- [x] S12: TUI Recording (RecordingController, background Task.Run, MainLoop.Invoke)
- [x] S13: TUI History (HistoryManager, CRUD, append mode)
- [x] S14: TUI Config + Translation + Clipboard (Prompts, ClipboardHelper, WinForms)
- [x] S15A: CompositionRoot (Microsoft.Extensions.DependencyInjection, all services wired)
- [x] S15B: TranscribeCommand wired (CLI: --list, --query, --remove, --file, mic)
- [x] S15C: TUI Launch wired (no-args detection, RecordingController owns state, TuiOrchestrator)
- [x] S15D: Integration Smoke Tests (4 tests, real temp SQLite, mocked APIs)

## Test Summary

- **160 tests total, 160 passed, 0 failed**
- Core.Tests: 63 tests
- Console.Tests: 97 tests

## Architecture

- CompositionRoot wires all services via Microsoft.Extensions.DependencyInjection
- TranscribeCommand uses static Services property set by Program.cs
- No-args → RunTui() before Spectre CommandApp
- CLI args → CommandApp<TranscribeCommand> with service resolution
- TuiOrchestrator wires RecordingController callbacks to AITranscribeTui

## Pending

- Issue #22: Integration tests (sub-issues I1-I6, #28/#23-#27)
- README.md
- Polish and UX refinement
- Push 3 local commits to origin

## Blockers

None

## Next Session

Implement integration tests starting with I1 (#28), or README.md / push.
