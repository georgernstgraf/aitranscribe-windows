# Architecture

Living structural map of the system as of 2026-04-19.
Overwritten when structural changes occur during a session.

## Overview

Native .NET 8 Windows port of AI-Transcribe (Python).
Solution structure: AITranscribe.Core (class library) + AITranscribe.Console (TUI/CLI app).

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
