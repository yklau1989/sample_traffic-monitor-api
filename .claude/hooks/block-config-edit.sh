#!/usr/bin/env bash
# PreToolUse/Edit|Write hook. Denies any Edit or Write targeting the
# hook scripts under .claude/hooks/ or .claude/settings.json — the files
# that enforce the secret-leak and config-delete rules.
#
# Motivation: without this, a future Claude session (or a compromised
# skill / CLAUDE.md / agent definition) could Edit those files to
# neuter the enforcement. The Bash-mutation lock in
# block-config-delete.sh already gates rm/mv/cp/sed-i/tee/dd/redirect
# against these paths; this hook closes the Edit/Write equivalent.
#
# NOT locked:
#   - .claude/settings.local.json — the harness writes to it continuously.
#   - .claude/rules/*, .claude/agents/*, .claude/skills/* — instructions,
#     editing them cannot disable the enforcement layer.
#   - CLAUDE.md — policy/instructions, same reasoning.
#
# Legitimate changes to the locked files go through the user's own
# editor: the assistant drafts the diff, the user applies it.

set -euo pipefail

path=$(jq -r '.tool_input.file_path // empty')

if printf '%s' "$path" | grep -qE '(^|/)\.claude/hooks/' \
   || printf '%s' "$path" | grep -qE '(^|/)\.claude/settings\.json$'; then
  jq -n '{
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: "Blocked by .claude/hooks/block-config-edit.sh. Edit/Write is locked on .claude/hooks/* and .claude/settings.json — those files enforce the secret-leak and config-delete rules, so they cannot be modified through the agent. If a change is genuinely needed, describe the diff to the user and let them apply it in their own editor."
    }
  }'
fi
