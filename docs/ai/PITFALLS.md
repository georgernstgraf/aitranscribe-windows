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

## NuGet / Dependencies

### P04: System.Text.Json version conflict (Terminal.Gui vs OpenAI SDK)
Terminal.Gui 2.0.0 constrains `System.Text.Json` to `>= 8.0.5 && < 9.0.0`. OpenAI SDK 2.10.0 requires `>= 10.0.3`. Resolution: explicitly reference `System.Text.Json 10.0.3` in both Core and Console projects. This produces NU1608 warnings but works at runtime. Terminal.Gui's constraint is overly strict.

### P05: Float-latest NuGet packages resolve to ancient versions
Omitting version bounds on `PackageReference` entries causes NuGet to resolve the lowest-ever published version (e.g. OpenAI 1.0.0, NAudio 1.5.0). Always specify explicit versions in `.csproj` files.
