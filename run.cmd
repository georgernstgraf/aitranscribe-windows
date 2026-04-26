@echo off
dotnet build src/AITranscribe.Console --verbosity quiet -nologo >nul 2>&1
dotnet src/AITranscribe.Console/bin/Debug/net8.0-windows/AITranscribe.dll %*