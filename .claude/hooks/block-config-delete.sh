#!/usr/bin/env bash
# PreToolUse/Bash hook. Denies any command that would mutate, delete, or
# move a .claude/ config file, hook script, rule, agent, or CLAUDE.md.
#
# Motivation: a prior incident where these files were deleted by the
# assistant and Martin was asked to reconfigure from scratch. Never again —
# if reconfig is needed, it must be an explicit Edit/Write by the assistant
# with a clear reason, not a Bash mutation that wipes or corrupts them.
#
# Protected paths: anything under `.claude/` (settings, hooks, rules, agents)
# plus `CLAUDE.md` at repo root.
#
# Blocked mutation verbs:
#   - Delete: rm, rmdir, unlink, trash, shred, git rm
#   - Move/rename: mv, git mv
#   - Copy-in (overwrite destination): cp, install
#   - Inline edit: sed -i, perl -pi, gsed -i
#   - Stream write: tee (default mode overwrites), dd with of=
#   - Output redirection: `> path` or `>> path` targeting a protected path
#
# Reads are NOT blocked — Claude Code itself needs to read .claude/settings.json,
# and CLAUDE.md is instructional context. `cat`, `ls`, `grep`, `git diff`, etc.
# against these paths are fine.

set -euo pipefail

cmd=$(jq -r '.tool_input.command // empty')

has_mutation_verb=0
if printf '%s' "$cmd" | grep -qE '(^|[[:space:]|&;`(])(rm|rmdir|unlink|trash|shred|mv|cp|install|tee|dd)([[:space:]]|$)'; then
  has_mutation_verb=1
fi
if printf '%s' "$cmd" | grep -qE 'git[[:space:]]+(rm|mv)([[:space:]]|$)'; then
  has_mutation_verb=1
fi
if printf '%s' "$cmd" | grep -qE '(^|[[:space:]|&;`(])(g?sed|perl)([[:space:]]|$).*(-i|-pi|-pie)'; then
  has_mutation_verb=1
fi

has_redirect_to_protected=0
if printf '%s' "$cmd" | grep -qE '>>?[[:space:]]*\.?/?(\.claude(/|$|[[:space:]])|CLAUDE\.md)'; then
  has_redirect_to_protected=1
fi

has_protected_path=0
if printf '%s' "$cmd" | grep -qE '\.claude(/|[[:space:]]|$)|(^|[[:space:]/])CLAUDE\.md'; then
  has_protected_path=1
fi

if { [ "$has_mutation_verb" -eq 1 ] && [ "$has_protected_path" -eq 1 ]; } \
   || [ "$has_redirect_to_protected" -eq 1 ]; then
  jq -n '{
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: "Blocked by .claude/hooks/block-config-delete.sh. The command would mutate, delete, move, copy-over, or redirect-into a .claude/ config path or CLAUDE.md. If the file genuinely needs to change, use the Edit/Write tool with a clear reason — never rm/mv/cp/tee/sed-i/perl-pi/redirect against these paths. Reads (cat/ls/grep/git-diff) are still allowed. If you believe this is a false positive, describe the intent to Martin and ask him to run the command himself."
    }
  }'
fi
