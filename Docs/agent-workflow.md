# Agent workflow: Linear issues + Langfuse usage

Applies to a repository where `Tools/Workflow/` is installed (into `.claude/` and `.github/` of the clone). Rules for the agent are in
`CLAUDE.md`; this page covers setup, access, verification and how to turn it off.

## What it does

| Moment | Who does it | What happens |
|---|---|---|
| Session starts | `SessionStart` hook | Reconnects to the active attempt (if any), resends queued usage. No status changes. |
| `/impl-start ABC-12` | `wf.sh start` | Issue → In Progress, issue branch, new attempt (issue id + attempt UUID), attempt opened in Langfuse. |
| Every reply / subagent / compaction | `Stop`, `SubagentStop`, `PreCompact` hooks | Usage of replies inside the attempt window copied to `.agent-workflow/usage/<attempt>.json`, keyed by message id. |
| End of each agent reply | `Stop` hook | If the issue branch has commits not on GitHub yet: push and create a **draft** PR when none is open. Status, attempt and Langfuse are not touched. Turn off with `"auto_draft_pr": false` in `Tools/Workflow/config.json`. |
| Code edit without an attempt | `PreToolUse` guard | Edits under `Assets/`, `Packages/`, `ProjectSettings/` and Unity MCP script edits are denied. |
| `git commit` | git `commit-msg`, `pre-commit` | Message must contain the active issue id; `.env` files, workflow state and key-like strings are blocked. |
| `/handoff` | agent self-check, then `wf.sh handoff` | Self-check by the agent (below) → checks → push → create/update PR (draft → ready for review) → In Review → usage to Langfuse → attempt closed. Stops at the first failed step. |
| `/impl-start ABC-12` after review | `wf.sh start` | Back to In Progress, new attempt linked to the same issue and PR. |
| PR merged on GitHub | `.github/workflows/linear-done-on-merge.yml` | Issue → Done, unless another PR for it is still open. Closing without merge does nothing. |

**Self-check before handoff** (`/handoff` step 3). In the project this workflow comes from, about 36% of the agent cost went to
review-fix issues opened after merge, so the agent checks its own work first:
- every acceptance criterion of the issue is met by the branch diff;
- `/code-review` on the diff, confirmed findings fixed (skipped for docs-only changes);
- C#: no compile errors, EditMode tests pass;
- UXML/USS: Game view screenshots of changed screens match the project's mockups;
- `Tools/Workflow`: the workflow tests pass;
- anything the project lists under "Self-check" in the "This project" section of `AGENTS.md`.

The results go to the PR body through `--check-result`. The script doesn't enforce the self-check; it is part of
the `/handoff` command text.

Statuses are resolved from the team's own workflow (team DEV: Todo, In Progress, In Review, Done) by the names in
`Tools/Workflow/config.json`; if a name is missing the script stops instead of guessing.

Other commands: `sh Tools/Workflow/wf.sh status | flush | abandon | collect`.

## What is sent to Langfuse

One trace per attempt (`langfuse.session.id` = issue id, so attempts group by issue): an `agent` span for the
attempt and one `generation` per model reply.

- Sent: issue id, attempt id, session id, branch, PR URL, model name, timestamps, token counts (input, output,
  cache read, cache creation), number of replies without usage data.
- Not sent: prompts, replies, code, file paths, tool names, tool inputs or outputs. The script only reads `id`,
  `model`, `timestamp` and `usage` from the transcript.
