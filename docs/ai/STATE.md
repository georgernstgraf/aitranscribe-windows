# Project State

Current status as of 2026-04-27.

## Current Focus

TUI polish and transcript rendering bug fix — **complete**.

## Completed

- [x] S1-S15: All TDD sub-issues completed (160 unit tests)
- [x] I1 (#28): Integration Test Infrastructure
- [x] I2 (#23): STT Integration
- [x] I3 (#24): LLM Integration
- [x] I4 (#25): Full Pipeline
- [x] I5 (#26): CLI Integration
- [x] I6 (#27): TUI Smoke
- [x] #30, #31: Terminal.Gui v2 RC4 migration + .NET 10 upgrade
- [x] #35: Scheme colors + focus system cleanup
- [x] #36: TUI Layout — feedback format `[ ]/[x]/[>]/[!]`, history subtitle, help bar, centered title, navy color scheme, new hotkeys (D/E/W/Del/A/C), label updates, **pane focus mode** (Tab/Shift+Tab navigation, mouse click focus, Escape to Command Mode)
- [x] #37: Config persistence via `OnConfigChanged` + `ConfigManager.Save()` on all config fields
- [x] #38: Ctrl+S works in pane focus mode, async `SaveTranscriptAsync()` with accurate feedback
- [x] Post-session polish: initial command mode, `SetFramedTitle()` helper, 1-space left/right padding on all FrameViews, mouse-click exit from pane focus mode, rounded outer Window border, robust clock title with trailing space
- [x] Bug fix: cleaned/LLM-processed text now renders in transcript pane (added `transcriptCallback?.Invoke(finalText)` after `PostProcessAsync`)

## Test Summary

- **173 unit tests**: Core.Tests (66) + Console.Tests (107)
- **22 integration tests** (all skip without LIVE_TEST=1): Integration.Tests
- 1 pre-existing flaky test in Console.Tests (NAudio file lock)

## Architecture (updated)

- .NET 10 SDK installed in local env (`%LOCALAPPDATA%\Microsoft\dotnet`)
- AITranscribeTui: `OnKeyDownNotHandled` override for custom keys (Terminal.Gui v2 RC4)
- New TUI callbacks: `OnTranslateRequested`, `OnWriteIssueRequested`, `OnDeleteRequested`
- HelpBar + HistorySubtitleLabel added to layout
- `Dim.Fill(1)` + `Pos.AnchorEnd(1)` reserved for bottom help bar
- Pane focus mode: `_commandModeSink` (0x0 view) for Escape-to-Command-Mode, `MouseHighlightStates = MouseState.In` on focusable views, manual Tab cycling
- `SetFramedTitle(FrameView, string)` helper applies `" {title} "` for clean source strings
- `BorderStyle = LineStyle.Rounded` on root `Window`
- `Padding.Thickness = new Thickness(1, 0, 1, 0)` on all `FrameView` instances
- `StartClock` uses `_app.Screen.Width` and accounts for border padding + trailing space
- `TranscriptionService` explicitly calls `transcriptCallback` after post-processing

## Pending

- README.md
- Further UX refinement

## Blockers

- Pre-existing flaky test: `TranscribeCommandExecutionTests.ExecuteRemove_WithInvalidIndex_ReturnsOne` fails when Services is null

## Next Session

- Create README.md
- Further UX refinement

## Working Agreements (updated)

- **Issue closure requires LIVE_TEST=1**: All GitHub issues must pass integration tests (`LIVE_TEST=1`) before closing. See `docs/ai/CONVENTIONS.md`.
