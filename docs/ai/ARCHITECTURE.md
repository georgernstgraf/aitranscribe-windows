# Architecture

Living structural map of the system as of 2026-04-24.
Overwritten when structural changes occur during a session.

## Overview

Native .NET 8 Windows port of AI-Transcribe (Python).
Solution structure: AITranscribe.Core (class library) + AITranscribe.Console (TUI/CLI app) + 3 test projects.

## Projects

| Project | Type | Purpose |
|---------|------|---------|
| AITranscribe.Core | class library (net8.0-windows) | Models, API clients, audio, config, data, services |
| AITranscribe.Console | exe (net8.0-windows) | CLI commands, TUI, composition root, entry point |
| AITranscribe.Core.Tests | test (net8.0-windows) | Unit tests for Core (66 tests) |
| AITransscribe.Console.Tests | test (net8.0-windows) | Unit + integration smoke tests for Console (98 tests) |
| AITransscribe.Integration.Tests | test (net8.0-windows) | LIVE_TEST-gated integration tests (22 tests, real APIs) |

## Integration Test Infrastructure

- `LiveTestConfig` — gates tests with `LIVE_TEST=1` env var, loads real config
- `CompositionRootTestHelper` — creates `IServiceProvider` via `CompositionRoot.Build()` with temp SQLite
- `TestFixturePaths` — resolves `Fixtures/trump.mp3` path
- All integration tests use `[Fact(Skip = LiveTestConfig.SkipReason)]` + early return guard

## Commands (`commands/`)

| Command | Purpose | Delegates to |
|---------|---------|-------------|
| `/issue-start` | Start or continue working on a task | `issue-workflow` |
| `/issue-commit` | Save work-in-progress progress to a GitHub issue | `issue-workflow` |
| `/issue-finish` | Complete a task: commit, push, close issue | `issue-workflow` |
| `/knowledge-persist` | Persist session context into docs/ai/ files | `knowledge-persistence` |

## Skills (`skills/`)

| Skill | Purpose | Used by |
|-------|---------|---------|
| `issue-workflow` | Issue lifecycle management (start, checkpoint, finish) with mandatory issue-linked commits | `/issue-start`, `/issue-commit`, `/issue-finish` |
| `knowledge-persistence` | Persist session context into structured docs/ai/ knowledge files | `/knowledge-persist` |
| `orchestration` | Orchestrate sub-agents for issue-driven task decomposition | Orchestrator agent (via Task tool) |

## Knowledge Files (`docs/ai/`)

| File | Purpose | Update mode |
|------|---------|------------|
| HANDOFF.md | Open tasks for next session | Overwrite |
| DECISIONS.md | Chronological record of choices | Append |
| ARCHITECTURE.md | Living structural map | Overwrite |
| CONVENTIONS.md | Ongoing rules to follow | Append |
| PITFALLS.md | Hard-won failure knowledge | Append |
| DOMAIN.md | Business/domain rules | Append |
| STATE.md | Current project status | Overwrite |