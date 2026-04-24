# Project State

Current status as of 2026-04-24.

## Current Focus

Integration tests (#22) fully implemented. All 6 sub-issues committed. Ready to close on GitHub.

## Completed

- [x] S1-S15: All TDD sub-issues completed (160 unit tests)
- [x] I1 (#28): Integration Test Infrastructure — new project, LiveTestConfig, CompositionRootTestHelper, trump.mp3 fixture, .sln
- [x] I2 (#23): STT Integration — Groq Whisper live tests (2 tests)
- [x] I3 (#24): LLM Integration — All Providers live tests (4 tests: OpenRouter, Cohere, z.ai, translation)
- [x] I4 (#25): Full Pipeline — File Mode live tests (4 tests: Raw, Cleanup, English, Raw vs Cleanup)
- [x] I5 (#26): CLI Integration — TranscribeCommand with real DI (4 tests: --file, --list, --query, --remove)
- [x] I6 (#27): TUI Smoke — Launch & State Wiring (8 tests: construct, callbacks, state transitions, feedback, history CRUD, clipboard)

## Test Summary

- **164 unit tests**: Core.Tests (66) + Console.Tests (98)
- **22 integration tests** (all skip without LIVE_TEST=1): Integration.Tests
- 1 pre-existing flaky test in Console.Tests (NAudio file lock)

## Architecture (updated)

- AITransscribe.Integration.Tests project added to solution
- InternalsVisibleTo updated to include AITransscribe.Integration.Tests
- LiveTestConfig gates all integration tests with LIVE_TEST=1
- CompositionRootTestHelper creates real DI container with temp SQLite
- Decision override: D31/D32 → integration tests in separate project with real API calls

## Pending

- Close GitHub issues #22, #23-#28
- Push local commits to origin (6 new commits ahead)
- README.md
- Polish and UX refinement

## Blockers

- Pre-existing flaky test: `TranscribeCommandExecutionTests.ExecuteRemove_WithInvalidIndex_ReturnsOne` fails when Services is null

## Next Session

Close GitHub issues, push commits, create README.md.