# Traffic Monitor API

Traffic incident monitoring API for a highway AI video system. Ingests event detections, exposes a filterable query API for an operator dashboard, and streams real-time updates via Server-Sent Events.

Built with **.NET 10 / ASP.NET Core / EF Core / PostgreSQL**, structured around a light Clean Architecture layout. See [`docs/architecture.md`](docs/architecture.md) for the design rationale.

## Quick start (Docker)

```bash
cp .env.example .env              # fill in real values if you care; defaults work for local
docker compose up --build
```

Services:

| Service  | Host port | Purpose                                       |
|----------|-----------|-----------------------------------------------|
| `api`    | `8080`    | ASP.NET Core Web API                          |
| `postgres` | `5432`  | PostgreSQL 16                                 |

Verify it's up:

```bash
curl -sS -o /dev/null -w "%{http_code}\n" http://localhost:8080/api/events
# → any valid HTTP response (endpoints are being added issue-by-issue)
```

Tear down and wipe the DB:

```bash
docker compose down -v
```

### Why port 8080, not 5000?

macOS Control Center (AirPlay Receiver) binds `:5000` and can't be reliably freed. `:8080` is the host-side port; the container internally still listens on `:8080` (the .NET image default).

## Local dev without Docker

Requires the .NET 10 SDK installed locally and a Postgres reachable at `localhost:5432`.

```bash
docker compose up -d postgres      # start only the DB
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=traffic_monitor;Username=traffic;Password=change-me-in-env"
dotnet run --project src/TrafficMonitor.Api
# API at http://localhost:5000
```

Swagger is mapped under `/openapi` in Development. The frontend (once implemented) is served as static files from `/`.

## Project structure

```
src/
  TrafficMonitor.Domain/          # Entities, value objects, enums — no external dependencies
  TrafficMonitor.Application/     # Commands, queries, handlers, DTOs, repository interfaces
  TrafficMonitor.Infrastructure/  # EF Core DbContext, repository implementations, migrations
  TrafficMonitor.Api/             # Controllers, middleware, SSE, DI, composition root
tests/
  TrafficMonitor.Tests/           # xUnit — unit + integration tests
frontend/                         # Static HTML/JS dashboard, served by the API
docs/                             # architecture.md, api-reference.md, deployment.md, logs/
.claude/                          # Agent + skill definitions for the Claude Code workflow
```

## Running tests

```bash
dotnet test
```

Integration tests spin up a real Postgres via the compose stack or Testcontainers — not mocks. See [`docs/architecture.md`](docs/architecture.md) for the rationale.

## Further reading

- [`CLAUDE.md`](CLAUDE.md) — conventions, Key Design Rules, session-resume protocol
- [`docs/architecture.md`](docs/architecture.md) — layers, CQRS, persistence, real-time design + trade-offs
- [`docs/api-reference.md`](docs/api-reference.md) — endpoint contracts
- [`docs/deployment.md`](docs/deployment.md) — operational notes

## Status

This is an interview take-home under active construction. See the [open issues](../../issues) for current scope and progress.
