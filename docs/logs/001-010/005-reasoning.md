# Issue #5 — Backend persistence slice

## Decision

Implemented the first persistence slice with a `TrafficEvent` aggregate in Domain, a minimal repository contract in Application, and EF Core persistence in Infrastructure backed by PostgreSQL. `traffic_events` uses an internal `id` primary key, an external unique `event_id` UUID for idempotency, explicit snake_case columns, and a JSONB `detections` column mapped through `OwnsMany(...).ToJson()`. Enum values are persisted as strings rather than ints so the stored data stays readable, avoids silent meaning drift if enum member order changes, and makes ad-hoc SQL inspection easier for the evaluator.

## Options considered

- Persist enums as ints: rejected because it makes the rows harder to inspect and couples storage semantics to enum ordering.
- Use `UseSnakeCaseNamingConvention()`: rejected because the cached package versions in this environment produced EF assembly-version conflicts during clean builds; explicit column naming kept the build warning-free.
- Split detections into a separate table: rejected because the architecture explicitly treats detections as value objects that are always loaded with their parent and never queried independently.
- Skip the repository until handlers exist: rejected because the persistence slice should expose the application-layer seam now, even before CQRS handlers are added.

## Trade-offs

- String-backed enums cost more storage than ints and are slightly less efficient to index, but the schema is clearer and safer to evolve for this take-home.
- Explicit snake_case mapping is more verbose than a global naming convention and adds maintenance overhead if more entities are added later.
- The repository is intentionally minimal, so later issues will still need to add projection/query methods for the CQRS read path.
- JSONB detections preserve aggregate boundaries and avoid joins, but Postgres-side querying inside the detection payload is less convenient if future requirements change.

## Status / Next

- Verified: `dotnet build` at solution root — 5 projects, 0 warnings, 0 errors.
- Verified: `dotnet test` passes — 1 test, 0 failures (no new tests added per brief).
- Verified: `dotnet ef database update --project src/TrafficMonitor.Infrastructure --startup-project src/TrafficMonitor.Api` applied the initial migration against the compose postgres service. Schema confirmed via `psql \d traffic_events` — `id` int PK, `event_id` uuid with unique index, snake_case columns, `detections` JSONB.
- Follow-up applied by the orchestrator (Claude) outside Codex's sandbox: added `Microsoft.EntityFrameworkCore.Design` (10.0.0) to `TrafficMonitor.Api.csproj` so the `dotnet ef` CLI can resolve design-time services in the startup project. Codex scaffolded the migration assembly but could not run the `database update` step inside its sandbox (localhost socket blocked); the orchestrator ran it to green.
- Next: issue #6 can add the ingest command/handler against `ITrafficEventRepository` and rely on the existing unique `event_id` constraint for idempotency handling.

## Reviewer verdict

Run by the `reviewer` agent on `feature/5-backend-persistence-slice` vs `main`.

| Check | Result |
|---|---|
| Build | PASS |
| Tests | PASS (1 passed, 0 failed) |
| Migration | PASS (schema verified via `psql \d traffic_events`) |
| Conventions (`code-style.md`, `security.md`) | PASS |
| Reasoning log (4 required sections) | PASS |
| Acceptance criteria | PASS after follow-up fix |

Auto-fail checks all clear: no `.Result` / `.Wait()`; `IQueryable` stays inside `TrafficEventRepository`; no public setters on state fields; no hardcoded secrets; no raw SQL with interpolation; no DTO classes with settable properties. `Id` is `private set` and never exposed. `OwnsMany(...).ToJson()` and unique index on `EventId` both confirmed.

Findings:

1. **Missing `ListAsync` on `ITrafficEventRepository`** — the issue acceptance criteria called out "add, get by EventId, list" but the initial Codex commit only had `AddAsync` + `FindByEventIdAsync` + `SaveChangesAsync`. Fixed in a follow-up commit on the same branch: `ListAsync(CancellationToken)` returning `IReadOnlyList<TrafficEvent>`, ordered by `OccurredAt DESC`, `AsNoTracking()`. Rebuilt + tests green.
2. Stale sandbox-blocker sentence in Status/Next — kept as historical context; explicitly paired with "orchestrator ran it to green" so future readers see the resolution.
3. `Detection` + `BoundingBox` are `sealed record` with `private set` — intentional for EF hydration; record equality remains public-property-based. No bug; worth knowing when tests arrive.
4. `Class1.cs` entries appearing as add-then-delete in the diff — scaffold artefacts from `dotnet new`, deleted in the same commit; no action.

**Verdict: READY FOR MARTIN.**

---

## Session handoff — 2026-04-16 (end of session)

### Where things stand

- **On `main`**, up to date with origin. `.claude/settings.local.json` shows as modified — harness auto-drift, do not commit.
- **Issues #1, #3, #5 merged this session.** PRs #2, #4, #6 all merged with merge commits. No open issues, no open PRs.
- Branches kept (not deleted — Martin's case-by-case rule): `feature/1-docker-compose-baseline`, `feature/3-rules-files`, `feature/5-backend-persistence-slice`.
- Postgres compose service left running (`docker compose ps` shows `postgres-1 healthy`). `traffic_events` table exists from the #5 migration.

### What this session accomplished

1. Merged #1 (Docker Compose baseline) — reviewed previously, Martin approved, merged.
2. Opened + merged #3 (populate `.claude/rules/*.md`) — 5 files, each ≤100 lines, reviewer READY FOR MARTIN.
3. Opened + merged #5 (backend persistence slice) via the hybrid workflow: **Codex drafted → Claude built/tested/migrated to green → reviewer agent approved → Claude opened PR + merged.** Added `Microsoft.EntityFrameworkCore.Design` (Codex missed it) and `ITrafficEventRepository.ListAsync` (reviewer gap).

### Open when the next session starts

- **Next issue to plan:** ingest handler + `POST /api/events` endpoint + CQRS command. Should rely on unique `event_id` index for idempotency (duplicate → 200 OK, not error). Ingest DTO lives in `Application/Commands/`; controller in `Api/`. Fake event generator is a separate issue; SSE also separate.
- **Hybrid workflow confirmed working.** Codex sandbox blocks `.git/index.lock` writes and localhost sockets — orchestrator must always run build/migrate/test to green and commit. Budget ~15 min codex exec + 5 min orchestrator verification per slice.

### Known landmines for future-me

- **Codex CLI quirks** — `codex exec --full-auto` cannot commit inside its sandbox (`Operation not permitted` on `.git/index.lock`) and cannot reach `localhost:5432` (`SocketException 13: Permission denied`). Plan for orchestrator to run `dotnet ef database update` + `dotnet test` + `git commit` itself.
- **`Microsoft.EntityFrameworkCore.Design` must be on the Api startup project**, not Infrastructure — `dotnet ef` CLI looks for it there. Codex missed this; future briefs should mention it explicitly.
- **`dotnet-ef` global tool** lands at `~/.dotnet/tools`; PATH doesn't auto-update on install — use `export PATH="$PATH:/Users/yklau/.dotnet/tools"` for the current shell.
- **Explicit snake_case column mapping** was chosen over `UseSnakeCaseNamingConvention()` due to an EF assembly conflict Codex hit. If adding another entity, check whether the convention package works now before duplicating explicit mappings.
- **`.env` load for `dotnet ef`** — `set -a && source .env && set +a` exports vars into the current shell so `DesignTimeTrafficMonitorDbContextFactory` resolves `ConnectionStrings__Postgres`.

### Usage note

Session ended quickly on request (extra-usage alert). No pending work, no uncommitted state beyond harness drift. Safe cold-start.
