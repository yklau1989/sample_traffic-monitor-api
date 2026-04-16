# Escalation rules

When to **stop and ask Martin** before taking the next action. These rules exist because every one of the listed conditions has cost us time or a revert in the past — the cheap thing is to pause for 30 seconds and confirm.

## Stop and ask — no exceptions

1. **Codex usage is close to its limit.** If `/codex:setup`, `/codex:status`, or any Codex companion output mentions remaining quota, rate limiting, or a "close to limit" warning, stop. Do not start a new `/codex:rescue`, `/codex:review`, or `/codex:adversarial-review` run until Martin confirms.
2. **Codex plugin / CLI is unreachable.** If `/codex:setup` reports `ready: false`, `auth.loggedIn: false`, or a `codex-companion.mjs` call errors out with auth / socket / transport failures, stop. Do not fall back to writing the implementation yourself.
3. **Any Docker or Docker Compose problem** while working on a non-infra task. Examples: `docker compose up` fails, a container won't become healthy, a volume won't mount, a port is already bound, a build cache corruption. Stop the current task and surface the docker failure before continuing.

## How to stop

- State the condition in one sentence (which rule tripped, what the symptom is).
- Do **not** retry in a loop, switch tools silently, or pick a workaround that masks the underlying issue.
- Wait for Martin's explicit go-ahead before the next action. "Retry", "skip", "do it yourself", or "abort" — those are Martin's calls, not yours.

## What auto-fails review

- Silently writing implementation code because Codex was unavailable — the backend-dev / frontend-dev agents explicitly forbid this (see their own rules).
- Continuing past a docker failure by editing unrelated files "while we're here."
- Retrying a failing Codex call more than once without confirmation.
- Hiding a Codex quota warning inside a reasoning log instead of surfacing it at the moment it appeared.

## Out of scope for this rule

- Ordinary `dotnet build` / `dotnet test` failures — those are just bugs, fix them.
- EF migration failures on first run — diagnose the migration, don't escalate unless the failure points at docker / the compose postgres.
- `.claude/settings.local.json` harness drift — ignore per `git-workflow.md`.
