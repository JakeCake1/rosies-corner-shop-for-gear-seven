# CLAUDE.md

Project rules for agents are in `AGENTS.md`; they still apply in full:

@AGENTS.md

## Linear + Langfuse implementation workflow

Mandatory steps are enforced by `Tools/Workflow/workflow.py` through Claude Code hooks and git hooks
(`sh Tools/Workflow/wf.sh install` puts them in place). Setup, access, verification and how to turn it off: `Docs/agent-workflow.md`.

### General rules

- Every implementation attempt is tied to one Linear issue id. Don't start implementation without a defined issue;
  if it's unclear which issue the work belongs to, ask.
- Starting a session is not starting work on an issue. Finishing a reply or closing the app is not a handoff to
  review.
- Discussion, planning and research don't change Linear statuses, branches or attempts.

### Starting implementation: `/impl-start <ISSUE-ID>`

Runs `sh Tools/Workflow/wf.sh start <ISSUE-ID>`: fetches the issue, resolves the team's real In Progress / In
Review / Done statuses, moves the issue to In Progress, switches to the issue's own branch and opens a new
accounting attempt (issue id + unique attempt id). Then read the issue context and plan before editing.

### While working

- Commit in meaningful commits whose message contains the issue id (the commit-msg hook enforces it).
- Don't commit unrelated changes or secrets (the pre-commit hook blocks `.env` files and key-like strings).
- Model usage is collected automatically from the session transcripts (Stop, SubagentStop, PreCompact hooks) and
  deduplicated by message id. After an app restart the SessionStart hook reconnects to the active attempt.
- Missing usage data is reported as unavailable, never as zero.
- Update `Docs/` when the issue changes something the docs describe.
- After each agent reply the Stop hook pushes new commits of the issue branch and opens a **draft** PR if none is
  open (`auto_draft_pr` in `Tools/Workflow/config.json`). A draft PR is not a handoff: the status stays In Progress
  and the attempt stays open.

### Handoff to review: `/handoff`

Runs the available checks and reports them (Unity EditMode tests via Unity MCP when the Editor is open, passed as
`--check-result`), pushes, creates or updates the PR for the issue (never a second open PR for the same branch; a draft is marked ready
for review),
moves the issue to In Review only after the PR step succeeded, sends the attempt's usage to Langfuse and closes the
attempt. If a step fails, state is kept and the output says which step is unfinished; run `/handoff` again.

### After review

When implementation explicitly resumes, run `/impl-start <ISSUE-ID>` again: the issue goes back to In Progress and
a new attempt starts, linked to the same issue and PR.

### Merge

Never merge PRs automatically (the guard hook blocks `gh pr merge`). The GitHub Action
`.github/workflows/linear-done-on-merge.yml` moves the issue to Done after its PR is merged, only when no other
open PR for the issue remains.

### Langfuse and privacy

Only identifiers (issue, attempt, session, branch, PR), model names, timestamps and token counts are sent. No
prompts, replies, code or tool content. Cost is not computed locally. If Langfuse is unreachable, payloads stay in
`.agent-workflow/outbox/` and are resent by `wf.sh flush`, `/handoff` or the next session start.
