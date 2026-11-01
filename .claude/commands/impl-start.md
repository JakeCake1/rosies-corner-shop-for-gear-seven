---
description: Start (or resume after review) implementation of a Linear issue
argument-hint: <ISSUE-ID>
allowed-tools: Bash(sh Tools/Workflow/wf.sh:*)
---
Start an implementation attempt for Linear issue `$ARGUMENTS`.

1. If `$ARGUMENTS` is empty, ask which Linear issue this work is for. Do not start without one.
2. Run `sh Tools/Workflow/wf.sh start $ARGUMENTS` from the project root. It fetches the issue, moves it to
   In Progress, switches to the issue branch and opens a new usage-accounting attempt.
3. If it fails, show the error and stop. Do not change the status or branch by hand.
4. Read the issue (Linear MCP: description, comments, attachments, parent issue) and the rule files from
   `AGENTS.md` that the task touches, then plan the change before editing code.
