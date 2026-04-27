# Pitfalls

Things that do not work, subtle bugs, and non-obvious constraints.
Read this file carefully before making changes in affected areas.

## Environment

### P01: German locale — dotnet CLI outputs German
All `dotnet` CLI output on this machine is in German (e.g. "Build succeeded" → "Der Buildvorgang wurde erfolgreich ausgeführt.", "error" → "Fehler", "warning" → "Warnung"). When parsing build/test output for success/failure, match German keywords:
- Build succeeded: `"Build succeeded"` or `"erfolgreich"`
- Build FAILED: `"Build FAILED"` or `"Fehler beim Buildvorgang"`
- Error: `"error"` or `"Fehler"`
- Warning: `"warning"` or `"Warnung"`
- Test passed: `"Passed"` or `"Bestanden"`
- Test failed: `"Failed"` or `"Fehlgeschlagen"`
- Restore succeeded: `"Wiederherstellung erfolgreich"` or `"wiederhergestellt"`

### P02: dotnet not in PATH after fresh install
.NET 8 SDK was installed via `winget` but the PATH is not available in existing shell sessions. Always prefix commands with:
```powershell
$env:PATH = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```
Or use a fresh shell session.

### P03: dotnet new does not accept net8.0-windows
`dotnet new` templates reject `-f net8.0-windows`. Create projects with `-f net8.0` then edit `.csproj` to change `<TargetFramework>` to `net8.0-windows`.

### P10: Build file locking by running dotnet.exe / AITranscribe.exe
The `dotnet` build process copies DLLs into the output directory. If the app (or a test runner) is currently running, the DLL files are locked and MSBuild fails with `MSB3027` / `MSB3021` after 10 retries. Always kill running processes before rebuilding:
```powershell
taskkill /F /IM dotnet.exe
taskkill /F /IM AITranscribe.exe
```

## NuGet / Dependencies

### P04: Float-latest NuGet packages resolve to ancient versions
Omitting version bounds on `PackageReference` entries causes NuGet to resolve the lowest-ever published version (e.g. OpenAI 1.0.0, NAudio 1.5.0). Always specify explicit versions in `.csproj` files.

## Terminal.Gui v2 RC4 Focus System

### P06: Parent `CanFocus = false` blocks ALL descendant focus
Terminal.Gui v2 `SetHasFocusTrue` checks `superViewOrParent.CanFocus`. If any ancestor has `CanFocus = false` (the default for plain `View`), focus is denied for ALL descendants, even if descendants explicitly set `CanFocus = true`. **Containers must have `CanFocus = true`** for their children to receive focus.

### P07: `MouseHighlightStates` defaults to `None`, disabling auto-grab
`MouseHighlightStates` defaults to `MouseState.None`, so `ShouldAutoGrab` returns `false`. Mouse clicks on focusable views handle internal logic but DO NOT set focus. **Set `MouseHighlightStates = MouseState.In`** on every focusable view to enable mouse-driven focus activation.

### P08: Tab/Shift+Tab have no default bindings in v2 RC4
`DefaultKeyBindings` does not include `Tab`, `Shift+Tab`, or `F6`. Tab navigation must be implemented manually in `OnKeyDownNotHandled` or via `AddKeyBinding` (but `AddKeyBinding` is not exposed on `View` in RC4).

### P09: `CanFocus` auto-sets `TabStop = TabStop` if null
When setting `CanFocus = true`, Terminal.Gui auto-sets `TabStop = TabBehavior.TabStop` if it was previously `null`. For container views that should not be Tab stops but must allow descendant focus, **set `TabStop = TabBehavior.NoStop` BEFORE `CanFocus = true`**.

### P11: Title bar vertical lines are hardcoded to `LineStyle.Single`
In `BorderView.DrawLegacyBorder`, the vertical lines drawn on the left and right sides of the title (the `├`/`┤` tee characters where the title meets the border) are **hardcoded** to `LineStyle.Single`. Even when the border is `LineStyle.Rounded`, these vertical segments remain single-line. There is no public API, theme property, or configuration to change this behavior in Terminal.Gui v2 RC4.

## Application Logic

### P12: Post-processed transcript text must be sent to callback explicitly
`TranscriptionService.ProcessMicAudioAsync` and `ProcessFileAsync` invoke `transcriptCallback` with the **raw** STT result, but they do NOT automatically invoke it again after `PostProcessAsync` returns the cleaned/English text. The caller must explicitly call `transcriptCallback?.Invoke(finalText)` after post-processing if the UI should display the final result.
