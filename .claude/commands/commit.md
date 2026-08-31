---
description: Draft a commit title and description from the current changes (does not commit)
---

You are drafting a commit message for the user to review and commit themselves. You must NOT run `git commit`, `git add`, `git stash`, `git checkout`, `git restore`, `git reset`, or any other state-changing git command. Only run read-only inspection commands.

Steps:

1. Run `git status`, `git diff` (unstaged changes), `git diff --staged` (staged changes), and `git log --oneline -10` (to match this repo's existing commit message style).
2. If there are no changes at all (nothing staged, nothing unstaged, nothing untracked), tell the user there's nothing to draft a message for and stop.
3. Analyze what changed and draft:
   - A concise, imperative **title** (~70 chars or less, matching the style of `git log`).
   - A short **body** (1-2 bullet points, each one line, ~15 words max) explaining *why* the change was made, not just what changed. Note this repo's existing commits have no body at all — if the change is simple enough that the title says it all, omit the body entirely rather than padding it out.
4. Present the drafted title and body clearly in your response, formatted so it's easy to copy (e.g. in a fenced code block).
5. Explicitly tell the user you have not staged or committed anything, and that they should review the diff and commit it themselves whenever ready.

Do not ask the user for permission to commit and do not offer to run the commit — this command's only job is producing the draft message.
