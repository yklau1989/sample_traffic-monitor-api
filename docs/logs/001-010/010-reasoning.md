# Issue #10 — Codex workflow pilot — Domain enums (EventType, Severity)

## Decision

Used the new codex-first workflow (rules + hooks + agent rewrite in #8) to deliver the two Domain enums as a dry-run of the full handoff. Codex produced both files and deleted the scaffold `Class1.cs` on the first successful write-mode turn; output matches the brief character-for-character; `dotnet build` is clean (0 warnings, 0 errors). Accepted as Pass 1 — the orchestrator did not modify Codex's output.

## Options considered

- **Brief Codex and let it write directly** — chosen. Tests the new `backend-dev` agent contract end to end.
- **Brief Codex, have it return a text diff, apply manually** — rejected. Hand-applying Codex's output is a grey-area violation of `.claude/agents/backend-dev.md` ("You do NOT write implementation code"). Preferring Codex-applies-its-own-patch keeps the invariant clean for future, larger slices.
- **Claude writes the enums directly** — rejected. The whole point of #8 is that Claude doesn't write implementation; skipping the Codex round on the very first opportunity would make the rules performative.

## Trade-offs

- **Discovered two Codex-plugin quirks** (captured below). The workflow needed small adjustments on the first try; a pure-text brief without sandbox flags produced a correct diff Codex couldn't apply.
- **This slice is trivially small** — ten lines of enum declarations. The build / tests / migrate loop Codex didn't exercise yet will land in the next slice (value objects + entity) or the one after (EF configs + migration). The pilot was scoped to validate the orchestration, not the full feature.

## Status / Next

- **Verified:** `dotnet build --nologo -v q` at repo root — 0 warnings, 0 errors.
- **Verified:** `ls src/TrafficMonitor.Domain/Enums/` lists exactly `EventType.cs` and `Severity.cs`.
- **Verified:** `src/TrafficMonitor.Domain/Class1.cs` is gone.
- **Codex job IDs:**
  - Thread `019d98be-4dca-7582-aa60-cfb110638e47` — Pass 1a, read-only sandbox (default), produced the right diff but could not write. Not counted against the iteration budget — it was a tooling misconfiguration by the orchestrator, not a Codex miss.
  - Thread `019d98c0-8615-7590-9967-122177bff47b` — Pass 1b, `--write --fresh`, applied the patch.
- **Iteration budget spent:** 1 of 2.
- **Next issue:** open a follow-up for the next persistence slice. My suggestion: value objects (`BoundingBox`, `Detection`) + domain entity (`TrafficEvent`) — still Domain-only, no EF. That keeps a second pilot scope small enough to trust while exercising more of the domain model. Save the EF layer for the slice after.

### Landmines discovered (for future briefs)

1. **Codex plugin defaults to read-only sandbox.** `node codex-companion.mjs task "..."` uses `sandbox: "read-only"` by default. To let Codex apply patches, pass `--write` (switches to `workspace-write`). Not mentioned in the `/codex:rescue` slash-command docs. The `backend-dev` agent should be updated to reference `--write` when briefing Codex for any implementation task.
2. **`--resume` inherits the original thread's sandbox.** Adding `--write` to a `--resume` call does not upgrade the sandbox of the existing thread — you have to start a fresh thread with `--fresh --write`. If Codex's first turn can't write, don't try to resume; start fresh.
3. **Codex plugin's `task` subcommand ≠ `/codex:rescue` slash command.** The slash command routes to the `codex:codex-rescue` subagent, which internally runs `node codex-companion.mjs task ...`. Both paths are valid; flags like `--write` live on the companion script. Using the companion directly is fine when we need explicit flag control.

## Reviewer verdict

_Pending — Martin decides whether to run the `reviewer` agent on this pilot or merge directly given the scope._
