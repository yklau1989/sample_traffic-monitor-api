# Issue #47 — Add Scalar UI for OpenAPI spec (Development only)

## Conversation trail

Martin's direct ask that set the direction:

> "I prefer Scalar as well haha / damn new issue"

My interpretation: Scalar was the preferred OpenAPI UI, so the implementation should stay lean and add it only where the existing ASP.NET Core OpenAPI document is already exposed.

The orchestrator then opened issue #47 with the inline implementation spec and briefed Codex for a single-pass implementation in `TrafficMonitor.Api` only: add the pinned `Scalar.AspNetCore` package, call `MapScalarApiReference()` inside the existing `if (app.Environment.IsDevelopment())` block, and write the reasoning log in the same pass.

## Decision

Added `Scalar.AspNetCore` to `TrafficMonitor.Api` and wired `app.MapScalarApiReference();` inside the existing Development-only block next to `app.MapOpenApi();`, leaving the existing `AddOpenApi()` registration and the rest of `Program.cs` unchanged.

## Options considered

- Swashbuckle: rejected because it adds a larger surface area and isn't needed when `Microsoft.AspNetCore.OpenApi` already produces the spec we want to display.
- ReDoc: rejected because there is no official NuGet package for this setup and it would require serving static assets manually.
- No UI, keep Postman-only: rejected because evaluators are expected to interact with the API in-browser, not only through imported collections.

## Trade-offs

Scalar is a third-party package, which is an added dependency, but it is a known vendor in the OpenAPI UI space and is justified for this narrow UI concern. The UI is only mapped in Development, so Production-mode evaluators running via Docker Compose will not see `/scalar/v1`. There is no auth on the UI, which is acceptable here because auth is explicitly out of scope in `docs/architecture.md`.

## Status / Next

`dotnet build` passes with 0 warnings and 0 errors. `dotnet test` is blocked in this Codex sandbox because VSTest cannot open its local communication socket (`System.Net.Sockets.SocketException (13): Permission denied`), so the failure is environmental rather than caused by the Scalar change. Manual verify via `dotnet run` to `http://localhost:5124/scalar/v1` is also blocked here because the sandbox does not expose a reachable localhost listener for browser-style checks; the next step outside the sandbox is to run with a Development connection string and confirm the UI shows the `POST /api/events` and `GET /api/events` operations. Next: reviewer pass, merge, then apply the same Scalar note to `README.md` on the separate branch that is already updating it.
