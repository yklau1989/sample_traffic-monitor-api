#!/usr/bin/env bash
# PreToolUse/Bash hook. Denies commands that assign the OpenAI API-key
# env var or run `codex login` with an api-key value. See
# .claude/rules/escalation.md > "Codex auth mode".

set -euo pipefail

cmd=$(jq -r '.tool_input.command // empty')

if printf '%s' "$cmd" | grep -qE '(OPENAI_API_KEY=[^[:space:]]|codex[[:space:]]+login[[:space:]]+--api-key([[:space:]]+[^[:space:]]|=[^[:space:]]))'; then
  jq -n '{
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: "Blocked by .claude/rules/escalation.md (Codex auth mode). Codex must use ChatGPT-subscription auth only; assigning OPENAI_API_KEY or running codex login with an --api-key value would flip to per-token API billing."
    }
  }'
fi
