# Issue #11 — redo #5 persistence slice via codex-first workflow

## Decision

Split #11 into two Codex runs along the Domain / Infrastructure boundary. **This log covers sub-slice 1 only — Domain value objects (`BoundingBox`, `Detection`) + the `TrafficEvent` entity + xUnit tests.** No EF Core, no migration, no repository. Those land in sub-slice 2 as a separate Codex invocation on a different branch or the same branch as a follow-up.

Pass 1 accepted. Codex produced the whole set (6 new files, 1 deleted scaffold, 1 csproj reference added) on the first write-mode turn. `dotnet build` clean (0/0), `dotnet test` 30/30 green. The orchestrator did not modify Codex's output.

## Options considered

- **Brief the whole persistence slice (Domain + EF + migration) in one Codex run** — rejected. Larger briefs mean more to go wrong per pass, and the 2-pass budget gives less room to recover. The #10 pilot proved the workflow for 10-line enums; this sub-slice (~200 lines across 6 files) is the next rung. EF + migration + repository + compose-Postgres verification belong in their own run so each acceptance boundary is clean.
- **Write the brief as a loose outline and let Codex fill in the domain shape** — rejected. Codex is the implementer, not the architect. The brief fixes the aggregate shape (private setters, backing field for detections, UTC-only timestamps, etc.) before Codex starts typing; otherwise Codex has to guess at invariants that belong to Domain, and reviews become re-architecting.
- **Invoke Codex via the `codex:rescue` Skill tool** — attempted first, the harness sat on a per-invocation approval prompt while Martin stepped away and the call was cancelled. Switched to the companion script directly (`node codex-companion.mjs task --write --fresh`). Same runtime, different entry point, no approval interstitial. Noted for future slices.

## Trade-offs

- **Sub-slice 1 ships a Domain model with no consumer yet.** The entity is internally consistent (private setters, UTC invariants, immutable value objects) but no Application handler or repository uses it. That debt lands in the next slice; until then the entity is reachable only from tests.
- **Validation throws `ArgumentException` uniformly.** The brief asked for `ArgumentException` across the board for simplicity; `ArgumentNullException` for the genuine-null cases might read slightly better, but mixing exception types in a small domain isn't worth the cognitive load. Revisit if a handler needs to distinguish.
- **Minor residue from the generated code** (deliberately not sent through Pass 2 per the iteration-budget rule):
  - `TrafficEvent.Id = default;` is a no-op (default for `int` is already 0). Harmless.
  - `BoundingBox` / `Detection` re-declare their positional-record properties with init-time validators. Valid C# pattern, works, tests pass — but some readers prefer an explicit constructor body. Non-blocking.

## Status / Next

### Verified

- `dotnet build --nologo -v q` at repo root — 0 warnings, 0 errors.
- `dotnet test --nologo -v q` at repo root — 30 tests, 30 pass, 0 fail, 0 skip.
- Files exactly as briefed:
  - `src/TrafficMonitor.Domain/ValueObjects/BoundingBox.cs`
  - `src/TrafficMonitor.Domain/ValueObjects/Detection.cs`
  - `src/TrafficMonitor.Domain/Entities/TrafficEvent.cs`
  - `tests/TrafficMonitor.Tests/Domain/{BoundingBox,Detection,TrafficEvent}Tests.cs`
  - `tests/TrafficMonitor.Tests/TrafficMonitor.Tests.csproj` — adds `<ProjectReference>` to Domain, nothing else.
  - `tests/TrafficMonitor.Tests/UnitTest1.cs` — removed (scaffold).
- `TrafficMonitor.Domain.csproj` remains dependency-free (no NuGet, no refs).

### Codex job IDs

- `task-mo36rakw-ucg0vz` (thread `019d9c80-2629-7be2-b925-97b4998eb5f7`) — Pass 1, `--write --fresh --effort medium`. Duration: 4m 24s (running 1m 45s, verifying 2m 39s — most of the wall time was internal verification, not generation).

**Iteration budget spent:** 1 of 2.

### Next

