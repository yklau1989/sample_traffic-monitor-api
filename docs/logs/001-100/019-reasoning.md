# 019 — Application list query + handler (filters, sort, pagination, read DTO)

## Conversation trail

Martin directed: "Implement GitHub issue #19 — Application list query + handler (filters, sort, pagination, read DTO)."

Context given:
- Repo at `main`, clean except `.claude/settings.local.json` harness drift
- POST /api/events shipped in #17 (PR #39 merged). EventsController, IngestTrafficEventHandler, ITrafficEventRepository all exist.
- 41 existing tests green. Don't regress them.
- Controller (#20) is out of scope here.

## Decision

Implemented the full read-side application + infrastructure slice for the list endpoint:
- Added `ListTrafficEventsQuery`, `EventListItemDto`, `PagedResult<T>`, `ListTrafficEventsHandler`, and `InvalidSortFieldException` in the Application layer.
- Extended `ITrafficEventRepository` with `ListAsync`.
- Implemented `ListAsync` in `TrafficEventRepository` with EF-side filter/sort/paging and in-memory DTO mapping (required due to JSONB ValueConverter on `Detections`).
- Added 12 new tests in `ListTrafficEventsHandlerTests`. All 53 tests pass.

## Options considered

- **Pure EF projection via `.Select(...)` before `.ToListAsync()`**: Rejected. The `Detections` field is stored via a custom `ValueConverter` on a backing field (`_detections`). EF cannot translate `e.Detections` in a LINQ-to-SQL `.Select()`. The in-memory mapping after materialization is unavoidable. IQueryable stays inside the repository, so the architectural rule is still honoured.

- **Separate `DetectionSummary` as a computed column or DB function**: Overengineered for take-home scope. The detections column is fetched in the same row anyway; computing the summary in memory adds negligible overhead.

- **MediatR for handler dispatch**: Rejected per the project brief — no MediatR, plain DI injection.

## Trade-offs

- Detections are always fetched (no column-level projection) because EF cannot project around the JSONB converter. This is the correct trade-off given the architecture — the jsonb column is narrow and co-located in the same row.
- The `FakeTrafficEventRepository` in tests replicates the filter/sort/paging logic in memory. This makes the fake slightly fat but ensures test fidelity without a DB.
- `InvalidSortFieldException` lives in `Application/Exceptions/` so the controller (issue #20) can catch it and map to 400 without the exception escaping to Infrastructure.

## Status / Next

Verified:
- `dotnet build` — 0 warnings, 0 errors (orchestrator)
- `dotnet test` — 53/53 green, 41 existing + 12 new (orchestrator)
- No `IQueryable` leaks outside Infrastructure
- No `.Result`/`.Wait()` in production code
- All DTOs are records; properties before constructors
- No migration needed (read-only query, no schema change)

Open / next:
- Issue #20: GET /api/events controller — needs to wire up `ListTrafficEventsHandler` and map `InvalidSortFieldException` to 400.
- `ListTrafficEventsHandler` is not yet registered in `Program.cs` (out of scope for this issue).

## Codex job IDs

- Pass 1: background job `bbpes9a5u` (output at `/private/tmp/claude-501/.../tasks/bbpes9a5u.output`)
- Pass 2: background job `b6zeu1off` — targeted fix for `EventsControllerTests.InMemoryTrafficEventRepository` missing `ListAsync` stub (Codex missed this third fake in Pass 1)

Pass 2 was required because Pass 1 missed one fake repository. Pass 2 succeeded on first try.

## Reviewer verdict

**Verdict: READY FOR MARTIN**

Review date: 2026-04-18. Reviewer: claude-sonnet-4-6.

Build: PASS (0 warnings, 0 errors). Tests: PASS (53/53).

All acceptance criteria met. No convention violations found. Specific checks:

- All four required files present in `Application/Queries/ListTrafficEvents/`.
- `EventListItemDto` is a positional record with `DetectionSummary` string (no raw bbox).
- `PagedResult<T>` is a positional record with `{ Items, Total, Page, PageSize }` — camelCase on wire.
- `ITrafficEventRepository.ListAsync` added; returns `PagedResult<EventListItemDto>`; `IQueryable<T>` stays inside the Infrastructure repository boundary.
- Filters are AND-combined; sort supports `occurredAt` and `-occurredAt`; unknown field throws `InvalidSortFieldException`.
- `pageSize` default 50, cap 200 enforced in both real and fake repositories.
- No `.Result`/`.Wait()`, no `IQueryable` leak, no hardcoded secrets, no `TODO`/`FIXME`.
- Comment at `TrafficEventRepository.cs:47` explains a genuine non-obvious architectural constraint (JSONB ValueConverter prevents EF projection) — passes code-style.md.
- Member order correct throughout.
- Reasoning log: all required sections present, zero-padded filename, both Codex job IDs recorded.
- 12 new tests covering: empty result, filter per field, combined AND filters, ascending/descending sort, pagination (mid-page, beyond-last-page), pageSize cap, unknown sort field.

One nit (not blocking): `BuildDetectionSummary` is duplicated identically between `TrafficEventRepository` and the nested `FakeTrafficEventRepository` in tests. A shared static helper in a test utility class would eliminate the drift risk, but this is a test-internal concern and does not affect production correctness.
