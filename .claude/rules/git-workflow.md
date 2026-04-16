# Git workflow rules

Solo repo, but the evaluator will read the history. Keep it clean.

## Branches

- **Never commit directly to `main` or `master`.** Always branch first.
- Branch name: `feature/{issue-number}-short-description` (kebab-case). E.g. `feature/3-rules-files`.
- Branches are *optional* per CLAUDE.md for tiny fixes, but default to creating one — a PR trail is cheap and evaluator-friendly.
- One branch per issue. Do not bundle unrelated work.

## Commits

- Commit message format: `<type>(#<issue>): <subject>` — lowercase subject, imperative mood, no trailing period.
  - Types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `ci`.
  - Example: `feat(#3): add .claude/rules baseline`
- Feature-branch commits are pre-authorized — do not ask each time.
- Small, self-contained commits. If a commit mixes unrelated changes, split it.
- **Never `--amend` a pushed commit.** Create a new one.
- **Never `--no-verify`** unless explicitly asked.

## Harness-drift files

- `.claude/settings.local.json` is auto-updated by the Claude Code harness. It frequently shows as modified. **Do not stage or commit it** unless the user explicitly stages it themselves.
- When checkout is blocked by harness drift, `git stash push .claude/settings.local.json`, switch, then `git stash pop`.

## Milestone / issue closing workflow

When an issue is complete:

1. **Comment on the GitHub issue first** — short summary of what landed, link to the key commits.
2. **Then open the PR** that closes it. PR body must include `Closes #<issue>`.
3. Do not skip the issue comment — it is the human-readable trail.

## Pull requests

- Title short, under ~70 chars. Detail goes in the body.
- Body: **Summary**, **Changes**, **Test plan** (bulleted checklist).
- Include `Closes #<issue>` so merge auto-closes the issue.
- Merge style: **merge commit** by default (preserves branch history for the evaluator). Rebase is not used on this repo. Squash only if explicitly requested.
- Do **not** delete the branch automatically after merge — the user decides case by case.

## Destructive operations

Never, without explicit approval in the same message:

- `git push --force` / `--force-with-lease`
- `git reset --hard`
- `git branch -D`
- `git clean -fd`
- Force-push to `main` — refuse even if asked; warn first.

## Review gate

- Run the `reviewer` agent on the diff before merging any PR.
- Reviewer writes a reasoning log at `docs/logs/{range}/{issue}-reasoning.md` — see `documentation.md`.
- Merge only after reviewer signs off **and** Martin approves.
