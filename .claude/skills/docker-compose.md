---
name: docker-compose
description: Docker Compose conventions for this repo — service layout, Postgres healthcheck, API startup ordering, migration strategy, env wiring.
---

# Docker Compose

Goal: `docker compose up --build` starts everything and the evaluator sees events flowing within ~30s.

## Services

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: traffic_monitor
      POSTGRES_USER: traffic
      POSTGRES_PASSWORD: traffic
    ports: ["5432:5432"]
    volumes: ["postgres-data:/var/lib/postgresql/data"]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U traffic -d traffic_monitor"]
      interval: 2s
      timeout: 3s
      retries: 10

  api:
    build:
      context: .
      dockerfile: src/TrafficMonitor.Api/Dockerfile
    environment:
      ConnectionStrings__Postgres: Host=postgres;Database=traffic_monitor;Username=traffic;Password=traffic
      ASPNETCORE_ENVIRONMENT: Development
      EventGenerator__Enabled: "true"
    ports: ["5000:8080"]
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres-data:
```

## Key decisions

1. **`depends_on: condition: service_healthy`** — API waits for Postgres to actually accept connections, not just for the container to start. Without this, EF retries on cold start look like real failures in the logs.
2. **Migrations run on API startup** — `dbContext.Database.Migrate()` in `Program.cs` behind an `if (app.Environment.IsDevelopment())` guard. No separate migration container for a take-home; overkill.
3. **Env vars over `appsettings.Production.json`** — connection string via `ConnectionStrings__Postgres` (double underscore = config section separator). Keeps secrets out of the image.
4. **Bind to `:8080` inside, `:5000` outside** — the image runs as non-root, can't bind <1024.
5. **Fake event generator is a `BackgroundService` inside the API**, toggled by `EventGenerator__Enabled`. Not a separate service — it's meant to demonstrate the ingest path via HTTP, not a separate deployment artifact.

## Dockerfile pattern (multi-stage)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.slnx ./
COPY src/ src/
RUN dotnet publish src/TrafficMonitor.Api -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "TrafficMonitor.Api.dll"]
```

- Restore + publish in one `RUN` — the SDK image caches NuGet between builds anyway.
- Use the `aspnet` runtime image, not `runtime` — saves pulling ASP.NET stack separately.

## When things go wrong

- **API exits immediately with connection refused** — healthcheck isn't wired, or the compose file has `depends_on: [postgres]` (short form, ignores health). Use the long form.
- **Migrations fail on startup** — someone committed two parallel migrations. Check `Infrastructure/Migrations/` for duplicate `__ModelSnapshot` divergence.
- **Port 5432 clash** — developer has a local Postgres. Either stop it or remove the `ports:` mapping on the `postgres` service (API still reaches it via the compose network).

## Production-ish concerns (explicitly out of scope)

Not addressed here because this is a take-home: TLS termination, secrets manager, separate migration job, readiness vs liveness probes, resource limits. Call these out in `docs/deployment.md` as "known gaps."
