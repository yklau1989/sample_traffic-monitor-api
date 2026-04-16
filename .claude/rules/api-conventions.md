# API conventions

Binding conventions for the HTTP surface. The endpoint list is in `docs/api-reference.md`; this file is the shape, status-code, and error rules that every endpoint must honour.

## URL shape

- Base path `/api/`. Resources are plural nouns: `/api/events`, `/api/events/{eventId}`, `/api/events/stream`.
- Path parameters are the **external UUID** (`eventId`), never the internal `int Id`.
- Query parameters are `camelCase`: `?cameraId=...&severity=high&from=2026-01-01T00:00:00Z`.
- No verbs in paths. Use HTTP methods.

## Methods and status codes

| Method | Path | Success | Duplicate / special | Client error | Server error |
|---|---|---|---|---|---|
| `POST` | `/api/events` | `201 Created` + `Location` | `200 OK` on duplicate `event_id` | `400` validation, `422` domain | `500` |
| `GET` | `/api/events` | `200 OK` | — | `400` bad filter | `500` |
| `GET` | `/api/events/{eventId}` | `200 OK` | `404` unknown id | — | `500` |
| `GET` | `/api/events/stream` | `200 OK` (SSE) | — | — | `500` |

- **Idempotency:** duplicate `event_id` is **200 OK with the existing resource**, not an error. The unique index on `EventId` is the source of truth (see `architecture.md`).
- `POST` responses include `Location: /api/events/{eventId}` on both 201 and 200.

## Request bodies

- JSON only. `Content-Type: application/json; charset=utf-8`.
- Unknown fields are **ignored** (System.Text.Json default). Do not enable strict deserialization — detection upstream may add fields.
- `event_id` is always client-supplied (UUID). Server never generates it.
- Timestamps are UTC ISO-8601 with `Z` suffix. Reject naive local timestamps at the controller boundary.

## Response bodies

- JSON, records serialised as objects. Property names `camelCase` (default .NET setting).
- Enums serialised as **strings** via `JsonStringEnumConverter`. No integer values on the wire.
- `null` is allowed for genuinely optional fields; do not emit `""` as a sentinel.
- List endpoints return `{ items: [...], total, page, pageSize }`, not a bare array. Makes pagination additive.
- **Never expose** internal `Id`, EF metadata, or raw DB column names.

## Errors

- Use **RFC 7807 Problem Details** (`application/problem+json`) for every non-2xx response.
- Populate `type`, `title`, `status`, `detail`, `traceId`, and — for validation errors — `errors: { "field": ["message"] }`.
- Never leak stack traces or SQL in `detail`. Log them server-side against `traceId`.

## Pagination, filtering, sorting

- Pagination: `?page=1&pageSize=50`. `pageSize` capped server-side (default 50, max 200).
- Filters are AND-combined. Unknown filter param → `400`.
- Sort: `?sort=-occurredAt` (prefix `-` for descending). One field only. Unknown field → `400`.

## SSE endpoint

- `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive`.
- Each message a single `data:` line with a JSON object, terminated by `\n\n`.
- Heartbeat every ~15s (comment line `: ping\n\n`) to keep proxies from closing the socket.
- No `Last-Event-ID` replay — out of scope per `architecture.md`.

## Versioning

- No versioning until a breaking change is needed. When it is: `/api/v2/events`, and keep `/api/events` alive for one release.
