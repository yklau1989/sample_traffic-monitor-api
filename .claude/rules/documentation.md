# Documentation rules

Two distinct audiences. Do not mix them.

## Evaluator-facing docs (hand-curated deliverables)

Kept current as the code evolves. Terse, accurate, opinionated.

| File | Purpose | Keep in sync with |
|---|---|---|
| `docs/architecture.md` | System design, Clean Architecture layers, CQRS rationale, trade-offs, what's out of scope | Any structural change |
| `docs/api-reference.md` | Every endpoint: method, path, request, response, status codes, examples | Any controller/DTO change |
| `docs/deployment.md` | Docker Compose, env vars, first-run commands, ports, operational notes | Any infra/compose/env change |
| `README.md` | Setup + quick-start only. One-screen friendly. | First-time run path |

Rules:

- Code changes **without** a docs update in the same PR are only acceptable if no evaluator-facing surface changed. Reviewer checks this.
- Diagrams: ASCII first, Mermaid if it genuinely helps. No binary image files in `docs/` unless unavoidable.
- No TODOs in evaluator docs. If it's not done, do not mention it.

## Reasoning logs (AI tool usage artifact)

One per issue at `docs/logs/{range}/{issue-number}-reasoning.md` — e.g. `docs/logs/001-100/011-reasoning.md`.

**Filename:** zero-pad the issue number to three digits (`011-reasoning.md`, not `11-reasoning.md`). Auto-fails review if not padded.

**Ranges:** group issues in blocks of 100 — `001-100`, `101-200`, `201-300`, ... Create the directory on first use. Do not sub-split into 10-issue buckets; keeps the log tree shallow and avoids near-empty directories.

**Required sections:**

1. **Conversation trail** — Martin's directives that shaped the issue, quoted verbatim, plus your response / interpretation / pushback for each. Do not paraphrase Martin. The evaluator (and future-you) needs to see *how* the decision was reached, not just the condensed outcome.
2. **Decision** — one paragraph: what was chosen.
3. **Options considered** — bulleted list of the alternatives; for each, why it lost.
4. **Trade-offs** — what this decision costs, explicitly.
5. **Status / Next** — what's verified, what's open, what the next issue should pick up. This is the cross-session handoff.

Optionally:

- **Reviewer verdict** — appended by the `reviewer` agent after diff review.
- **Session handoff** — appended at end-of-session with branch state, open PRs, landmines.

Rules:

- Every agent writes one before marking an issue done. No log, no merge.
- Write it in the same PR as the code it documents.
- Keep under ~200 lines. If a decision needs more, it's an ADR (see below).
- Capture the conversation, not just the conclusion. A log that only states "decided X" loses the evidence trail.

## Architecture Decision Records (optional)

For decisions that will outlive a single issue: `docs/decisions/NNNN-short-slug.md`. Use MADR-lite: Context, Decision, Consequences. Only create when the decision is genuinely load-bearing — most things belong in a reasoning log.

## What does NOT go in `docs/`

- Planning docs, checklists, scratchpads — use GitHub Issues or a plan, not a file.
- Session summaries — use the reasoning log's "Session handoff" section, not a new file.
- Memory content — that lives in the Claude auto-memory directory, not the repo.
- Meeting notes — out of scope.

## File-creation discipline

- Do **not** create new `.md` files reflexively. Before creating one, check there isn't already a place it belongs (architecture.md, api-reference.md, a reasoning log).
- `CLAUDE.md` is edited; it is not a dumping ground. Trim as much as you add.
