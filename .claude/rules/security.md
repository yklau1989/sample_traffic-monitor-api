# Security rules

Take-home scope — no auth, no multi-tenant, no TLS termination here (all listed as out-of-scope in `architecture.md`). The rules below are the hygiene that still applies and that the evaluator can spot in one pass.

## Secrets

- **No secrets in the repo.** Ever. That includes: DB passwords, API keys, connection strings with credentials, cloud tokens.
- `.env` is **gitignored** and is the only file that holds real credentials.
- `.env.example` ships placeholder values (`change-me-in-env`) so a fresh clone can `cp .env.example .env`.
- `docker-compose.yml` must reference secrets via `${VAR}` substitution — never inline a value.
- Before committing, grep the staged diff for `password=`, `secret=`, `key=`, `token=`. If the evaluator can see a credential in the diff, that's an automatic fail.

## Container hygiene

- API image runs as **non-root** (`USER app` in the Dockerfile — verified by `docker compose exec api whoami`).
- Runtime image installs only what healthcheck needs (`curl`). Nothing else.
- `.dockerignore` excludes `.env`, `.git`, `bin/`, `obj/`, `node_modules/`, secrets. A `.env` must never land in a built layer.

## Input validation

- Validate at the **controller boundary**, not deep in handlers. Return 400 with Problem Details (see `api-conventions.md`).
- Reject unparseable JSON, missing required fields, out-of-range enums, malformed UUIDs, naive timestamps.
- For `POST /api/events`: `eventId` must be a valid UUID; `occurredAt` must be UTC; `cameraId` must be non-empty.
- Bound string lengths on every inbound string field (default max 256 unless documented otherwise). Prevents trivial memory-pressure attacks.

## Data access

- **EF Core parameterised queries only.** No string concatenation into SQL. `FromSqlInterpolated` is allowed (it parameterises); `FromSqlRaw` with user input is not.
- Nullable reference types on — null-safety is part of security hygiene here, not only code style.

## Output / logging

- Never log raw request bodies that may contain operator PII (none expected, but apply the rule pre-emptively).
- Never log secrets, connection strings, or JWT contents (if auth is added later).
- Log `eventId`, `cameraId`, and HTTP status — enough to trace, not enough to leak.

## Error responses

- Problem Details only. No stack traces, no SQL, no EF Core internals in `detail`. Log those against `traceId` server-side.

## Dependencies

- Pin versions in `.csproj`. Do not use floating version ranges (`*`).
- When adding a NuGet package, check it's published by a known vendor (Microsoft, Npgsql, xUnit, etc.). Random packages need justification in the reasoning log.

## What auto-fails review

- Any credential, token, or non-placeholder password visible in the diff.
- `FromSqlRaw` with user input.
- `docker-compose.yml` with an inline secret.
- `.env` (the real one) committed.
- API image running as root.
- A controller that accepts a DTO without validation.
