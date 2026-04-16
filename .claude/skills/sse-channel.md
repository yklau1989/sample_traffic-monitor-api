---
name: sse-channel
description: Server-Sent Events implementation using Channel<T> as the in-process fan-out for real-time event streaming to dashboard clients.
---

# SSE + Channel<T>

Real-time feed for the dashboard. Single in-process broker, no Redis, no SignalR — the take-home is a single instance.

## Topology

```
POST /api/events  ──▶  IngestHandler  ──▶  Repository.Save
                                      │
                                      └──▶  IEventBroadcaster.Publish(dto)
                                                       │
                                            ┌──────────┴──────────┐
                                            ▼                     ▼
                                   Channel<T> per client   Channel<T> per client
                                            │                     │
                                   GET /api/events/stream   GET /api/events/stream
```

One unbounded-ish `Channel<TrafficEventStreamDto>` **per connected client**, registered with the broadcaster on connect, removed on disconnect.

## Broadcaster interface

```csharp
public interface IEventBroadcaster
{
    ChannelReader<TrafficEventStreamDto> Subscribe(CancellationToken ct);
    ValueTask PublishAsync(TrafficEventStreamDto dto);
}
```

- `Subscribe` creates a new `Channel`, stores the `ChannelWriter` internally, returns the `ChannelReader`. On `ct` cancellation, completes the writer and removes it.
- `PublishAsync` iterates all writers, calls `TryWrite` — never awaits a slow reader (drop if bounded + full).

## Channel configuration

```csharp
Channel.CreateBounded<TrafficEventStreamDto>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.DropOldest,
    SingleReader = true,
    SingleWriter = false,
});
```

- Bounded (100) — a stalled client doesn't blow memory.
- `DropOldest` — dashboard cares about *recent* events; old backlog is noise.
- `SingleReader = true` — each channel is drained by one SSE handler.
- `SingleWriter = false` — broadcaster writes from whatever request thread ingested the event.

## Publish timing

Publish **after** `SaveChangesAsync` commits. Never inside the transaction — a failed commit + a published event means dashboards show phantom data.

```csharp
await _repo.AddAsync(evt, ct);
await _uow.SaveChangesAsync(ct);
await _broadcaster.PublishAsync(streamDto);  // only reached on success
```

## SSE endpoint

`GET /api/events/stream`:

1. Set headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `X-Accel-Buffering: no` (nginx).
2. `await foreach (var dto in reader.ReadAllAsync(ct))` — write `data: {json}\n\n`, flush.
3. Heartbeat every ~15s — write `: ping\n\n` comment line so proxies don't close the idle socket.
4. On `OperationCanceledException` (client disconnect), let it unwind; the broadcaster's `ct` handler removes the writer.

## DTO shape

`TrafficEventStreamDto` is **not** the same as `TrafficEventListItemDto`. Stream payload is optimised for "pop a toast + insert a row" — usually: `eventId`, `cameraId`, `eventType`, `severity`, `occurredAt`, `detectionSummary`. Keep it small; full detail is fetched via `GET /api/events/{eventId}` when the operator clicks.

## What to avoid

- Don't use a single shared channel — one slow client would block everyone.
- Don't publish inside the handler before saving — on DB failure, subscribers see non-existent events.
- Don't forget the heartbeat — some proxies kill sockets idle >30s.
