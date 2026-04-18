# Issue #1 — Local dev environment — Docker Compose baseline

## Decision

Two-service stack (`postgres` + `api`) defined in `docker-compose.yml` at the repo root, with a multi-stage `Dockerfile` at `src/TrafficMonitor.Api/Dockerfile`. Secrets flow from `.env` (gitignored) into Compose variable substitution, then into container env. Adminer deliberately deferred to a follow-up — minimum viable stack first.

## Options considered

- **Include Adminer now vs defer**: deferred. Evaluator can already verify state via the forthcoming API endpoints + `psql` exec into the postgres container. Adding a third service adds compose noise without earning anything for the take-home. Follow-up issue if wanted.
- **Separate migration container vs migrate-on-startup**: chose migrate-on-startup (`dbContext.Database.Migrate()` inside `IsDevelopment()` — will be added by backend-dev in a later issue). A dedicated migration job is production hygiene, out of scope here.
- **Alpine vs Debian base for the API image**: stayed on the official `mcr.microsoft.com/dotnet/aspnet:10.0` (Debian) for broad compatibility with diagnostic tools. Alpine would save ~50MB but complicates native-dependency stories.
- **Healthcheck transport for the API**: installed `curl` in the runtime image rather than shelling TCP-checks through bash. `curl` is ~3MB; the compose healthcheck stays trivial to read (`curl -sS -o /dev/null http://localhost:8080/`) and will stay correct when a real `/health` endpoint is added.
- **Host port for the API**: planned `:5000`, switched to `:8080`. macOS Control Center (AirPlay Receiver) binds `:5000` and cannot reliably be freed on evaluator machines. `:8080` is the de-facto web dev port and doesn't clash. Internal container port stays `:8080` (the .NET image default; no `ASPNETCORE_URLS` override needed — avoids the "overriding HTTP_PORTS" warning).
- **`.env` in the image build context vs excluded**: excluded via `.dockerignore`. `.env` holds credentials; no reason to let it land in an image layer. Compose reads `.env` from the host, substitutes into the container env at runtime.

## Trade-offs

- **Migrate-on-startup** couples schema changes to deployments and makes it harder to roll back. Acceptable for a single-writer take-home; would be wrong in production.
- **Installing `curl` in the runtime image** adds a small attack surface. Acceptable for a dev image; production build would drop it in favour of a `/health` endpoint + `dotnet` TCP probe or a distroless variant.
- **Port `:8080` instead of `:5000`** breaks the muscle-memory of any .NET dev expecting the default Kestrel port. Documented in README/CLAUDE.md; follow-on issues keep `:8080` consistently.
- **`.env.example` password placeholder is obviously weak** (`change-me-in-env`). The evaluator is expected to copy to `.env` and leave dev defaults; not a security stance.

## Status / Next

- `docker compose up --build` brings up both services to `healthy`; `curl http://localhost:8080/api/events` returns `404` (expected — no endpoints mapped yet; ticket only requires "any valid HTTP response").
- API startup throws `InvalidOperationException` if `ConnectionStrings:Postgres` is missing — confirmed by omitting the env var locally. This is the "reads ConnectionStrings__Postgres from configuration" acceptance criterion satisfied minimally.
- No EF Core / Npgsql package yet — deliberate, that lands with the first backend-dev issue (DbContext + entity).
- Issue body updated to reflect `:8080` host port (originally said `:5000` — corrected alongside this commit).

**Reviewer should verify:**
1. `docker compose config` is clean
2. `.env` is gitignored (`git check-ignore .env` returns `.env`)
3. No secrets in `docker-compose.yml` — all values reference `${VAR}`
4. `depends_on` uses the long form (`condition: service_healthy`), not the short list form
5. API image runs as non-root (`docker compose exec api whoami` → `app`)

**Next issue (suggested):** backend-dev adds `Npgsql.EntityFrameworkCore.PostgreSQL`, the `TrafficMonitor.Domain.TrafficEvent` entity, `TrafficDbContext`, and the first migration.

---

## Session handoff — 2026-04-16 (end of session)

### Where things stand

- **Branch:** `feature/1-docker-compose-baseline` (pushed to origin)
- **PR #2:** open — https://github.com/yklau1989/sample_traffic-monitor-api/pull/2
- **Issue #1:** all acceptance criteria ticked in the body; reviewer agent signed off in a PR comment; waiting on Martin's human review before merge.
- **Commits on branch:**
  - `a8c8c6e` chore: align project agents with CLAUDE.md
  - `1896fb1` feat(#1): docker compose baseline for local dev environment
  - `ebf4428` docs(#1): add setup + quick-start README

### What actually got done this session

1. Revised `CLAUDE.md` — tightened handoff model (reasoning-log-only, no separate `docs/handoff/`), added Session Resume protocol at the top, fixed frontend path (`frontend/` at repo root, not `src/TrafficMonitor.Frontend/`), added Running Locally section, added Codex hybrid workflow detail.
2. Wrote the six `.claude/skills/*.md` files from scratch (codex-review, cqrs-light, csharp-clean-architecture, docker-compose, ef-core-patterns, sse-channel) — all derived from the Key Design Rules.
3. Rewrote every `.claude/agents/*.md` — `develop-planner`, `backend-dev`, `frontend-dev`, `infra-dev`, `reviewer` — removing Skill Library drift and inlined extra agents (collapsed `reviewer.md` from api-reviewer + frontend-reviewer → one; dropped `database-admin` from `infra-dev.md`).
4. Wrote `docs/architecture.md` (was empty).
5. Created GitHub issue #1 via planner conventions (`[planner]` title prefix + `agent:planner` label).
6. Implemented #1 — `docker-compose.yml`, multi-stage `Dockerfile`, `.env.example`, `.dockerignore`, `.gitignore` additions, stripped `Program.cs` to strict minimum with a connection-string guard, README rewritten.
7. Proof of work: both services `healthy`, `curl /api/events` = 404 (reachable), clean `down -v`.
8. Reviewer agent posted verdict **READY FOR MARTIN** on PR #2.

### Open when the next session starts

- **Martin's review of PR #2.** If approved → merge → close issue #1 → plan issue #2 (first backend-dev issue: Npgsql + DbContext + TrafficEvent entity + initial migration).
- Anything Martin flags on the PR will need follow-up commits on the same branch.

### Known landmines for future-me

- **macOS `:5000`** is held by Control Center (AirPlay Receiver). Don't suggest `:5000` as a host port on this project — we're on `:8080`.
- **`.gitignore` `[Ll]ogs/` rule** silently swallows anything matching. Kept an explicit `!docs/logs/**` un-ignore; do not remove without re-testing that reasoning logs stay tracked.
- **`.claude/settings.local.json`** is auto-updated by the harness when permissions are granted. It'll show up as modified after most sessions. Not business logic — leave it alone unless the user stages it.
- **`.NET 10` image tag `10.0` works** as of 2026-04 — verified by a successful build.
- **`develop-planner.md` was empty at the start of this session**; Martin populated it mid-session. Don't trust a stale `wc -l` read.

### Memory written this session

- `feedback_git.md` — never commit to main/master; feature-branch commits pre-authorized
- `feedback_issue_workflow.md` — on finishing a milestone: comment on the issue first, then open the PR

