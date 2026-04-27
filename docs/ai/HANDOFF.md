No open tasks. Last session completed TUI polish and transcript rendering bug fix.

Completed work:
- Initial app mode: starts in command mode (`EnterCommandMode()` via `_commandModeSink`)
- FrameView titles: clean source strings via `SetFramedTitle()` helper
- All FrameViews: 1-space left/right padding (`Padding.Thickness = new Thickness(1, 0, 1, 0)`)
- Mouse click exit from pane focus mode on `HistoryList`, `SourceRadioGroup`, `PreprocessRadioGroup`
- Outer `Window`: `BorderStyle = LineStyle.Rounded`
- Clock title: uses `_app.Screen.Width`, accounts for border padding, includes trailing space
- Bug fix: `TranscriptionService` now calls `transcriptCallback?.Invoke(finalText)` after LLM post-processing so cleaned text renders in transcript pane

## Working Agreements

- **Issue closure requires LIVE_TEST=1**: Before closing any GitHub issue, execute `$env:LIVE_TEST = "1"; .\test.cmd` and ensure all integration tests pass. Do not close issues with failing or unverified integration tests.

Last updated: 2026-04-27.
