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

- Branch: `feature/20-get-events-controller`
- Codex Pass 1 job ID: _(to be appended after Codex run completes)_
- Orchestrator will: `dotnet build` → `dotnet test` → commit → push → open PR → hand to `reviewer` agent.
- After reviewer signs off and Martin approves the PR, #20 closes and the repo reaches the MVP target for submission.
- Issues explicitly out of scope for submission: #18, #21, #22, #23–#29, #27, #30, #31.

**Next after #20:** Martin reviews + approves PR; submission ready.
