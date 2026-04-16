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
