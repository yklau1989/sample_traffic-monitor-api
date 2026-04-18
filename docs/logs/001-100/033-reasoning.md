# Issue #33 — log is not good

## Conversation trail

Martin opened issue #33 with:

> You used 11 instead of 011
> Also you are not capturing everything
>
> For example you should log this message and add your comments as well
>
> One more thing is I told you should be 001~100, 101~200. now your 001~010 only have 3 issues. しょぼりすぎる

Three distinct complaints, each needing a rule change:

1. **Filename `11-reasoning.md` instead of `011-reasoning.md`.** Previous rule said "zero-pad" implicitly by example, but the example was `003-reasoning.md` which could be read as "one-digit issues get padding; two-digit ones don't". My commentary: fair — the rule needs to be explicit. Zero-pad to three digits always. Auto-fail on review if not padded.

2. **Logs aren't capturing everything.** "You should log this message and add your comments as well." Martin wants the log to preserve both sides of the exchange — his verbatim directive + my commentary — not just the distilled decision. My commentary: this is a strong signal. The evaluator reads reasoning logs as the "AI tool usage artifact", and a log that reads "decided X" is a thin artifact. A log that quotes the user message and shows my interpretation and pushback is a thick artifact — you can see the thinking happening. Added a **Conversation trail** required section at the top of every log, ahead of Decision.

3. **Range grouping is too shabby** (しょぼりすぎる — "way too sparse"). 001-010 only has 3 issues, 011-020 only has 1. Martin specified "001~100, 101~200". My commentary: consistent with the project's actual issue density. The original 10-issue ranges were a premature structure from the very first reasoning-log commit; they multiplied directory count without helping discoverability. Consolidating to 100-issue blocks.

When he followed up in-session:

> for #33 yes please do it.

Confirmed go-ahead. No re-scope requested.

## Decision

Three changes land together on `feature/33-log-conventions`:

1. **Filename rule** in `.claude/rules/documentation.md`: zero-pad issue numbers to three digits (`011-reasoning.md`, not `11-reasoning.md`). Auto-fail on review if violated.
2. **New required section** — `Conversation trail` — added as section 1 of every reasoning log, ahead of `Decision`. Quotes Martin verbatim and records my response. Paraphrase is not allowed.
3. **Range grouping** changed from `001-010 / 011-020 / 021-030` to `001-100 / 101-200 / ...`. Existing four logs (001, 003, 010, 011) moved into `docs/logs/001-100/`; old `001-010/` and `011-020/` directories removed. `11-reasoning.md` also renamed to `011-reasoning.md` during the move.

`CLAUDE.md` updated to mirror the new directory structure and point at `.claude/rules/documentation.md` for the full required-sections list (avoids duplication drift).

## Options considered

- **Leave the old 10-issue ranges and just rename `11-reasoning.md`** — fixes #33's first complaint, ignores the other two. Rejected: Martin was explicit about all three.
- **Group by phase name instead of issue-number range** (`foundation/`, `core/`, `polish/`) — more semantic. Rejected: phase membership can shift as scope changes, but issue numbers are stable forever. Martin's proposed scheme is simpler.
- **Put the "Conversation trail" content inside the existing `Decision` section** — lighter structural change. Rejected: `Decision` is meant to be one paragraph; mixing quoted messages into it loses the "one decision, one paragraph" tightness. Separate section is cleaner.
- **Keep the `Status / Next` section optional** — it was already required; no change. Just clarifying here that only Conversation trail was added; nothing removed.

## Trade-offs

- Every future log gets longer because it now has to quote + comment, not just conclude. Offset: the 200-line cap still applies; if a log blows past it, the decision is an ADR. Logs shouldn't grow unboundedly.
- Moving logs rewrites git history of where the files live — anyone who bookmarked `docs/logs/001-010/001-reasoning.md` will get 404. No such bookmarks exist (solo repo, four files), so real cost is zero.
- Three-digit zero padding is a minor aesthetic asymmetry for issues <10, but the rule is now mechanical and unambiguous — cheaper to enforce than "use your judgement about padding".

## Status / Next

- Done: logs relocated (git rename, history preserved); `documentation.md` rule updated with filename + range + conversation-trail rules; `CLAUDE.md` directory tree updated; this log itself demonstrates the new format.
- Verified: `git status` shows 4 renames + 2 rule-file edits + this new log.
- Open: none for #33. PR will close the issue on merge.
- Next session: pick up issue #15 (Application ingest command + handler) under the demo priority saved in memory (`project_demo_priority.md`). Apply the new log convention starting with #15's log.