- **Sub-slice 2 (same issue #11):** EF Core `DbContext`, EF configuration using `OwnsMany(...).ToJson()` for detections, snake_case naming convention, unique index on `EventId`, repository interface in `Application`, repository implementation in `Infrastructure`, initial migration, `dotnet ef database update` against compose Postgres. Separate Codex brief; keep the same 2-pass budget fresh for that run.
- **Suggested ordering for sub-slice 2:** Application layer first (repository interface only, no DbContext dependency), then Infrastructure (DbContext + config + impl + migration). Or bundle both into one Codex pass if Martin prefers a single transaction. Decide at kickoff.

### Landmines discovered

1. **`codex:rescue` Skill invocation requires an approval prompt that will sit indefinitely if the operator steps away.** Direct invocation via `node codex-companion.mjs task --write --fresh "<brief>"` bypasses the prompt (goes through Bash, which is already authorised for the session). Use the companion-script path by default for background runs; reserve the Skill invocation for interactive foreground runs.
2. **Verification phase dominates wall-clock time.** Codex spent ~2m 40s in "verifying" after only 1m 45s of actual generation. Mid-task polls can look stalled — they aren't. Poll with a 15–20s cadence and trust the phase label.
3. **`codex-companion.mjs result <id> --json` has no narrative `message` field for `task`-kind jobs** (unlike `review`). The "output" of a task is the filesystem diff itself, not a text summary. Verify by `git status` + `dotnet build` + `dotnet test`, not by reading a Codex message.

---

## Sub-slice 2 — Infrastructure (EF + DbContext + repo + migration) — 2026-04-18, **PAUSED AT 2-PASS BUDGET**

### Decision (sub-slice 2 — paused)

Two Codex passes spent. Pass 1 produced a clean Infrastructure layer (DbContext, config, repo, DI extension, design-time factory) with 0/0 build. Pass 2 converted `Detection` + `BoundingBox` from positional records to classes to fix an EF constructor-binding error on the nested value object. That fix worked — 30/30 domain tests still green, build still clean — but `dotnet ef migrations add InitialCreate` now fails on a *new* error: `EFCore.NamingConventions` blindly applies snake_case *column* names to properties inside a `ToJson()` owned collection, and EF Core 10 rejects the combination. **Paused per `.claude/rules/escalation.md`** — Pass 3 forbidden without Martin's explicit go-ahead.

### What has been committed to `feature/11b-infrastructure-persistence` at handoff

1. **Pass 1 — Infrastructure scaffold** (one commit): `TrafficMonitorDbContext`, `TrafficEventConfiguration`, `TrafficEventRepository`, `DependencyInjection.AddInfrastructure`, `TrafficMonitorDbContextFactory` (design-time, needed because `dotnet ef` couldn't boot the minimal-API host), csproj package / project-reference wiring across Application / Infrastructure / Api, `Program.cs` call to `AddInfrastructure`, `Class1.cs` removed from both scaffolded projects.
2. **Pass 2 — Domain refactor** (one commit): `BoundingBox` and `Detection` rewritten from positional records to classes with private parameterless ctor + validated public ctor + `IEquatable<T>` overrides (`Equals`, `GetHashCode`). Same 30 tests still pass — observable semantics preserved.

### What is **not** in the repo yet

- `src/TrafficMonitor.Infrastructure/Migrations/` — never generated, because `dotnet ef migrations add InitialCreate` fails before writing any file.
- No integration tests; deferred to a later slice anyway.

### The blocker — verbatim error

```
Property 'Detection.BoundingBox#BoundingBox.Height' cannot have both a column name
('bounding_box_height') and a JSON property name ('Height') configured.
Properties in JSON-mapped types should use JSON property names, not column names.
```

Trigger: `TrafficEventConfiguration.cs` has `OwnsMany(e => e.Detections, owned => { owned.OwnsOne(d => d.BoundingBox); owned.ToJson(); });`, and `DependencyInjection.AddInfrastructure` calls `.UseSnakeCaseNamingConvention()`. The naming convention applies snake_case column names to *every* property, including the properties inside the JSON-owned `BoundingBox`, which EF Core 10 forbids.

### Codex job IDs (sub-slice 2)

- `task-mo37zirn-2vkum9` — Pass 1 (`--write --fresh --effort medium`). 8m 54s. Thread `019d9cd0-xxxx...`. Produced the Infrastructure layer cleanly; migration-generation failed on the record ctor binding error.
- `task-mo38k606-33u71j` — Pass 2 (`--write --resume`). 8m 0s. Converted records → classes. Constructor-binding error gone; naming-convention / JSON-column-name error surfaced instead.

**Iteration budget for sub-slice 2:** 2 of 2 spent.

### The four options on the table — **Martin chooses**

| Option | What it changes | Pros | Cons |
|---|---|---|---|
| **A (Martin-recommended?)** Drop `UseSnakeCaseNamingConvention()`; remove the `EFCore.NamingConventions` package; hand-set `HasColumnName("event_id")` etc. on the non-JSON properties inside `TrafficEventConfiguration.cs`. | Minimal package footprint, keeps `OwnsMany.ToJson()` intact (the spec's approach). | ~10 extra lines of config. Less automatic. |
| **B** Keep the convention; drop `OwnsMany.ToJson()`; use a `ValueConverter<List<Detection>, string>` on a shadow/backing property with `System.Text.Json`. | Cleanest config. Sidesteps every EF-JSON-owned-collection quirk. | Deviates from `docs/architecture.md` ("Detections stored as JSONB via `OwnsMany + ToJson()`"). Needs a reasoning-log justification. |
| **C** Keep both; write a custom naming convention that skips JSON-owned properties. | Spec intact, convention intact. | Most bespoke code; highest corner-case risk. |
| **D** Explicitly approve a Pass 3 against the 2-pass rule, targeting `.HasJsonPropertyName()` overrides on each JSON property. | Might succeed in one more shot. | Breaks the iteration budget — exists as a rule precisely because Pass 3s historically burn quota without landing. |

### Suggested next step on resume

If **A**: this is a fresh Pass 1 of a new micro-slice ("wire snake_case manually"). Brief Codex to (a) remove `UseSnakeCaseNamingConvention()` from `DependencyInjection.cs`, (b) drop the `EFCore.NamingConventions` package from `Infrastructure.csproj`, (c) add explicit `HasColumnName("...")` calls in `TrafficEventConfiguration.cs`, (d) set `.ToTable("traffic_events")`, (e) run `dotnet ef migrations add InitialCreate`. Fresh 2-pass budget on that slice.

If **B**: this is a persistence-strategy pivot. Open a small planning comment on #11 noting the deviation, update `docs/architecture.md` to reflect the ValueConverter approach, then brief Codex with the converter shape and the expected column names.

If **C**: higher-risk; probably want to write the custom convention ourselves (it's ~40 lines) rather than brief Codex on a corner-case EF API.

If **D**: rules are rules — prefer one of A/B/C over this.

---

## Session handoff — 2026-04-18 (end of day, pre-sleep)

### State of play in one paragraph

PR #13 (sub-slice 1 of #11) is merged to `main`. Sub-slice 2 is paused on `feature/11b-infrastructure-persistence` with two commits pushed: Pass 1 Infrastructure scaffold + Pass 2 Domain refactor. The branch compiles cleanly and 30/30 Domain tests pass, but no migration has been generated and no PR is open yet. The decision required is **which of A/B/C/D above** to pursue next session. Every option starts a fresh Codex budget because it's a new scope.

### What to read first when resuming

1. This file, under **"The four options on the table"** — pick one.
2. The latest commit on `feature/11b-infrastructure-persistence`:
   - `refactor(#11): convert Detection and BoundingBox to classes (codex sub-slice 2 pass 2)`
   - `feat(#11): add persistence infrastructure (codex sub-slice 2 pass 1)`
3. `src/TrafficMonitor.Infrastructure/Persistence/Configurations/TrafficEventConfiguration.cs` — where the naming-convention collision happens.
4. `src/TrafficMonitor.Infrastructure/DependencyInjection.cs` — where `.UseSnakeCaseNamingConvention()` is wired.
5. `src/TrafficMonitor.Domain/ValueObjects/BoundingBox.cs` and `Detection.cs` — the class rewrites Codex produced. Worth a quick eyeball; member order follows the new `.claude/rules/code-style.md` rule (properties above constructors).

### Open issues / PRs at handoff

- **#11** — OPEN. The "redo #5 persistence slice" umbrella. Sub-slice 1 landed in PR #13; sub-slice 2 is what this reasoning log documents.
- **PR #13** — MERGED. Sub-slice 1 (Domain value objects + entity).
- No PR open for sub-slice 2 yet. When the path is chosen and the migration lands, open a single PR that bundles Pass 1 + Pass 2 + the chosen-path resolution.

### Usage at handoff

- **Claude**: ~87% context used in this session.
- **Codex**: 3 `task` jobs this session (sub-slice 1 Pass 1, sub-slice 2 Pass 1, sub-slice 2 Pass 2). Total runtime ~21 minutes. No quota warnings. `authMethod: "chatgpt"` throughout.

### Landmines for future-me

- **Watchdog "phase stall" was too aggressive** — it fired after 3 min in `verifying`, but Codex was still actively editing files (mtime on the domain value objects was inside the 3-min window). Consider either (a) loosening the stall cap when phase bounces between `verifying` / `investigating` / `running`, or (b) combining the file-system-mtime signal with the phase signal so active edits reset the stall counter.
- **Codex does exercise judgment against the brief.** Pass 2 kept the `owned.OwnsOne(detection => detection.BoundingBox)` line in `TrafficEventConfiguration.cs` even though the brief told it to remove it — Codex's reasoning was "still needed for EF to recognize the nested class inside the JSON-owned collection", which turned out to be correct. Don't over-prescribe when Codex has a strong reason to deviate; the reviewer (Claude / Martin) catches real deviations.
- **`tr -d '\000-\037'` on macOS must be prefixed with `LC_ALL=C`** — otherwise locale-aware byte handling mis-processes multi-byte UTF-8 in the payload and the effective strip is no-op. Saved to memory as the `shell JSON parsing` feedback.
- **`.claude/settings.local.json`** continues to show as modified — harness drift, do not commit.


---

## Sub-slice 2 resolution — Option B (ValueConverter + JSONB) — 2026-04-18

### Decision

Replaced `OwnsMany(...).ToJson()` with a `ValueConverter<List<Detection>, string>` wired to `HasColumnType("jsonb")` on the `_detections` backing field. This sidesteps the EF Core 10 + `UseSnakeCaseNamingConvention()` collision that blocked `dotnet ef migrations add InitialCreate`. The JSONB storage guarantee is preserved. Two additional changes were required: `[JsonConstructor]` on the public constructors of `Detection` and `BoundingBox` (so `System.Text.Json` can deserialize through the validated constructor, not the private parameterless one), and `builder.Ignore(e => e.Detections)` in `TrafficEventConfiguration` (so EF does not register the `Detections` navigation property as an entity relationship alongside the ValueConverter-backed `_detections` field).

### Options considered

(Full option table is in the "Sub-slice 2 — PAUSED" section above. Martin chose B.)

- **A (drop naming convention, hand-set column names)** — rejected by Martin in favour of B.
- **B (ValueConverter + JSONB)** — chosen. Cleanest config; sidesteps every EF JSON-owned-collection quirk; naming convention stays.
- **C (custom naming convention that skips JSON-owned properties)** — rejected; highest bespoke-code risk.
- **D (approve a Pass 3 against the old 2-pass budget)** — ruled out by the iteration-budget rule.

### Trade-offs

- `OwnsMany.ToJson()` is gone. The domain model (`Detection`, `BoundingBox`) is no longer EF-first-class; it travels as a JSON blob and is deserialized on read. This means: (a) no `JSONB` path-query via EF LINQ — raw SQL would be needed for JSON subfield queries, which are out of scope; (b) `Detection` and `BoundingBox` now carry a `[JsonConstructor]` attribute, which is the only EF/serialization concern that leaked into Domain. The attribute is metadata-only and does not change observable behaviour or tests.
- `UseSnakeCaseNamingConvention()` is retained, which is the correct outcome — the whole point of Option B was to avoid dropping it.
- `ValueComparer<List<Detection>>` uses serialization-based equality (serialize both sides, compare strings). This is slightly heavier than field-by-field comparison but is correct for change-tracking and eliminates false positives on structural equality edge cases.

### Status / Next

**Verified by orchestrator (Codex sandbox cannot run migrations or reach Postgres):**

- `dotnet build --nologo -v q` — 0 warnings, 0 errors (verified twice: after Pass 1 and after Pass 2).
- `dotnet test --nologo -v q` — 30/30 pass (verified twice: after Pass 1 and after Pass 2).
- `dotnet ef migrations add InitialCreate --project src/TrafficMonitor.Infrastructure --startup-project src/TrafficMonitor.Api` — exits 0; migration files written to `src/TrafficMonitor.Infrastructure/Migrations/`.
- Migration content verified: table `traffic_events`, column `detections jsonb`, unique index `ix_traffic_events_event_id`, all column names snake_case.
- `dotnet ef database update` — succeeded against compose Postgres. Schema verified via `\d traffic_events`: correct column types, PK `pk_traffic_events`, unique index present.
- `docs/architecture.md` — Persistence section and Key design trade-offs table updated to reflect ValueConverter approach.

**Open / next:**
- No PR open yet for sub-slice 2. Martin opens the PR when ready.
- Next issue after #11: Application layer — commands, queries, handlers, repository implementation (the repo interface already exists in Application from sub-slice 2 Pass 1).

### Codex job IDs (Option B resolution — fresh 2-pass budget)

- **Pass 1** — `codex exec --full-auto` launched ~12:16 JST 2026-04-18. Produced ValueConverter config, `[JsonConstructor]` attributes, architecture.md update. Build 0/0, tests 30/30. Migration failed: EF discovery registered `Detections` navigation as entity type.
- **Pass 2** — `codex exec --full-auto` launched ~12:33 JST 2026-04-18. Added `builder.Ignore(e => e.Detections)` to `TrafficEventConfiguration`. Migration generated successfully.

**Iteration budget spent (Option B resolution):** 2 of 2.

