# Traffic Monitor API

Traffic incident monitoring API for a highway AI video system. Ingests detection events, exposes a filterable query API for an operator dashboard, and will stream real-time updates via Server-Sent Events.

Built with **.NET 10 / ASP.NET Core / EF Core / PostgreSQL**, structured around a light Clean Architecture layout. See [`docs/architecture.md`](docs/architecture.md) for the design rationale.

---

## Getting started

This is the step-by-step setup path a new contributor takes after `git clone`. Follow in order — each step checks a prerequisite or produces something the next step depends on.

### Prerequisites — install these once

You need three things on your machine before any of this works. The order doesn't matter here; verify each with the given command.

**Step A — Docker Desktop**

Postgres runs in a container, so you don't pollute your machine with a database install.

```bash
docker compose version
# → Docker Compose version v2.x.x
```

If this fails, install Docker Desktop from https://www.docker.com/products/docker-desktop/.

**Step B — .NET 10 SDK**

Needed to build and run the API.

```bash
dotnet --version
# → 10.x.xxx
```

If this fails or shows an older version, install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0.

**Step C — EF Core CLI (global .NET tool)**

Needed to apply the database migrations.

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version
# → 10.x.x
```

If `dotnet-ef` is already installed, use `dotnet tool update --global dotnet-ef` to make sure it matches .NET 10.

---

### Setup — run these in order

**Step 1 — Clone and enter the repo**

```bash
git clone https://github.com/yklau1989/sample_traffic-monitor-api.git
cd sample_traffic-monitor-api
```

**Step 2 — Create your `.env`**

The repo ships `.env.example` as a template. The defaults work out-of-the-box for local development — you only need to change values if you plan to expose this somewhere.

```bash
cp .env.example .env
```

**Step 3 — Start Postgres**

Bring up *only* the Postgres container from `docker-compose.yml`. We leave the API container out of this path so you can run the API locally (hot reload, attach debugger, etc.).

```bash
docker compose up -d postgres
```

Wait for the healthcheck to pass — a few seconds. Verify:

```bash
docker compose ps postgres
# STATUS should show "healthy"
```

**Step 4 — Apply database migrations**

This creates the `traffic_events` table, the unique index on `event_id` (how idempotency works), and the JSONB `detections` column.

```bash
dotnet ef database update \
  --project src/TrafficMonitor.Infrastructure \
  --startup-project src/TrafficMonitor.Api
```

You should see `Done.` at the end.

**Step 5 — Point the API at your local Postgres**

The API reads its connection string from the `ConnectionStrings__Postgres` environment variable. Export it in the shell you're about to run the API from (the password must match what's in your `.env`):

```bash
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=traffic_monitor;Username=traffic;Password=change-me-in-env"
```

If you changed `POSTGRES_PASSWORD` in `.env`, match it here.

**Step 6 — Run the API**

```bash
dotnet run --project src/TrafficMonitor.Api
```

The API listens on `http://localhost:5124` (set in `src/TrafficMonitor.Api/Properties/launchSettings.json`). Leave this terminal open — `Ctrl+C` stops it.

**Step 7 — Verify it's alive**

From a different terminal:

```bash
curl -sS http://localhost:5124/api/events
# → {"items":[],"total":0,"page":1,"pageSize":50}
```

An empty list is the happy path — it means the API is up, Postgres is reachable, and the schema is there.

**Step 8 — Explore with Postman (or curl)**

The API publishes its OpenAPI spec in Development at:

```
http://localhost:5124/openapi/v1.json
```

In Postman: **File → Import → Link**, paste the URL above, and Postman generates a collection with every endpoint pre-filled. (There's no bundled Swagger UI yet — on purpose, it was out of scope for the take-home.)

A sample `POST /api/events` body to try:

```json
{
  "eventId": "11111111-1111-1111-1111-111111111111",
  "eventType": "Debris",
  "severity": "High",
  "cameraId": "cam-101",
  "occurredAt": "2026-04-18T10:30:00Z",
  "detections": [
    {
      "label": "debris",
      "confidence": 0.91,
      "boundingBox": { "x": 120, "y": 240, "width": 80, "height": 60 }
    }
  ]
}
```

Then `GET /api/events` will show the event. POSTing the same `eventId` again returns `200 OK` instead of `201 Created` — idempotency working.

---

## All-in-Docker alternative

If you don't care about debugging and just want the whole stack running (for an evaluator, say), skip the local-dev path entirely:

```bash
docker compose up --build
```

The API is then at `http://localhost:8080` (not 5124 — that port is only for local `dotnet run`). Postgres is on `:5432` as before. Tear everything down with `docker compose down -v` (the `-v` wipes the DB volume).

Why host port `8080` inside Docker, not `5000`? macOS Control Center (AirPlay Receiver) binds `:5000` and can't be reliably freed.

---

## Running tests

```bash
dotnet test
```

The test project (`tests/TrafficMonitor.Tests/`) covers Domain invariants, Application handlers, and controller-level tests via `WebApplicationFactory`. A full integration-test harness backed by Testcontainers Postgres is planned — tracked in issue #18.

---

## Project layout

```
src/
  TrafficMonitor.Domain/          # Entities, value objects, enums — no external dependencies
  TrafficMonitor.Application/     # Commands, queries, handlers, DTOs, repository interfaces
  TrafficMonitor.Infrastructure/  # EF Core DbContext, repository, migrations, JSONB config
  TrafficMonitor.Api/             # Controllers, middleware, DI composition root
tests/
  TrafficMonitor.Tests/           # xUnit — Domain, Application, Api folders
frontend/                         # Static HTML/JS dashboard (planned)
docs/
  architecture.md                 # Design rationale, trade-offs, out-of-scope list
  api-reference.md                # Endpoint contracts
  deployment.md                   # Operational notes
  design/                         # Evaluator-facing artifacts (decks, SVG diagram)
  logs/                           # Reasoning logs — one per issue
.claude/                          # Agent + skill definitions for the Claude Code workflow
```

---

## Further reading

- [`CLAUDE.md`](CLAUDE.md) — project conventions, Key Design Rules, session-resume protocol
- [`docs/architecture.md`](docs/architecture.md) — layer design, CQRS, persistence, real-time, trade-offs
- [`docs/api-reference.md`](docs/api-reference.md) — endpoint contracts and status codes
- [`docs/deployment.md`](docs/deployment.md) — operational notes
- [`docs/design/`](docs/design/) — evaluator decks + architecture diagram

---

## Status

Interview take-home under active construction. See the [open issues](../../issues) for current scope and progress.
