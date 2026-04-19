# Conventions

Coding patterns, naming rules, and style agreements for this project.
Follow these without question. Do not deviate unless explicitly told.

## Naming

- Namespaces match folder structure: `AITranscribe.Core.Models`, `AITranscribe.Core.Audio`, etc.
- File-scoped namespaces in all files
- PascalCase for public members, _camelCase for private fields

## File Layout

- One type per file
- Folder = namespace

## API Patterns

- All async methods return `Task<T>` and accept `CancellationToken`
- Interfaces for all external dependencies: `ISttClient`, `ILlmClient`, `IPromptManager`
- `record` types for data models, `class` for services

## Database

- SQLite via Microsoft.Data.Sqlite
- Schema matches Python: `id INTEGER PRIMARY KEY AUTOINCREMENT, prompt TEXT NOT NULL, filename TEXT NOT NULL, created_at TEXT NOT NULL, summary TEXT DEFAULT NULL`
- `PRAGMA journal_mode=DELETE` for cloud sync compatibility
- Same DB path as Python: `%APPDATA%\aitranscribe\prompts.sqlite`

## Testing

- xUnit + Moq + FluentAssertions
- TDD cycle: write failing test -> implement minimum -> refactor
- `dotnet test` and `dotnet build` must pass before reporting completion
- Test file mirrors source path: `src/Core/Audio/AudioProcessor.cs` -> `tests/Core.Tests/Audio/AudioProcessorTests.cs`

## TUI

- Terminal.Gui for interactive TUI
- Spectre.Console for CLI commands and status rendering only
- `[STAThread]` on Main entry point (required for clipboard)