- Attempt span metadata (numbers only, measured from the transcripts and git):
  - `subagents`: replies, share of all replies and tokens of subagent replies;
  - `context`: average and maximum prompt size (input + cache read + cache creation) of main-agent replies;
  - `user_messages`: user turns in the attempt (tool results and injected skill text don't count), a rework signal;
  - `compactions`: context compactions (`compact_boundary` entries);
  - `diff`: files, insertions and deletions of the issue branch against the merge base with `origin/main`, measured at
    handoff or abandon.
  A value that could not be measured is `unavailable` or missing, never zero.
- Cost: not computed here. Langfuse fills it in only if its model price table has the model.
- Missing usage: a reply without usage data gets `usage_status: unavailable` and no usage numbers; a whole attempt
  without data is marked `unavailable`, not zero.
- Duplicates: the ledger is keyed by message id; span ids are derived from the attempt and message ids, and a
  payload leaves the outbox only after Langfuse accepted it.
- Offline: payloads wait in `.agent-workflow/outbox/` and go out with `flush`, `/handoff` or the next session start.

## Context size and cost

Each reply re-reads the whole context, so cost follows context size (rules for agents: `CLAUDE.md`, *Token budget*).

- The Stop hook prints a one-time note when the last main reply read more than `context_warning_tokens`
  (`Tools/Workflow/config.json`, default 150000, `0` turns it off). It repeats only after a compaction or in a new
  session.
- Baseline before any work (first reply of a session, 2026-11-02): about **52k tokens** in the terminal `claude` CLI
  and about **79.5k** in the Claude Desktop Code tab. The difference is the app's own instructions and its connectors
  (browsers, computer use, docs, visualizer), and it is paid again on every reply. Long implementation sessions are
  cheaper in the CLI. In the app, turn off connectors a Unity task doesn't need.
- The project MCP servers are `codebase-memory-mcp` (`.mcp.json`) and the user-level `UnityMCP` and `linear-server`.
  The workflow reaches Linear through `LINEAR_API_KEY`, so the Linear MCP is only needed to read issues in chat.
- `CLAUDE.md` and `AGENTS.md` add about 2.5k tokens to every session. `Docs/Code Rules/` (about 37k tokens in total)
  is read per task, only the files the task needs.

The official Langfuse Claude Code plugin was not used: it sends prompts, replies and tool inputs/outputs and has no
switch to turn content off (only truncation).

The older `Tools/Langfuse` per-phase report is independent of this workflow. Its PreCompact hook is no longer
installed and its generated files (`ledger.jsonl`, `otel/`, `usage_summary.json`) are not tracked in git; run
`usage_report.py` by hand if you need them.

## Code graph (codebase-memory)

Agents are pointed at the graph by the trigger table in `AGENTS.md`, the routing rule in `CLAUDE.md` and a soft
hint from the guard hook. The graph itself (`.codebase-memory/graph.db.zst`) is kept fresh without any manual step.

- **Nudge:** the `PreToolUse` guard hook adds a hint (`additionalContext`) when a Bash / PowerShell `grep` or `rg`
  over C# code (`*.cs`, `Assets/`) searches for a symbol-like pattern (PascalCase identifier, `Name(`). It shows
  once per session (marker in `.agent-workflow/nudges/`) and never denies: text search keeps working.
- **Index refresh:** the git `post-commit` hook and the `SessionStart` hook (only when `HEAD` differs from the last
  indexed commit, `.agent-workflow/index-head`) start `workflow.py reindex` detached. It runs
  `codebase-memory-mcp cli index_repository` (command, project name and timeout: `code_index` in
  `Tools/Workflow/config.json`) and never blocks the commit or the session; failures go to
  `.agent-workflow/hook.log`. Run it by hand with `sh Tools/Workflow/wf.sh reindex`.
  `CLAUDE_WORKFLOW_NO_INDEX=1` disables the background start (the tests set it, so a throwaway repo never
  overwrites the project's index).
- **Adoption report:** `sh Tools/Workflow/wf.sh adoption [--since YYYY-MM-DD] [--json]` reads the local session
  transcripts (`~/.claude/projects/<project>/`, subagents included) and prints, per session and in total: graph
  searches, text searches for symbol-like patterns (`grep` / `rg` / `find -name` in Bash, `Grep`, `Glob`), the graph
  share (target: above 50%), the fallback rate (a graph search followed within 5 tool calls by a text search or
  `Read` of the same identifier) and the subagent types launched. It runs locally only: nothing is sent to Langfuse
  and tool names stay on this machine.
- **Manual verification** of the refresh: commit anything, then `.agent-workflow/hook.log` shows `reindex ok at
  <sha>` within a minute or two and `index-head` holds the new `HEAD`.

## Install

1. **Python 3.8+** on PATH (`python`, `python3` or `py`) and **Git for Windows** (Claude Code runs hooks with its
   `sh`).
2. **Secrets**, never committed. Either set user environment variables (Windows: *Edit environment variables for your
   account*) or add them to a git-ignored `.env` file at the project root using `NAME=value` lines:
   - `LINEAR_API_KEY`: Linear → Settings → Security & access → Personal API keys.
   - `LANGFUSE_PUBLIC_KEY`, `LANGFUSE_SECRET_KEY`, `LANGFUSE_HOST`: Langfuse → Project settings → API keys.
   Restart Claude Desktop after changing environment variables.
3. **GitHub CLI**: install `gh` and run `gh auth login` once.
4. **Install** (once per clone, from Git Bash in the project root): `sh Tools/Workflow/wf.sh install`. It adds the
   workflow hooks to `.claude/settings.json` (existing hooks and settings are kept, re-running adds nothing twice),
   copies `/impl-start` and `/handoff` to `.claude/commands/`, copies the GitHub Action to
   `.github/workflows/linear-done-on-merge.yml` and installs the git `commit-msg`, `pre-commit` and `post-commit`
   hooks. Commit
   `.claude/` and `.github/` changes, then restart Claude Desktop so it loads the hooks. Sources are in
   `Tools/Workflow/templates/`.
5. **Merge → Done automation**: GitHub → repository → Settings → Secrets and variables → Actions → new secret
   `LINEAR_API_KEY`. The action works once the workflow file is on `main`. Optional variable for a different Done name: set `LINEAR_DONE_STATUS` in the workflow file.
   The secret is per repository: if the repository is recreated, renamed or transferred, add it again, otherwise
   every merged PR from the repository itself fails the `Linear - Done on merge` job with
   `LINEAR_API_KEY secret is not set; the issue was NOT moved to Done` (a red job, not a silent skip). Only PRs
   from forks, which never get secrets, are skipped. After adding the secret, re-run the failed jobs under
   Actions, or move the issues to Done by hand.
   If Linear's own GitHub integration is also enabled, both are fine together: the action skips issues that are
   already Done.
6. Optional, for batch-mode tests in `/handoff`: set `UNITY_EDITOR` to the Unity 6000.3.25f1 executable. While the
   project is open in the Editor the batch run is skipped; the agent runs the tests through Unity MCP and passes the
   result with `--check-result`.

## Verify

- Offline scenario tests (fake Linear, Langfuse and `gh`): `python Tools/Workflow/tests/test_scenarios.py` and
  `python Tools/Workflow/tests/test_done_on_merge.py`, plus `test_guard.py` (guard, code-graph nudge, index
  refresh) and `test_adoption.py`. They cover start, repeated start, restart with a new
  transcript, duplicate hooks, Langfuse down then flush, a failed PR step and re-run without a second PR, return from
  review, missing usage, guards and git hooks, automatic draft PRs after agent replies.
- Live: open the project in Claude Desktop; the first message shows `[workflow] No active implementation attempt`.
  Run `/impl-start <ISSUE>`, check the issue status and branch, run `sh Tools/Workflow/wf.sh status`.
- Langfuse: the trace `<ISSUE> implementation` appears with the session = issue id.

## Turn off

- For one shell or session: `CLAUDE_WORKFLOW_DISABLED=1` makes every hook a no-op.
- For good: `sh Tools/Workflow/wf.sh uninstall` removes the workflow hooks from `.claude/settings.json` (other
  hooks stay), the slash commands and the git hooks. Delete `.github/workflows/linear-done-on-merge.yml` to stop the
  merge automation. Local state is in `.agent-workflow/` and can be deleted.
