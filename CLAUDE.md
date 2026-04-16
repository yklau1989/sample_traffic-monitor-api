# CLAUDE.md — sample_traffic-monitor-api

## What This Is

Traffic incident monitoring API. AI video system detects highway events (debris, stopped vehicles, congestion) → this API ingests them → operators see them on a dashboard and dispatch safety teams.

**Interview take-home.** Evaluators care about: schema design, codebase structure, trade-offs, code clarity.

## Session Resume

When Martin says "let's continue", "resume", "where were we", or similar at the start of a session, do this BEFORE taking any implementation action:

1. **Read the most recently modified reasoning log** under `docs/logs/` (any `{range}/*-reasoning.md` — sort by mtime). There is no separate `docs/handoff/` directory; handoff state lives in reasoning logs.
2. **List open GitHub issues** and partition them:
   - `gh issue list --label agent:planner --state open` — planner-created backlog
   - `gh issue list --state open -- --search '-label:agent:planner'` — **Martin's own issues**, treat these as higher priority unless Martin says otherwise
3. **Check for in-progress branches**: `git branch -a` + `git status` — if there's an active `feature/{n}-...` branch with uncommitted work, surface it.
4. **Summarise in one short message**: the last log's Status/Next, any Martin-authored open issues, and a proposed next step. Then wait for Martin's direction — do not start work until he confirms.

Never assume the lowest-numbered issue is what to pick up next. Martin may have opened something more urgent.

## Tech Stack

- .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Docker Compose
- Clean Architecture, light CQRS (same DB, separate command/query paths)
- Simple HTML/JS frontend (demo only)

## Project Structure

```
src/
├── TrafficMonitor.Domain/          # Entities, enums, value objects — zero dependencies
├── TrafficMonitor.Application/     # Commands, queries, handlers, DTOs, interfaces
├── TrafficMonitor.Infrastructure/  # EF Core DbContext, repositories, migrations
└── TrafficMonitor.Api/             # Controllers, middleware, DI composition root

tests/
└── TrafficMonitor.Tests/           # xUnit tests (domain + application + integration)

frontend/                           # Static HTML/JS dashboard, served by the API
```

## Key Design Rules

- **Idempotency:** `event_id` (UUID from detection system) is the idempotency key. Unique index in DB. Duplicate POST → 200 OK, not error.
- **CQRS:** Commands in `Application/Commands/`, Queries in `Application/Queries/`. Different DTOs for write vs read. Read DTOs are dashboard-optimised (e.g., `detectionSummary` string, not raw bbox arrays).
- **SSE:** `GET /api/events/stream` pushes new events via `Channel<T>`.
- **Fake event generator:** `BackgroundService` that POSTs to the API, not direct DB insert.
- **Internal PK (int) vs external ID (UUID):** Never expose internal `Id` through API.
- **Detections stored as JSONB:** EF Core `OwnsMany` + `ToJson()`. Not a separate table.

## Endpoints

- `POST /api/events` — ingest (201 Created / 200 if duplicate)
- `GET /api/events` — list with filters (eventType, severity, from/to, cameraId), sorting, pagination
- `GET /api/events/{eventId}` — full detail with raw detections
- `GET /api/events/stream` — SSE real-time feed

## Code Conventions

- Nullable reference types enabled
- Async all the way — no `.Result` or `.Wait()`
- No abbreviations: `TrafficEvent` not `TrfEvt`
- Records for DTOs: `public record EventListItemDto(...)`
- Private setters on domain entities
- `IQueryable` inside repository only. Return `IReadOnlyList<T>`.
- `_camelCase` private fields, PascalCase public members

## Agent Workflow

Agents live in `.claude/agents/`. Use them via subagent delegation.

- **develop-planner** — reads `docs/architecture.md`, creates GitHub Issues with dependencies, assigns phases
- **reviewer** — reviews diffs before merge (PR or direct-to-main), writes reasoning log
- **backend-dev** — implements backend issues (domain, application, infrastructure, API layers)
- **frontend-dev** — implements frontend dashboard
- **infra-dev** — Docker, Docker Compose, CI, Dockerfiles

**Branching:** solo repo, so branches are optional. Use `feature/{issue-number}-short-description` when you want a PR trail for the evaluator to review (recommended for cross-cutting or risky changes). Small, self-contained changes can go straight to `main` with a clear commit message.

