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
