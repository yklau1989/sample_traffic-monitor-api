---
name: codex-review
description: Structured review checklist for diffs produced by Codex, enforcing this repo's Key Design Rules and conventions. Used by the reviewer agent.
---

# Codex Review

Review a Codex-produced diff before it hits `main`. Output a findings list, not a rewrite.

## Input to this skill

- Issue number + link
- Diff (or branch name to `git diff main...{branch}`)
- Target layer stated in the handoff

## Review order (stop at first blocker, don't pile on)

1. **Scope** — did Codex change only what the issue asked for? Reject unrelated refactors, stray formatting churn, new dependencies not in scope.
2. **Layer boundaries** — apply `csharp-clean-architecture.md`:
   - No `DbContext` in Application / Api.
   - No `IQueryable<T>` escaping repositories.
   - Domain entities have private setters.
   - `using Microsoft.EntityFrameworkCore` absent from Application.
3. **Key Design Rules** (CLAUDE.md):
   - Idempotency: unique index on `EventId`, duplicate POST returns 200 not 500.
   - Internal `Id` not exposed in any DTO, route, or JSON field.
   - Detections via `OwnsMany(...).ToJson()`, not a new table.
   - Commands and queries in separate folders with separate DTOs.
   - Read DTOs are dashboard-shaped, not raw entity shape.
4. **Async correctness** — no `.Result`, `.Wait()`, `Task.Run` wrappers; `CancellationToken` plumbed through.
5. **Nullability** — no `!` bang operators in new code without a comment explaining why.
6. **Tests** — new handler → at least one unit test covering success + the one invariant it enforces.

## Output format

```
## Findings — issue #{n}

### Blockers (must fix before merge)
- [file:line] Problem. Why it violates {rule}. Suggested fix.

### Non-blockers (optional)
- [file:line] Minor issue. Trade-off.

### Accepted as-is
- Short note on any unusual choice Codex made that's actually fine, with reasoning — prevents the next reviewer from flagging the same thing.
```

After review, write the reasoning log to `docs/logs/{range}/{issue-number}-reasoning.md`:

- What Codex decided, what you changed or accepted, why.
- Alternatives considered (briefly).
- Open questions routed to Martin.

## What NOT to do in review

- Don't rewrite the code. Codex gets another turn with your findings.
- Don't nitpick naming unless it violates the "no abbreviations" rule.
- Don't request tests for pure DTOs, config records, or EF configurations.
- Don't invent new conventions mid-review — if it's not in CLAUDE.md or a skill, accept it or raise it as a separate issue.

## Escalation

Two review rounds failing on the same blocker → stop, write the state to the reasoning log, ping Martin. Don't let the review loop run indefinitely.
