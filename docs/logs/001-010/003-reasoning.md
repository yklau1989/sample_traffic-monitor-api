# Issue #3 — Populate `.claude/rules/` baseline

## Decision

Write five short, flat rules files — `api-conventions.md`, `code-style.md`, `documentation.md`, `git-workflow.md`, `security.md` — derived from CLAUDE.md, `docs/architecture.md`, and the two active auto-memories. Each file is a "must / must not" list; each stays under 100 lines per the issue's constraint. Deeper "how" material stays in `.claude/skills/` so the two directories do not overlap.

## Options considered

- **Rules as long explanatory docs vs short constraint lists**: chose short constraint lists. The existing `.claude/skills/*.md` already cover the "how" (CQRS, EF Core patterns, SSE channel, Docker Compose, Clean Architecture, Codex review). Duplicating that material under `rules/` would bloat the context every agent loads and blur the boundary between the two directories. Rules here read like a reviewer's checklist — scan in 30 seconds, catch violations by name.
- **Synthesise from scratch vs derive from existing sources**: derived. CLAUDE.md's "Code Conventions" and "Key Design Rules", `docs/architecture.md`'s CQRS / idempotency / SSE decisions, and the two feedback memories (`feedback_git.md`, `feedback_issue_workflow.md`) already represent the opinions in force. Rules files codify what's already true so an agent has one place to look without reading three documents.
- **One consolidated `rules.md` vs five files**: five files. Issue asked for the current file set (`api-conventions`, `code-style`, `documentation`, `git-workflow`, `security`), and the split reads well — an agent working on a Dockerfile only needs `security.md` + `documentation.md`, not the full pile.
- **Inclusion of the CQRS/idempotency gates in rules**: decided these stay in `architecture.md` + `skills/cqrs-light.md` + `skills/ef-core-patterns.md`. The rules files reference them (code-style.md: "Read vs write DTOs are distinct types — see `cqrs-light` skill"; api-conventions.md: "Duplicate `event_id` is 200 OK, not an error") without restating the rationale. Martin's concern was making sure CQRS + idempotency are *captured* in CLAUDE.md — verified they already are under Key Design Rules.

## Trade-offs

- **Terseness costs context.** A new reader won't learn *why* the repo forbids `.Result` from `code-style.md` alone — they'd have to open the skill or ASP.NET Core docs. Acceptable: the reader here is almost always an agent already loaded with CLAUDE.md and the relevant skill.
- **Five files means five places to update** when a convention shifts. Mitigation: cross-references are by section name, not line number, so rename-drift is the worst case.
- **Hard rules ("auto-fails review") risk ossifying** early choices. Kept the auto-fail list small and anchored to objectively bad patterns (`.Result`, inline secrets, `IQueryable` leaking, DTO-as-class-with-setters).

## Status / Next

- Five files written, each under 100 lines (longest: 60). `wc -l` verified.
- No overlap with `.claude/skills/` content — rules reference skills by filename where relevant.
- CLAUDE.md not modified; Martin confirmed CQRS + idempotency coverage there is sufficient.
- Branch: `feature/3-rules-files`, one commit expected.
- Next issue after merge: planner opens a backend-dev ticket for Npgsql + `TrafficDbContext` + `TrafficEvent` entity + initial migration (per #1's reasoning log suggestion).

**Reviewer should verify:**
1. No content duplicated from `.claude/skills/*.md`.
2. Every rule traceable to CLAUDE.md, `docs/architecture.md`, or an active memory.
3. Each file ≤100 lines.
4. No endpoint contract in `api-conventions.md` contradicts `docs/architecture.md`.
5. `git-workflow.md` aligns with the two active auto-memories (`feedback_git.md`, `feedback_issue_workflow.md`).
