# Code style rules

Hard rules for C# / .NET code in this repo. Skills (`csharp-clean-architecture`, `ef-core-patterns`) cover the "how" — this file is the "must / must not".

## Language features

- **Nullable reference types ON** in every project. Do not suppress with `!` unless there is a provable invariant; prefer a runtime guard.
- **Async all the way.** No `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()`. Handlers, repositories, controllers all `Task`-returning.
- **`CancellationToken`** flows from controller → handler → repository. Never swallow it.
- **File-scoped namespaces** (`namespace TrafficMonitor.Domain;`), one type per file.

## Naming

- **No abbreviations.** `TrafficEvent`, not `TrfEvt`. `cameraId`, not `camId`. The only exceptions are well-known acronyms (`Api`, `Id`, `Sse`, `Http`).
- `_camelCase` for private fields, `PascalCase` for public members, `camelCase` for locals and parameters.
- Async methods end in `Async`.
- Interfaces prefixed with `I` (`IEventRepository`).
- Enum members `PascalCase`; map to JSON with an explicit `JsonStringEnumConverter`.

## Types

- **DTOs are `record`s.** `public record EventListItemDto(Guid EventId, ...)`. Positional records preferred; immutable by default.
- **Domain entities have private setters** and a private parameterless constructor for EF. Public methods mutate state through intent-revealing names (`event.MarkAcknowledged()`), not property assignment.
- **Value objects** are records with validation in the constructor.
- **Read vs write DTOs are distinct types** even when fields overlap (CQRS rule — see `cqrs-light` skill).

## Repository / data access

- **`IQueryable<T>` stays inside the repository.** Repositories return `IReadOnlyList<T>`, `T?`, or domain entities — never `IQueryable` to the application layer.
- **EF projection happens in the repository** so the SQL stays narrow. Do not `.ToList()` and then map in memory.
- **No raw SQL string interpolation.** Use `FromSqlInterpolated` with parameters if raw SQL is unavoidable.

## Comments and output

- Default to **no comments.** Write one only when the *why* is non-obvious: a hidden invariant, a workaround, a trade-off a reader would otherwise miss.
- Do **not** reference tickets, callers, or history in comments (`// fix for #123`, `// used by dashboard`). That belongs in git log or the PR body.
- Do **not** write XML doc comments on internal types.
- Do **not** leave `TODO` or `FIXME` without a linked issue.

## Layout

- One public type per file, file named after the type.
- Method length: no hard cap, but if a method grows past ~40 lines or three levels of nesting, extract.
- Prefer early returns over nested `if`/`else`.
- **Member order inside a type.** Readers should see what state a type carries before how it is built. Order is:
  1. Constants (`const`) and `static readonly` fields.
  2. Instance fields — private first (backing fields for the properties below).
  3. Properties — **before** constructors.
  4. Constructors — private / parameterless (e.g. the EF ctor) first, then public.
  5. Methods — public surface first, private helpers at the bottom.

  Codex's default places properties *after* constructors; include this ordering in every Codex brief so the first pass gets it right instead of needing a re-review. Auto-fails review if violated.

## What auto-fails review

- `.Result` / `.Wait()` anywhere in production code.
- `IQueryable` leaking out of `Infrastructure`.
- A domain entity with a public setter on a state field.
- A DTO declared as a `class` with settable properties.
- Raw SQL with string interpolation of user input.
- Comments that explain *what* the code does.
- Member order with properties below constructors (see `Layout` — properties come first).
