All 6 integration test sub-issues (I1-I6) implemented and committed. 22 integration tests created (all gated by LIVE_TEST=1). 164 unit tests green (1 pre-existing flaky test in Console.Tests).

Issues #22 (parent) and #28, #23, #24, #25, #26, #27 (sub-issues) are ready to close on GitHub.

Integration test infrastructure:
- tests/AITransscribe.Integration.Tests/ — new project (xUnit, net8.0-windows)
- LiveTestConfig — gates tests with LIVE_TEST=1 env var
- CompositionRootTestHelper — real DI with temp SQLite
- TestFixturePaths — resolves Fixtures/trump.mp3
- All 22 tests properly skip without LIVE_TEST=1

Remaining open items:
- Close GitHub issues #22, #23-#28
- Push local commits to origin
- README.md
- Pre-existing flaky test: TranscribeCommandExecutionTests.ExecuteRemove_WithInvalidIndex_ReturnsOne fails when Services is null
- Decision override: D31/D32 updated — integration tests now in separate project with real API calls

Last updated: 2026-04-24.