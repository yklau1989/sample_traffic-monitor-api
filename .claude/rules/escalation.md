# Escalation rules

When to **stop and ask Martin** before taking the next action. These rules exist because every one of the listed conditions has cost us time or a revert in the past — the cheap thing is to pause for 30 seconds and confirm.

## Stop and ask — no exceptions

1. **Codex usage is close to its limit.** If `/codex:setup`, `/codex:status`, or any Codex companion output mentions remaining quota, rate limiting, or a "close to limit" warning, stop. Do not start a new `/codex:rescue`, `/codex:review`, or `/codex:adversarial-review` run until Martin confirms. **If quota is fully exhausted: we wait.** Do not switch Codex to API-key mode to keep going (see "Codex auth mode" below).
2. **Codex plugin / CLI is unreachable.** If `/codex:setup` reports `ready: false`, `auth.loggedIn: false`, or a `codex-companion.mjs` call errors out with auth / socket / transport failures, stop. Do not fall back to writing the implementation yourself.
3. **Any Docker, Docker Compose, Dockerfile, or CI problem — regardless of what task you're on.** Examples: `docker compose up` fails, a container won't become healthy, a volume won't mount, a port is already bound, a build cache corruption, a Dockerfile layer fails to build, a GitHub Actions workflow fails. Stop the current task and surface the failure before continuing — do not "work around it" by editing unrelated files. This applies whether you're on an infra task or any other task: infra problems always escalate first, even when infra itself is the target.

## Codex auth mode — ChatGPT subscription only

Martin pays for Codex through the **$20 ChatGPT subscription**. Do not run Codex against the paid **OpenAI API** (per-token billing) under any circumstance.

- `/codex:setup --json` must report `"authMethod": "chatgpt"`. If it ever reports `"apikey"` or anything else, stop — do not issue `/codex:rescue`, `/codex:review`, or `/codex:adversarial-review` until Martin confirms.
- Never set `OPENAI_API_KEY` in the environment, in `.env`, in `docker-compose.yml`, or inside any Codex brief or script. That env var silently flips Codex into API mode.
- Never suggest `codex login --api-key ...`, `codex login --provider openai`, or any equivalent that swaps to API-key auth.
- If ChatGPT-plan quota runs out: **we wait** for it to reset. That is the policy — do not offer a "just for this one run" API-mode workaround. Surface the exhaustion to Martin and pause.

## Codex iteration budget — max 2 passes per feature slice

For any single issue / feature slice handed to Codex, the budget is **two Codex runs, total**:

1. **Pass 1** — the initial `/codex:rescue` run. Review the output.
   - If it's roughly **80–90% good** (builds, tests pass, Key Design Rules honoured, minor polish aside), **accept it**. Don't keep polishing for its own sake. Ship it, reviewer catches the rest.
   - If it clearly misses the acceptance criteria, has a design-rule violation, or doesn't build, continue to Pass 2.
2. **Pass 2** — exactly one follow-up `/codex:rescue --resume` with a *specific* list of what to fix. Review the output.
   - If the fix lands, accept and ship.
   - If it still falls short — **stop**. Do not start Pass 3. Surface the situation to Martin with: what was wrong, what you asked Codex to fix, what Codex produced on the second pass, and what's still missing.

**You never write the fix yourself**, regardless of how small it looks after Pass 2. The two-pass rule exists because trying to squeeze a third pass out of Codex has historically burned quota faster than accepting the 80% result and moving on. Orchestrator work (build / test / migrate / commit) still happens after every pass — that doesn't count against the budget.

Log every pass in the reasoning log's *Status / Next* section with the Codex job ID so Martin can run `/codex:result <id>` and see what each pass produced.

## How to stop

- State the condition in one sentence (which rule tripped, what the symptom is).
- Do **not** retry in a loop, switch tools silently, or pick a workaround that masks the underlying issue.
- Wait for Martin's explicit go-ahead before the next action. "Retry", "skip", "do it yourself", or "abort" — those are Martin's calls, not yours.

## What auto-fails review

- Silently writing implementation code because Codex was unavailable — the backend-dev / frontend-dev agents explicitly forbid this (see their own rules).
- Continuing past a docker failure by editing unrelated files "while we're here."
- Retrying a failing Codex call more than once without confirmation.
- Hiding a Codex quota warning inside a reasoning log instead of surfacing it at the moment it appeared.
- A **third Codex pass** on the same slice without Martin's go-ahead (see iteration-budget section).
- Hand-patching Codex's output after Pass 2 instead of stopping and reporting.
- Running Codex in **API-key mode** (`OPENAI_API_KEY` set, or `authMethod != "chatgpt"` in `/codex:setup`). ChatGPT-subscription auth only.

## Out of scope for this rule

- Ordinary `dotnet build` / `dotnet test` failures — those are just bugs, fix them.
- EF migration failures on first run — diagnose the migration, don't escalate unless the failure points at docker / the compose postgres.
- `.claude/settings.local.json` harness drift — ignore per `git-workflow.md`.