## Documentation Structure

```
docs/
├── architecture.md             # System design, Clean Architecture layers, data flow
├── api-reference.md            # Endpoint contracts, DTOs, status codes
├── deployment.md               # Docker Compose, env vars, operational notes
├── logs/                       # Reasoning logs — AI tool usage artifact
│   ├── 001-010/                # Phase 1: Foundation
│   ├── 011-020/                # Phase 2–3: Core + Real-Time
│   └── 021-030/                # Phase 4: Polish
└── decisions/                  # Architecture Decision Records (optional)
```

**Evaluator-facing docs** (`architecture.md`, `api-reference.md`, `deployment.md`) are hand-curated deliverables — keep them current.

**Reasoning logs** (`docs/logs/{range}/{issue-number}-reasoning.md`) — every agent writes one before completing work. Include: what was decided, alternatives considered, why this approach won, and what's still open/next. These are the AI tool usage artifact AND the cross-session handoff — the next session reads the latest log to pick up where the last one left off. Git log + open issues cover the rest.

## Hybrid Workflow (Claude + Codex)

**Claude plans → Codex codes → Claude reviews.**

Role split (Claude side = the `.claude/agents/` named agents; Codex side = OpenAI Codex):

- **Claude — `develop-planner`** owns architecture + issue breakdown. Reads `docs/architecture.md`, produces GitHub Issues with acceptance criteria, dependencies, and a target layer (domain / application / infrastructure / api / frontend / infra).
- **Codex** executes one issue at a time. Brief it with: the issue link, the target layer, the relevant Key Design Rule, and the acceptance criteria. Do NOT let it re-plan — it's the implementer.
- **Claude — `reviewer`** reviews the diff Codex produced, catches convention drift, and writes the reasoning log to `docs/logs/`.
- **Martin** resolves disagreements — cross-layer decisions require human context.

### Invoking Codex

Two entry points, pick per task:

1. **Codex CLI** (`codex`) — run directly in a terminal at the repo root. Best for a focused, single-issue implementation session. Pass the issue number and expected files to touch.
2. **Codex plugin inside Claude Code** (`/plugin install codex@openai-codex`) — use when you're already in a Claude session and want a quick handoff without switching terminals. Verify the exact slash commands after install (`/plugin` → list); earlier versions of this file mentioned `/codex:review` and `/codex:rescue` but confirm before relying on them.

### Handoff pattern (Claude → Codex)

When handing off to Codex, give it a self-contained brief — Codex starts cold and doesn't see the Claude conversation. Include:

- Issue number + link
- Target layer and files expected to change
- Applicable Key Design Rules (e.g., "idempotency via `event_id`", "return `IReadOnlyList<T>` from repo")
- Acceptance criteria copied from the issue
- Out-of-scope items (explicitly list what NOT to touch)

### When to skip Codex

Implement directly in Claude when: the change is <50 lines, touches one file, or is pure review/refactor. The handoff overhead isn't worth it.

## Running Locally

**First-time setup:**
```bash
docker compose up -d postgres           # start DB only
dotnet tool install --global dotnet-ef   # if not already installed
dotnet ef database update --project src/TrafficMonitor.Infrastructure --startup-project src/TrafficMonitor.Api
```

**Run the API:**
```bash
dotnet run --project src/TrafficMonitor.Api
# Swagger: http://localhost:5000/swagger (dotnet run default)
# Dashboard: http://localhost:5000/  (served from frontend/)

# Inside Docker, the API is exposed on :8080 — http://localhost:8080/...
# Rationale: macOS Control Center (AirPlay Receiver) binds :5000 and can't reliably be freed.
```

**Full stack (evaluator path):**
```bash
docker compose up --build               # API + DB + event generator
```

**Tests:**
```bash
dotnet test                             # all projects
dotnet test tests/TrafficMonitor.Application.Tests   # single project
```

**Add a migration:**
```bash
dotnet ef migrations add {Name} \
  --project src/TrafficMonitor.Infrastructure \
  --startup-project src/TrafficMonitor.Api
dotnet ef database update --project src/TrafficMonitor.Infrastructure --startup-project src/TrafficMonitor.Api
```

Migrations live in `src/TrafficMonitor.Infrastructure/Migrations/`. Never hand-edit — revert with `migrations remove` and regenerate.
