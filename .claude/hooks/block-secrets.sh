#!/usr/bin/env bash
# PreToolUse/Bash hook. Denies any command whose string references a
# secret-bearing file path. Pattern list drawn from: Claude Code docs
# (code.claude.com/docs/en/settings#prevent-claude-code-from-accessing-files),
# gitleaks default ruleset, and common secret-store locations.
#
# Categories covered:
#   - Env files (.env / .env.* except templates)
#   - SSH / GPG (~/.ssh, ~/.gnupg)
#   - Cloud creds (~/.aws, ~/.azure, ~/.config/gcloud, ~/.oci)
#   - Orchestration (~/.kube, ~/.docker, ~/.cloudflared)
#   - Vault / password stores (~/.vault-token, ~/.config/op, ~/.password-store)
#   - Git creds (~/.git-credentials, ~/.netrc)
#   - Package-manager creds (~/.npmrc, ~/.pypirc, ~/.gem, ~/.cargo, ~/.m2)
#   - DB creds & history (~/.pgpass, ~/.my.cnf, *_history files)
#   - Cert/key material (*.pem, *.key, *.pfx, *.p12)
#   - Terraform state/vars (*.tfvars, *.tfstate)
#   - Generic secrets (credentials.*, secrets/, secret_*)
#
# Template files (.env.example, .env.sample, .env.template) are stripped
# before the danger check — they're safe to read.
#
# Motivation: a leaked password via `grep ... .env` (2026-04) showed that
# allow-lists like `Bash(cat *)` / `Bash(grep *)` are too broad — deny rules
# win, so this hook is the authoritative gate for Bash access to secrets.

set -euo pipefail

cmd=$(jq -r '.tool_input.command // empty')

# Strip template-file references first — they're safe to read.
cleaned=$(printf '%s' "$cmd" | sed -E \
  -e 's/\.env\.(example|sample|template|dist)//g' \
  -e 's/\.envrc\.example//g')

if printf '%s' "$cleaned" | grep -qE \
  -e '\.env([^A-Za-z0-9]|$)' \
  -e '\.env\.[A-Za-z0-9]' \
  -e '\.envrc([^A-Za-z0-9]|$)' \
  -e '(^|[/~])\.ssh(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.gnupg(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.aws(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.azure(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.oci(/|$|[^A-Za-z])' \
  -e '/\.config/gcloud' \
  -e '(^|[/~])\.kube(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.docker(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.cloudflared(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.heroku(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.vercel(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.netlify(/|$|[^A-Za-z])' \
  -e '(^|[/~])\.firebase(/|$|[^A-Za-z])' \
  -e '\.vault-token' \
  -e '/\.config/op(/|$)' \
  -e '(^|[/~])\.password-store(/|$|[^A-Za-z])' \
  -e '\.git-credentials' \
  -e '/\.config/git/credentials' \
  -e '(^|[/~])\.netrc([^A-Za-z0-9]|$)' \
  -e '(^|[/~])\.npmrc([^A-Za-z0-9]|$)' \
  -e '(^|[/~])\.pypirc([^A-Za-z0-9]|$)' \
  -e '(^|[/~])\.gem/credentials' \
  -e '(^|[/~])\.cargo/credentials(\.toml)?' \
  -e '(^|[/~])\.m2/settings\.xml' \
  -e '(^|[/~])\.bundle/config' \
  -e '(^|[/~])\.pgpass' \
  -e '(^|[/~])\.my\.cnf' \
  -e '\.(zsh|bash|sh|fish|python|node_repl|irb|psql|mysql|sqlite|mongo|redis)_history' \
  -e '\.(pem|pfx|p12)([^A-Za-z0-9]|$)' \
  -e '[A-Za-z0-9_.-]+\.key([^A-Za-z0-9]|$)' \
  -e '\.(tfvars|tfstate|tfstate\.backup)([^A-Za-z0-9]|$)' \
  -e '(^|[/[:space:]])credentials(\.(json|yaml|yml|toml|env|txt))?([^A-Za-z0-9]|$)' \
  -e '/secrets?/' \
  -e '(^|[/[:space:]])secrets?\.(json|yaml|yml|toml|env|txt)' \
  -e 'service[_-]?account.*\.json' \
  -e 'application_default_credentials'; then
  jq -n '{
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: "Blocked by .claude/hooks/block-secrets.sh. The command references a secret-bearing path (env file, SSH/GPG, cloud creds, kubeconfig, vault token, password store, git/package-manager creds, DB creds or *_history, *.pem/*.key, terraform state, service-account JSON, or a /secrets/ path). Reading these into the conversation leaks values to the transcript and telemetry. Edit the file directly in your editor; do not pipe it through Bash."
    }
  }'
fi
