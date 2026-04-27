# Architecture

Living structural map of the system as of 2026-04-27.
Overwritten when structural changes occur during a session.

## Overview

Native .NET 10 Windows port of AI-Transcribe (Python).
Solution structure: AITranscribe.Core (class library) + AITranscribe.Console (TUI/CLI app) + 3 test projects.

## Projects

| Project | Type | Purpose |
|---------|------|---------|
| AITranscribe.Core | class library (net10.0-windows) | Models, API clients, audio, config, data, services |
| AITranscribe.Console | exe (net10.0-windows) | CLI commands, TUI, composition root, entry point |
| AITranscribe.Core.Tests | test (net10.0-windows) | Unit tests for Core (66 tests) |
| AITransscribe.Console.Tests | test (net10.0-windows) | Unit + integration smoke tests for Console (107 tests) |
| AITransscribe.Integration.Tests | test (net10.0-windows) | LIVE_TEST-gated integration tests (22 tests, real APIs) |

## Integration Test Infrastructure

- `LiveTestConfig` — gates tests with `LIVE_TEST=1` env var, loads real config
- `CompositionRootTestHelper` — creates `IServiceProvider` via `CompositionRoot.Build()` with temp SQLite
- `TestFixturePaths` — resolves `Fixtures/trump.mp3` path
- All integration tests use `[Fact(Skip = LiveTestConfig.SkipReason)]` + early return guard

## Commands (`commands/`)

| Command | Purpose | Delegates to |
|---------|---------|-------------|
| `/issue-start` | Start or continue working on a task | `issue-workflow` |
| `/issue-commit` | Save work-in-progress progress to a GitHub issue | `issue-workflow` |
| `/issue-finish` | Complete a task: commit, push, close issue | `issue-workflow` |
| `/knowledge-persist` | Persist session context into docs/ai/ files | `knowledge-persistence` |

## Skills (`skills/`)

| Skill | Purpose | Used by |
|-------|---------|---------|
| `issue-workflow` | Issue lifecycle management (start, checkpoint, finish) with mandatory issue-linked commits | `/issue-start`, `/issue-commit`, `/issue-finish` |
| `knowledge-persistence` | Persist session context into structured docs/ai/ knowledge files | `/knowledge-persist` |
| `orchestration` | Orchestrate sub-agents for issue-driven task decomposition | Orchestrator agent (via Task tool) |

## Knowledge Files (`docs/ai/`)

| File | Purpose | Update mode |
|------|---------|------------|
| HANDOFF.md | Open tasks for next session | Overwrite |
| DECISIONS.md | Chronological record of choices | Append |
| ARCHITECTURE.md | Living structural map | Overwrite |
| CONVENTIONS.md | Ongoing rules to follow | Append |
| PITFALLS.md | Hard-won failure knowledge | Append |
| DOMAIN.md | Business/domain rules | Append |
| STATE.md | Current project status | Overwrite |

## TUI Architecture

### AITranscribeTui (Window)
- Root window with `BorderStyle = LineStyle.Rounded`
- Contains `primaryColumn` (57% width), `sidebarColumn`, `HelpBar`, `_commandModeSink`
- Title bar shows centered `"AITranscribe"` + clock, computed from `_app.Screen.Width` with border padding

### Focus System
- `_focusableViews`: TranscriptView, HistoryList, SourceRadioGroup, FilePathField, PreprocessRadioGroup, SttModelField, LlmModelField
- `_commandModeSink`: 0x0 invisible `View` with `CanFocus=true`, `TabStop=NoStop` — when focused, app is in Command Mode
- `IsPaneFocusMode`: true when any `_focusableViews` has focus
- Tab/Shift+Tab: manual cycling through `_focusableViews` in `OnKeyDownNotHandled`
- Escape: `_commandModeSink.SetFocus()` returns to Command Mode
- Mouse click on focusable views: `MouseHighlightStates = MouseState.In` enables auto-grab
- Mouse click on `HistoryList`, `SourceRadioGroup`, `PreprocessRadioGroup`: calls `EnterCommandMode()`

### FrameViews
All 6 `FrameView` instances use:
- `SetFramedTitle(frame, "Title")` helper: stores `" Title "` (clean source string, spaces applied at render)
- `Padding.Thickness = new Thickness(1, 0, 1, 0)` for 1-space left/right padding

### RecordingController
- State machine: Idle → Recording → Processing → Idle
- `ProcessAudioWorkerAsync` calls `TranscriptionService.ProcessMicAudioAsync`
- `TranscriptCallback` invoked on main thread via `InvokeOnMainThread`

### TranscriptionService
- `ProcessMicAudioAsync` / `ProcessFileAsync`: raw STT text sent via `transcriptCallback`
- After `PostProcessAsync` (LLM cleanup/English), **explicitly** calls `transcriptCallback?.Invoke(finalText)` again so UI shows cleaned result
- Summary generation: separate async `GenerateSummaryAsync()` called by TUI after processing complete
