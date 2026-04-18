# Issue #20 — [claude] API: GET /api/events controller (paged envelope)

## Conversation trail

**Martin's directive (verbatim from task brief):**

> Implement GitHub issue #20 — API: GET /api/events controller (paged envelope).
> ...
> **Deviation from issue's "depends on #18" line:** #18 (Testcontainers harness) is being skipped for submission. Use the **same in-memory stub pattern** that #17's `EventsControllerTests` used — `WebApplicationFactory` + `InMemoryTrafficEventRepository` test double. Do NOT pull in Testcontainers. Brief Codex on this explicitly.
> ...
> This is the LAST required code issue for submission. Martin is shipping a take-home; nice-to-haves (#18 integration harness, #21+#22 detail, #23-#29 SSE+UI, #27 fake generator, #30 compose wiring, #31 CI) are all explicitly out of scope.

**Interpretation:** Issue #20 has `Depends on: #18 (integration harness)` in its description, but Martin explicitly confirmed this dependency is being dropped. The question "do you need #18 to reach our target? No" was resolved by the directive above — #18's Testcontainers fixture is being skipped in favour of the same in-memory `WebApplicationFactory` + `InMemoryTrafficEventRepository` stub pattern established in #17's tests. This is a deliberate scope reduction for submission deadline reasons.

**Key pre-conditions confirmed before briefing Codex:**
- #19 shipped: `ListTrafficEventsHandler`, `ListTrafficEventsQuery`, `EventListItemDto`, `PagedResult<T>`, `InvalidSortFieldException`, and `ITrafficEventRepository.ListAsync` all in place.
- #17 shipped: `EventsController` with POST action, `GlobalExceptionHandler`, `WebApplicationFactory`-based test infra with `InMemoryTrafficEventRepository` stub all present.
- 53/53 tests green before starting #20.
- The `InMemoryTrafficEventRepository.ListAsync` stub in the existing test file currently throws `NotSupportedException` — needs to be replaced with a working in-memory implementation.

## Decision

Add a `[HttpGet]` `ListAsync` action to the existing `EventsController`. Bind query parameters to a `ListEventsRequest` positional record. Enforce UTC on `from`/`to` at the controller boundary. Cap `pageSize` at 200 in the controller. Map `InvalidSortFieldException` → 400 in `GlobalExceptionHandler`. Register `ListTrafficEventsHandler` in `Program.cs`. Upgrade the existing `InMemoryTrafficEventRepository` stub to support filtering/sorting/paging for the new test cases. Add 4 new test methods covering happy path, severity filter, unknown sort field, and naive timestamp.

## Options considered

- **Return 400 for unknown query params via custom model binding** — rejected because ASP.NET default ignores unknown query params, and the acceptance criteria only requires unknown *sort field* → 400. Enumerating unknown query params would require a custom binder and adds complexity for zero evaluator value.
- **Introduce a `ListEventsRequest` in a separate file** — rejected; the record is small and API-layer only. Keeping it in the same file as the controller reduces file count and is consistent with take-home scope.
- **Use Testcontainers for integration tests (as originally specified in #18's dependency)** — rejected by Martin's explicit directive. The in-memory stub provides sufficient coverage for a take-home submission and keeps the test suite fast and dependency-free.
- **Add a `[Range]` attribute on `PageSize` to validate max** — considered, but clamping silently (rather than rejecting) follows the principle of being lenient on input for this endpoint. The cap is noted in docs.

## Trade-offs

- **No #18 Testcontainers harness** means the tests exercise only in-memory behaviour, not actual PostgreSQL query semantics (e.g. case sensitivity, index behaviour). For a take-home this is acceptable.
- **Silent `pageSize` clamping** (min/max) rather than a 400 means callers may get fewer results than asked for without knowing why. Documented in api-reference.md.
- **`ListEventsRequest` in controller file** keeps things tidy for a small record but deviates slightly from "one type per file" (`code-style.md`). Acceptable at take-home scope.

## Status / Next

**PAUSED — pending Claude Code session restart.**

Two backend-dev spawns this session both stopped at the Codex handoff because the runtime tool-list snapshot for subagents omitted `Skill`, even though `.claude/agents/backend-dev.md` frontmatter granted it and `.claude/settings.local.json` allowlisted `Skill(codex:rescue)`. The runtime appears to cache subagent tool grants at session start; frontmatter changes need a Claude Code restart to take effect.

**Companion fix landed (PR #42, branch `chore/agent-watchdog-skill-handoff`):**
- Explicit `Skill(skill: "codex:rescue", args: "<brief>")` invocation guidance added to backend-dev and frontend-dev.
- Watchdog section added to both: 10-min cap, 3-min phase-stall, terminal-status stop, poll-error retry-once, 30-sec interactive-prompt bail-out, escalation format.
- Note added that stale system-reminder tool lists should not block the attempt.

**Resumed 2026-04-18 (session 2):**

- PR #42 (Skill handoff + watchdog rules) merged cleanly; main now has it and feature/20 was updated with `git merge origin/main`.
- Martin directed a plan change: do NOT spawn `backend-dev` again. Previous two `backend-dev` spawns hung indefinitely at `Skill(codex:rescue)` (runtime tool-grant cache issue). Instead, **orchestrator drives Codex directly from main thread** per the new `Orchestrator fallback` rule added in `.claude/rules/escalation.md` (commit 366b33a, same branch).
- Tier 1 (orchestrator drives Codex via `Skill(codex:rescue)` from main thread) is the path taken for #20. Tier 2 (orchestrator writes code layer-by-layer with reviewer gates) is reserved for Codex-unreachable cases.
- `/codex:setup --json` verified: `ready: true`, `authMethod: chatgpt`, `loggedIn: true`. Proceeding.

**Brief for next backend-dev spawn (self-contained):**
- Issue: https://github.com/yklau1989/sample_traffic-monitor-api/issues/20
- Files to change:
  - `src/TrafficMonitor.Api/Controllers/EventsController.cs` — add `[HttpGet] ListAsync` action with `[FromQuery] ListEventsRequest` positional record, UTC validation on `from`/`to` (return 400 ProblemDetails if `Kind != Utc`), pageSize cap 200, dispatch to `ListTrafficEventsHandler`, return `Ok(PagedResult<EventListItemDto>)`.
  - `src/TrafficMonitor.Api/Middleware/GlobalExceptionHandler.cs` — add `InvalidSortFieldException` → 400 branch BEFORE the existing `ArgumentException` → 422 branch.
  - `src/TrafficMonitor.Api/Program.cs` — register `ListTrafficEventsHandler` as scoped.
  - `tests/TrafficMonitor.Tests/Api/EventsControllerTests.cs` — replace `InMemoryTrafficEventRepository.ListAsync` `NotSupportedException` stub with a real in-memory implementation mirroring `TrafficEventRepository.ListAsync` (filter + sort + page over a `List<TrafficEvent>`); add 4+ tests: happy path returns paged envelope, severity filter narrows (seed 3 events, filter `severity=high`), unknown sort field → 400, naive timestamp `from` (no `Z`) → 400.
  - `docs/api-reference.md` — populate the `GET /api/events` section: query params, response shape, example request/response.
- **Use stub-based test pattern from #17. Do NOT pull in Testcontainers.** #18 is out of scope for submission.
- Take-home scope: NO RFC URL constants, NO boilerplate `const string`s. Inline literals fine.
- Member order: properties before constructors (positional records satisfy this).
- Out of scope: Testcontainers, refactoring #17/#19 code, any new files outside the 5 listed.

**Codex job IDs:**
- Pass 1: `task-mo4afmvw-zeigzu` — kicked off via `node codex-companion.mjs task --background --write --fresh` from the main-thread orchestrator (tier 1 of the new fallback). Skill(codex:rescue) had been stalling indefinitely on its internal `task-resume-candidate` call; the direct-companion path bypassed that. Completed in 8m 55s (`status=completed`, `phase=done`). Touched exactly the 5 in-scope files. **Accepted Pass 1 — no Pass 2 needed.**

**Orchestrator verification:**
- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 57/57 passed (53 pre-existing + 4 new: happy path, severity filter, unknown-sort 400, naive-timestamp 400).
- `git diff src/ tests/ | grep -E '\.Result|\.Wait\(\)|GetAwaiter|FromSqlRaw'` — no hits.
- `ListEventsRequest` declared as positional record in `EventsController.cs` per brief. `InvalidSortFieldException` branch added before `ArgumentException` branch per brief. `ListTrafficEventsHandler` registered scoped in `Program.cs`.

**After #20 ships:** Martin reviews + approves PR; submission ready. Issues explicitly out of scope: #18, #21, #22, #23–#29, #27, #30, #31.
