@echo off
set "LOCALDOTNET=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
"%LOCALDOTNET%" build src/AITranscribe.Console --verbosity quiet -nologo >nul 2>&1
"%LOCALDOTNET%" src/AITranscribe.Console/bin/Debug/net10.0-windows/AITranscribe.dll %*
