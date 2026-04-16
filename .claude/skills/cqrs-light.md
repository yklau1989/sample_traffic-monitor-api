---
name: cqrs-light
description: Light CQRS pattern used in this repo — same DB, separated command and query paths, distinct DTOs, when to split and when not to.
---

# Light CQRS

Not full CQRS (no separate databases, no event sourcing). Just a clean split between **write** and **read** paths in the Application layer.

## Why split

- Commands enforce invariants → need full domain entities, validation, transactional boundaries.
- Queries feed the dashboard → need flat, projection-shaped DTOs with joins pre-resolved.
- Mixing them causes read DTOs to accidentally expose write-only fields, and makes handlers fat.

## Folder layout

```
Application/
├── Commands/
│   ├── IngestTrafficEvent/
│   │   ├── IngestTrafficEventCommand.cs     # input record
│   │   ├── IngestTrafficEventHandler.cs     # validates, hydrates entity, saves
│   │   └── IngestTrafficEventResult.cs      # output (new eventId, wasDuplicate)
│   └── ...
└── Queries/
    ├── ListTrafficEvents/
    │   ├── ListTrafficEventsQuery.cs        # filters, paging
    │   ├── ListTrafficEventsHandler.cs      # projects to DTOs
    │   └── TrafficEventListItemDto.cs       # dashboard-shaped
    └── GetTrafficEventDetail/
        └── ...
```

One folder per use case. Related types live together — easier to grep, easier to delete when a feature is cut.

## Rules

1. **Write DTOs and read DTOs never share a type.** `TrafficEventInput` (command) and `TrafficEventListItemDto` (query) are different records, even if they overlap in fields.
2. **Commands return a minimal result** — usually an id + status flags, never the full entity.
3. **Queries project directly in the repository** — `.Select(e => new TrafficEventListItemDto(...))` so EF generates a narrow SQL projection.
4. **No command handler calls a query handler.** If a command needs to read, it uses the repository directly.
5. **Read DTOs are dashboard-shaped, not entity-shaped.** Example: `detectionSummary` as a pre-formatted string, not the raw `Detection[]` array.

## When NOT to split

- Simple CRUD with identical read/write shape — don't force it. If the read DTO and the entity are truly the same, one handler is fine.
- Internal admin endpoints — not worth the ceremony.

## Handler signature convention

```csharp
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
```

No MediatR for a take-home — adds a dependency the evaluator has to learn. Plain interfaces + DI are enough.
