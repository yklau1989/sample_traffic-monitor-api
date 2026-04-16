---
name: csharp-clean-architecture
description: Clean Architecture layer rules for this repo — what belongs in each project, dependency direction, and common violations to reject.
---

# C# Clean Architecture

Four layers, dependencies point **inward only**. Domain knows nothing. Api knows everything.

## Project dependency graph

```
Api ──▶ Application ──▶ Domain
 │           │
 └──▶ Infrastructure ──▶ Application ──▶ Domain
```

- `Domain` — zero references.
- `Application` — references `Domain` only.
- `Infrastructure` — references `Application` + `Domain`.
- `Api` — references `Application` + `Infrastructure` (composition root).

## What goes where

| Layer | Belongs | Does NOT belong |
|---|---|---|
| Domain | Entities, enums, value objects, domain exceptions | EF attributes, DTOs, DI, anything `Microsoft.*` except `System.*` |
| Application | Commands, queries, handlers, DTOs, repository **interfaces**, validation | EF `DbContext`, SQL, HTTP types |
| Infrastructure | `DbContext`, EF configs, migrations, repository **implementations**, external clients | Business rules, controllers |
| Api | Controllers, middleware, DI wiring, `Program.cs`, SSE endpoint | Business logic, direct EF queries |

## Rules the reviewer enforces

1. **Domain entities have private setters.** Mutation via methods that enforce invariants.
2. **Repository interfaces in Application, implementations in Infrastructure.** Controllers depend on the interface.
3. **No `DbContext` in Application or Api.** If a controller needs data, it goes through a handler → repository.
4. **DTOs are records.** Input DTOs in `Application/Commands/`, output DTOs in `Application/Queries/` — read and write DTOs are different types.
5. **`IQueryable<T>` never leaves the repository.** Return `IReadOnlyList<T>` or a single entity/DTO.
6. **Async I/O methods end in `Async`.** No `.Result` / `.Wait()` anywhere.
7. **Nullable reference types enabled** in every `.csproj`. Treat warnings as errors in CI.

## Common violations (reject in review)

- Controller calling `_dbContext.Events.Where(...)` directly.
- Domain entity with `public` setters or a parameterless public constructor used outside EF.
- DTO shared between a command and a query.
- `using Microsoft.EntityFrameworkCore;` inside `Application/`.
- Repository returning `IQueryable<T>` — defers execution outside the repo and leaks the ORM.

## Naming

- Entities: `TrafficEvent`, not `TrfEvt` or bare `Event` (collides with `System.EventHandler`).
- Handlers: `{Action}{Entity}Handler` — `IngestTrafficEventHandler`, `ListTrafficEventsHandler`.
- DTOs: `{Entity}{Purpose}Dto` — `TrafficEventListItemDto`, `TrafficEventDetailDto`.
