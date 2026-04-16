#!/usr/bin/env bash
# SessionEnd hook. Runs `dotnet build` at session close so a broken
# build surfaces before the session ends. Silent on success; emits a
# systemMessage with the build output on failure.

set -uo pipefail

if out=$(dotnet build --nologo -v q 2>&1); then
  exit 0
fi

printf '%s' "$out" | jq -Rs '{
  systemMessage: ("dotnet build FAILED at session end — fix before next session:\n\n" + .)
}'
