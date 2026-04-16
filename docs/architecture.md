# Architecture

## Problem

An AI video system watches highway cameras and detects incidents (debris, stopped vehicles, congestion). Each detection must reach a control-room dashboard within seconds so an operator can dispatch a safety team. This service is the ingestion + query + real-time feed layer between the video system and the dashboard.

## High-level flow

```
┌─────────────────────┐        POST /api/events          ┌──────────────────────┐
│  Detection system   │ ───────────────────────────────▶ │                      │
│  (AI video)         │                                  │                      │
└─────────────────────┘                                  │   Traffic Monitor    │
                                                         │        API           │
┌─────────────────────┐  GET /api/events (filter/page)   │                      │
│                     │ ◀─────────────────────────────── │                      │
│  Operator           │                                  │                      │
│  dashboard          │  GET /api/events/stream  (SSE)   │                      │
│  (HTML/JS)          │ ◀─────────────────────────────── │                      │
└─────────────────────┘                                  └──────────┬───────────┘
                                                                    │
                                                         ┌──────────▼───────────┐
                                                         │   PostgreSQL         │
                                                         │   (events + JSONB    │
                                                         │    detections)       │
                                                         └──────────────────────┘
```

A fake event generator runs inside the API as a `BackgroundService` and POSTs synthetic events, so the whole pipeline is observable end-to-end without a real video system.

## Layers (Clean Architecture)

```
Api ──▶ Application ──▶ Domain
 │           │
 └──▶ Infrastructure ──▶ Application ──▶ Domain
```

| Project | Responsibility |
|---|---|
| `TrafficMonitor.Domain` | Entities (`TrafficEvent`, `Detection`), enums (`EventType`, `Severity`), invariants. Zero dependencies. |
| `TrafficMonitor.Application` | Commands, queries, handlers, DTOs, repository **interfaces**. Orchestrates domain behaviour. |
| `TrafficMonitor.Infrastructure` | EF Core `DbContext`, repository **implementations**, migrations, outbound adapters. |
| `TrafficMonitor.Api` | Controllers, middleware, DI, SSE endpoint, fake event generator, composition root. |

Dependencies point inward. Domain has no knowledge of EF, HTTP, or DI.

## CQRS (light)

Same database, separated write/read paths:

- `Application/Commands/` — ingest, enforce idempotency, write via repository. Input: `TrafficEventInput`. Output: `{ eventId, wasDuplicate }`.
- `Application/Queries/` — list with filters, get-by-id, dashboard-shaped DTOs. Projections happen in the repository so EF emits narrow SQL.
- Write DTOs and read DTOs are **distinct types**, even when fields overlap.

Why: keeps handlers small, prevents write-side fields leaking into dashboard responses.

## Persistence

- Postgres + Npgsql + EF Core 10.
- Snake-case naming convention.
- **Internal `int Id`** for joins; **external `Guid EventId`** for the API and idempotency. API never exposes `Id`.
- `Detections` stored as **JSONB** via `OwnsMany(...).ToJson()` — value objects always loaded with their parent, never queried independently.
- Unique index on `EventId` enforces idempotency at the DB layer.

## Real-time feed

`GET /api/events/stream` is SSE. A single in-process broadcaster owns one bounded `Channel<T>` **per connected client**. After a successful `SaveChangesAsync`, the ingest handler publishes a stream DTO to all writers. Backpressure is handled by `DropOldest` — recent state matters, old backlog doesn't. Heartbeat every ~15s keeps proxies from closing idle sockets.

Single-instance design; no Redis / SignalR. Sufficient for the take-home.

## Key design trade-offs

| Decision | Chosen | Rejected | Why |
|---|---|---|---|
| Idempotency | Unique index on `EventId`, catch unique-violation | Pre-check SELECT | Race-free; one round trip in the happy path |
| Detections storage | JSONB via `OwnsMany.ToJson()` | Separate `detections` table | No independent query need, avoids joins, keeps aggregate intact |
| Real-time delivery | In-process `Channel<T>` + SSE | SignalR / Redis pub-sub | Single instance, no extra infra, easy to reason about |
| CQRS | Light (handlers + separate DTOs, same DB) | Full CQRS with read store | Over-engineered for the scale; preserves most of the readability win |
| Event generator | `BackgroundService` POSTing via HTTP | Direct DB seeding | Exercises the real ingest path; duplicates = free idempotency test |

## What's explicitly out of scope

Auth, multi-tenant, horizontal scaling, TLS termination, observability beyond basic logs, retries on publish failures, replay of missed events on reconnect. All would be needed in production; none are needed to show the design.
