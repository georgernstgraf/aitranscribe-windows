Current work: Issue #36 — TUI Layout Based on Reference Design — **complete**.

Pane focus mode fix implemented and tested. Build green (0 errors, 173 unit tests pass).

Changes made:
1. `primaryColumn` and `sidebarColumn`: `TabStop = NoStop; CanFocus = true` — enables descendant focus
2. All focusable views: `MouseHighlightStates = MouseState.In` — enables mouse click focus
3. `TuiOrchestrator.WireTui`: `tui.TranscriptView.SetFocus()` — sets initial focus
4. `OnKeyDownNotHandled`: Tab/Shift+Tab manual cycling through `_focusableViews`, Escape focuses `_commandModeSink` (0x0 off-screen view) to return to Command Mode

Technical details:
- `_commandModeSink` is a 0x0 `View` with `CanFocus = true` and `TabStop = NoStop`. When focused, `IsPaneFocusMode` is false because it's not in `_focusableViews`. It receives no mouse hits (0 area) and no keys (they bubble up).
- `Key.Tab.WithShift` used for Shift+Tab detection in Terminal.Gui v2 RC4.
- `AdvanceFocus` exists on `View` but manual cycling gives explicit control over Tab order.

Last updated: 2026-04-27.
