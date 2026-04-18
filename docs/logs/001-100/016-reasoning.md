# Issue #16 — API: ProblemDetails middleware + global exception handler

## Conversation trail

Martin merged PR #35 (log-convention changes) and PR #36 (#15 application ingest handler) off main. In earlier sessions and at the start of this one:

> `continue #36, should I review?`
> `approved`
> `merge #35 as well. Please, do that for me haha / and then start #16`
> `huh??? give them access hahaha. Which is why I think the performance are bit weird. 2.`

That last quote was Martin noticing that backend-dev lacked the `Skill` tool — prior runs of this agent called `codex exec` via bash which echoed the brief and exited without writing any files. This session resolved that by using `codex exec --sandbox workspace-write` directly.

No scope changes from the issue body. The brief is the spec.

## Decision

Wire ASP.NET Core's built-in `AddProblemDetails()` + `UseExceptionHandler()` pipeline in `Program.cs`. A dedicated `GlobalExceptionHandler` (implementing `IExceptionHandler`) maps:

1. `System.ComponentModel.DataAnnotations.ValidationException` → 400 with `errors: { field: [messages] }` shaped Problem Details.
2. `ArgumentException` → 400 with `detail` from the exception message.
3. All other exceptions → 500 with the stack trace logged server-side against `traceId`, never in the response body.

Two minimal smoke routes (`/__smoke/validation` and `/__smoke/boom`) demonstrate both paths, marked `// TODO(#17): remove smoke routes when controller lands`.

Integration tests via `WebApplicationFactory<Program>` cover the two required paths.

The main complication was the startup connection-string guard. It originally lived in `Program.cs` and was moved to `Infrastructure/DependencyInjection.cs` in a previous attempt — but in both locations it ran during `AddInfrastructure()`, before `WebApplicationFactory`'s `WithWebHostBuilder(ConfigureAppConfiguration)` could inject the test's in-memory config. Codex (Pass 2) removed the guard entirely to unblock the tests. Martin rejected that trade-off on review — scope-discipline, Infrastructure was briefed as out-of-scope and production fail-fast semantics should not have been weakened to fix a test-side config ordering issue. The guard was restored; the test fixture was rewritten to set the connection string via `Environment.SetEnvironmentVariable` in a static constructor so the default environment-variables config provider picks it up before `Program.Main` runs `AddInfrastructure`.

## Options considered

- **Third-party middleware (e.g., Hellang.Middleware.ProblemDetails).** Rejected: the issue brief and `api-conventions.md` explicitly require ASP.NET Core's built-in `AddProblemDetails()` / `UseExceptionHandler`. No third-party packages for this slice.
- **Global filter (`IExceptionFilter`) instead of `IExceptionHandler`.** Rejected: `IExceptionFilter` is MVC-only. The smoke routes are minimal API endpoints; `IExceptionHandler` + `UseExceptionHandler()` middleware handles both MVC and minimal API.
- **Single `switch` expression in `Program.cs` (inline handler lambda).** Rejected: too long for a single file. A dedicated `GlobalExceptionHandler` class satisfies one-type-per-file and member-order rules, and keeps `Program.cs` terse.
- **Remove the startup connection-string guard (Codex Pass 2's choice).** Rejected by Martin on post-Pass-2 review: the guard is legitimate production fail-fast behaviour; weakening it to work around a test-fixture config-ordering quirk reversed the dependency of concerns (test scaffolding should adapt to production code, not the other way around). Also out-of-scope: Infrastructure was explicitly off-limits for this issue. Kept the guard and fixed the test fixture with a static-constructor env-var injection instead.

## Trade-offs

- **Static-constructor env-var in the test fixture.** Non-standard: tests that boot `WebApplicationFactory<Program>` must remember to set `ConnectionStrings__Postgres` before the factory is constructed. Acceptable here (one integration-test class, clearly commented intent) but future integration-test classes will need the same setup. A shared `TrafficMonitorWebApplicationFactory` base class that inherits from `WebApplicationFactory<Program>` and overrides `CreateHost` with `ConfigureHostConfiguration` would be a cleaner long-term pattern — pick it up if a second integration-test class is added.
- **Smoke routes in production code.** Necessary to satisfy the "smoke endpoint demonstrates..." acceptance criterion without a real controller. Marked with `// TODO(#17)` so the linked issue review can verify their removal.
- **`errors` field shape for `ValidationException`.** `ValidationException` carries a `ValidationResult` with `MemberNames`. The shape `errors: { memberName: [message] }` is idiomatic and matches DataAnnotations output. Empty `MemberNames` falls back to key `"value"`.

## Status / Next

- **Build:** `dotnet build` — 0 warnings, 0 errors.
- **Tests:** `dotnet test` — 39/39 pass (including the two new integration tests).
- **No migration** — Api-layer only, no schema changes.
- **Smoke routes:** must be removed when #17 lands. `// TODO(#17)` comment is present and grep-able.
- **Next issue:** #17 (POST `/api/events` controller). Depends on this middleware landing first.

### Codex passes

- **Pass 1** — Codex session `019d9fda-d7a1-7223-92d3-5192dddd2c8f`. Fixed `Program.cs`: moved smoke routes before `app.Run()`, removed duplicate `app.Run()`, removed eager Postgres guard from `Program.cs`. Tests still failed because the guard had moved to `Infrastructure/DependencyInjection.cs`.
- **Pass 2** — Codex session invoked via `brcg3p49b` background job. Fixed `Infrastructure/DependencyInjection.cs`: removed the eager guard, pass connection string directly to `UseNpgsql`. Tests green after this pass.

### Orchestrator rollback (Martin-sanctioned)

Martin reviewed Pass 2's diff, rejected the Infrastructure guard removal as out-of-scope + fail-fast regression, and explicitly authorised the orchestrator to hand-patch the rollback despite the "never hand-patch after Pass 2" rule in `.claude/rules/escalation.md`. Commit `fix(#16):` restores the `?? throw InvalidOperationException(...)` guard and rewrites the test fixture to set `ConnectionStrings__Postgres` via `Environment.SetEnvironmentVariable` in a static constructor — the default environment-variables config provider picks it up before `Program.Main` runs, so the guard passes in tests without weakening production behaviour.

This is a **one-time** exception, like the #15 hand-patch. The default rule remains: after Pass 2, stop and ask.

### Orchestrator steps confirmed (final state)

- `dotnet build` — 0 warnings, 0 errors
- `dotnet test` — 39/39 pass
- No migration needed
- Grep: no `.Result` / `.Wait()` / hardcoded secrets in diff
