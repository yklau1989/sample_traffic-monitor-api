---
name: ef-core-patterns
description: EF Core conventions for this repo — JSONB detections, internal int PK vs external UUID, IQueryable containment, migrations workflow.
---

# EF Core Patterns

## Internal PK vs external ID

Every aggregate has **two** identity fields:

```csharp
public class TrafficEvent
{
    public int Id { get; private set; }              // internal, DB-only, never in JSON
    public Guid EventId { get; private set; }        // external, from detection system, in API
}
```

- `Id` — `int`, clustered PK, fast joins, small foreign keys.
- `EventId` — `Guid`, unique index, idempotency key, shown to clients.

**API never exposes `Id`.** Controllers and DTOs use `EventId` only. Route is `/api/events/{eventId:guid}`.

## Idempotency

Unique index on `EventId`:

```csharp
modelBuilder.Entity<TrafficEvent>()
    .HasIndex(e => e.EventId)
    .IsUnique();
```

Ingest handler catches `DbUpdateException` on unique-violation → returns existing event with `wasDuplicate = true`. Controller maps that to `200 OK` instead of `201 Created`.

## Detections as JSONB (not a child table)

Detections are value objects, never queried independently, always loaded with their event. Store them as JSONB:

```csharp
modelBuilder.Entity<TrafficEvent>().OwnsMany(e => e.Detections, d =>
{
    d.ToJson();
});
```

- Postgres column type: `jsonb` (EF picks this automatically on Npgsql).
- No `Detections` table, no foreign keys, no separate `DetectionId`.
- Query path: `SELECT ... detections FROM traffic_events` — one row, array hydrated in-memory.

Don't index inside the JSONB unless a query needs it — premature.

## Repository boundary

```csharp
// Application/
public interface ITrafficEventRepository
{
    Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken ct);
    Task<IReadOnlyList<TrafficEventListItemDto>> ListAsync(ListTrafficEventsQuery q, CancellationToken ct);
    Task AddAsync(TrafficEvent e, CancellationToken ct);
}
```

- `IQueryable<T>` stays inside the implementation. Never returned.
- List queries return already-projected DTOs — projection happens in the `.Select(...)` before `.ToListAsync()`.
- Single-entity fetch returns the entity (handler may map to DTO).

## Migrations workflow

```bash
# Add
dotnet ef migrations add {Name} \
  --project src/TrafficMonitor.Infrastructure \
  --startup-project src/TrafficMonitor.Api

# Apply
dotnet ef database update \
  --project src/TrafficMonitor.Infrastructure \
  --startup-project src/TrafficMonitor.Api

# Undo the last one (before commit!)
dotnet ef migrations remove \
  --project src/TrafficMonitor.Infrastructure \
  --startup-project src/TrafficMonitor.Api
```

Never hand-edit generated migrations. If wrong, `remove` and regenerate. Once committed + applied in an environment, only roll forward with a new migration.

## Misc

- `snake_case` table and column names via `UseSnakeCaseNamingConvention()` (Npgsql extension) — matches Postgres idiom, keeps SQL readable.
- Configure entities in `Infrastructure/Persistence/Configurations/` using `IEntityTypeConfiguration<T>`, not fluent code in `OnModelCreating`.
- No lazy loading. Eager-load via `.Include()` only when needed; projections handle most cases.
