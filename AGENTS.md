# aitranscribe-windows

Native .NET 8 Windows port of [AI-Transcribe](https://github.com/georgernstgraf/aitranscribe)
(Python → C# 12 / Spectre.Console / NAudio / OpenAI .NET SDK).

## Tech Stack

- C# 12, .NET 8, Windows
- Spectre.Console + Spectre.Console.Cli
- NAudio (WasapiCapture)
- OpenAI .NET SDK (Groq STT, OpenRouter, Cohere, z.ai)
- Microsoft.Data.Sqlite
- xUnit + Moq + FluentAssertions

## Build & Test

- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project src/AITranscribe.Console`

## Knowledge Bootstrap

Before starting any task, read the following files in order:

1. `docs/ai/HANDOFF.md` ← **read first, act on it**
2. `docs/ai/CONVENTIONS.md`
3. `docs/ai/DECISIONS.md`
4. `docs/ai/ARCHITECTURE.md`
5. `docs/ai/PITFALLS.md`
6. `docs/ai/STATE.md`
7. `docs/ai/DOMAIN.md` (if task involves business logic)

If the user says "continue", "resume", or "finish where we left off":
read and act on HANDOFF.md immediately without asking clarifying questions.

When the user asks to save context or invokes the knowledge-persist workflow,
use the `knowledge-persistence` skill to update `docs/ai/` files:

- `HANDOFF.md` for open tasks and next-session context
- `CONVENTIONS.md` for ongoing rules and working patterns
- `DECISIONS.md` for durable architectural or process choices and rationale
- `ARCHITECTURE.md` for the living structural map of the current system
- `PITFALLS.md` for non-obvious failures, gotchas, and ordering constraints
- `DOMAIN.md` for business or teaching-domain rules when relevant
- `STATE.md` for the current focus, completed work, pending work, and blockers

## Issue Workflow

This project uses issue-centered delivery. Before starting any
issue-related task, read the full issue-workflow skill:

- `skills/issue-workflow/SKILL.md`

Every commit must reference a GitHub issue number. Use `/issue-start`,
`/issue-commit`, and `/issue-finish` commands which delegate to the
issue-workflow skill.

## Orchestration

When coordinating multiple sub-tasks or managing an epic with sub-issues,
read the full orchestration skill and act as a pure orchestrator (no code):

- `skills/orchestration/SKILL.md`

The orchestrator decomposes work into sub-issues, delegates each to a
Task agent, verifies tests pass, and advances to the next sub-issue.

## Repository

- GitHub: `georgernstgraf/aitranscribe-windows`
- Issue tracker: GitHub Issues
