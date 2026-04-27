@echo off
set "LOCALDOTNET=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
"%LOCALDOTNET%" build %*
