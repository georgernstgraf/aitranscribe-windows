param(
    [string]$OutputDir = "publish",
    [string]$Configuration = "Release",
    [string]$Project = "src/AITranscribe.Console"
)

Write-Host "Publishing AITranscribe as self-contained single-file executable..." -ForegroundColor Cyan

dotnet publish $Project `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

if ($LASTEXITCODE -eq 0) {
    $exe = Join-Path $OutputDir "AITranscribe.exe"
    if (Test-Path $exe) {
        $size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
        Write-Host "`nDone: $exe ($size MB)" -ForegroundColor Green
    }
}
