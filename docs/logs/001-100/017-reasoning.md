# Issue #17 - API: POST /api/events controller (201 / 200 duplicate, Location header)

## Decision

`EventsController` dispatches to `IngestTrafficEventHandler`, maps `WasDuplicate` to `201`/`200`, and sets the `Location` header on both responses. `ArgumentException` mapping changed from `400` to `422` to separate shape errors from domain-rule errors. The temporary smoke routes were removed and replaced by `EventsControllerTests` backed by an in-memory repository stub.

## Options considered

- `CreatedAtAction` vs `Created(location, body)`: `CreatedAtAction` needs extra route-name wiring for no gain here; `Created(location, body)` keeps the take-home controller lean.
- Real Postgres in tests vs in-memory stub: Testcontainers work for #18, but this issue is API-layer only and the stub keeps the tests self-contained without a live database.
- Separate const strings for RFC URLs/titles vs inline values: Martin explicitly called out over-engineering, so the `422` title/type strings stay inline in the `ArgumentException` mapping.

## Trade-offs

- The in-memory stub does not exercise repository SQL. That belongs in the broader integration coverage tracked in #18.
- Mapping `ArgumentException` to `422` is opinionated, but it gives the evaluator a clean split between request-shape failures (`400`) and domain-rule violations (`422`).

## Status / Next

**Pass 1 (Codex session `019d9fff-3459-7882-9da0-9168b582d9dd`):** Controller, Program.cs, GlobalExceptionHandler, tests, api-reference.md, reasoning log all created. `dotnet build` clean (0 warnings, 0 errors). All 4 new `EventsControllerTests` returned 500 due to a .NET 10 breaking change.

**Pass 2 (Codex session `019d9fff-7763-7692-bec3-eec1097e4763`, resumed):** Applied `.NET 10` fix to `TrafficEventInput.cs` — changed `[property: Required]` to `[Required]` (parameter-level). The same `.NET 10` constraint affects `DetectionInput.cs` (property `BoundingBox`, `Label`) which was not fixed in Pass 2. `dotnet build` still clean. Tests: 7 failures remain.

**Failures after Pass 2:**

1. **4 new `EventsControllerTests`** — still 500. Root cause: `DetectionInput` still uses `[property: Required]` on `Label` and `BoundingBox`. .NET 10 ASP.NET Core throws `InvalidOperationException` at model binding: *"Record type 'DetectionInput' has validation metadata defined on property 'BoundingBox' that will be ignored."*

2. **3 pre-existing `HandleAsync_WithMissingCameraId_ThrowsValidationException`** — now regressed. The `[Required]` change on `TrafficEventInput.CameraId` parameter changes validation ordering: `Validator.ValidateObject` no longer catches empty/whitespace `CameraId` (because parameter-level `[Required]` is only enforced at the binding layer, not by `Validator.ValidateObject`), so it falls through to the `ArgumentException` guard in `ValidateInput`. Tests expected `ValidationException`; now get `ArgumentException`.

**2-pass budget exhausted.** Stopped per escalation rules — no Pass 3, no hand-patching.

**Hand-patch (Martin-sanctioned, Path 1 + option a):** .NET 10 breaking change forces the input records to parameter-level DataAnnotations; `Validator.ValidateObject` only reads property-level metadata and can't coexist. Resolution:
- `DetectionInput.cs`: `[property: Required]`/`[property: StringLength]`/`[property: Range]` → parameter-level `[Required]`, `[StringLength]`, `[Range]`.
- `IngestTrafficEventHandler.cs`: dropped both `Validator.ValidateObject` calls. Shape validation is now ASP.NET Core's job (400 via model binding); handler keeps domain-rule guards (`Guid.Empty`, non-UTC `OccurredAt`, null/whitespace `CameraId`, null `Detections`) as `ArgumentException` → 422.
- `IngestTrafficEventHandlerTests.cs`: the 3 `HandleAsync_WithMissingCameraId` theory tests now expect `ArgumentException` (renamed suffix accordingly).

`dotnet build`: 0 warnings / 0 errors. `dotnet test`: 41/41 green.

Next issues: GET endpoints (#20, #22), fake event generator (#27), compose wiring (#30), frontend list (#28).

## Conversation trail (Martin's verbatim quotes)

> "You two are using so many tokens for my 'interview question' to do things like [10 const strings for RFC URLs / titles / extension names]"
>
> "just merge them and be sure that we are heading to finish a human(me) testable api so that I can submit"
