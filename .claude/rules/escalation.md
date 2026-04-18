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

## Orchestrator fallback — when dev agents can't reach Codex

When `backend-dev` or `frontend-dev` returns blocked (per their "When you're blocked" rule), the **main-thread orchestrator** (Claude) takes over. Two modes, in order of preference:

1. **Orchestrator drives Codex directly via the Bash companion.** `Skill(codex:rescue)` hangs indefinitely on its internal `task-resume-candidate --json` call — confirmed on issue #20 and codified in memory `feedback_codex_via_bash`. Use the companion script instead:

   ```bash
   COMPANION=$(ls -1t /Users/yklau/.claude/plugins/cache/openai-codex/codex/*/scripts/codex-companion.mjs | head -1)
   # Write brief to /tmp/codex-brief.md (use Write tool, not heredoc)
   RESPONSE=$(node "$COMPANION" task --background --write --fresh "$(cat /tmp/codex-brief.md)")
   JOB=$(echo "$RESPONSE" | grep -oE 'task-[a-z0-9-]+' | head -1)
   ```

   **Polling MUST run in the FOREGROUND of the main thread** (never `run_in_background: true`). A silent 60-second block feels like a freeze — Martin is watching. Emit a visible status line every 45s:

   ```bash
   while :; do
     node "$COMPANION" status "$JOB" --json > /tmp/codex-status.json 2>/dev/null
     STATUS=$(grep -oE '"status":[^,]+' /tmp/codex-status.json | head -1 | grep -oE '"[^"]+"$' | tr -d '"')
     PHASE=$(grep -oE '"phase":[^,]+' /tmp/codex-status.json | head -1 | grep -oE '"[^"]+"$' | tr -d '"')
     ELAPSED=$(grep -oE '"elapsed":[^,]+' /tmp/codex-status.json | head -1 | grep -oE '"[^"]+"$' | tr -d '"')
     echo "[$(date +%H:%M:%S)] status=$STATUS phase=$PHASE elapsed=$ELAPSED"
     [ "$STATUS" != "running" ] && break
     sleep 45
   done
   ```

   `grep` — not `jq` / `python3 -c json.load` — because the status response embeds raw newlines inside strings. Same 2-pass budget, same watchdog (10-min cap, 3-min phase-stall, terminal-status stop). Fetch the result with `python3 -c "import json; ..."` (result response is clean).

2. **Codex itself is unreachable** (watchdog trips, terminal error, quota exhausted per the ChatGPT-auth rule, or companion script missing). Orchestrator implements directly — **one layer at a time** (Domain → Application → Infrastructure → Api → Tests), spawning the `reviewer` agent between layers before continuing. No batching.

This is the **only** sanctioned exception to the "do not silently write code" rule below, and it only applies to the main-thread orchestrator, never to `backend-dev` / `frontend-dev`. Document in the reasoning log: which tier was used, why Codex wasn't reachable via the dev agent, reviewer verdict per layer if tier 2.

## What auto-fails review

- Silently writing implementation code because Codex was unavailable — the backend-dev / frontend-dev agents explicitly forbid this (see their own rules).
- Continuing past a docker failure by editing unrelated files "while we're here."
- Retrying a failing Codex call more than once without confirmation.
- Hiding a Codex quota warning inside a reasoning log instead of surfacing it at the moment it appeared.
- A **third Codex pass** on the same slice without Martin's go-ahead (see iteration-budget section).
- Hand-patching Codex's output after Pass 2 instead of stopping and reporting.
- Running Codex in **API-key mode** (`OPENAI_API_KEY` set, or `authMethod != "chatgpt"` in `/codex:setup`). ChatGPT-subscription auth only.
- Running the Codex polling loop with `run_in_background: true` on the main thread. It MUST be foreground with visible 45s status ticks so Martin sees the run progressing.

## Out of scope for this rule

- Ordinary `dotnet build` / `dotnet test` failures — those are just bugs, fix them.
- EF migration failures on first run — diagnose the migration, don't escalate unless the failure points at docker / the compose postgres.
- `.claude/settings.local.json` harness drift — ignore per `git-workflow.md`.
