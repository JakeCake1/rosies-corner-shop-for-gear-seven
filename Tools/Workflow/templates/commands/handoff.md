---
description: Hand the active implementation attempt off to review (checks, PR, In Review, usage to Langfuse)
allowed-tools: Bash(sh Tools/Workflow/wf.sh:*)
---
Hand off the active implementation attempt.

1. Run `sh Tools/Workflow/wf.sh status` and confirm there is an active attempt and you are on its branch.
2. Make sure all work is committed in meaningful commits whose messages contain the issue id, and that no
   unrelated files or secrets are staged.
3. Run the checks you can: if the project is open in the Unity Editor, run the EditMode tests through Unity MCP
   (`run_tests`) and check the console for compile errors.
4. Run `sh Tools/Workflow/wf.sh handoff` and pass each check you ran yourself as
   `--check-result "<name>: <result>"`. The script pushes, creates or updates the PR (never a second one; the automatic draft PR is marked ready for review),
   moves the issue to In Review only after the PR step succeeded, sends usage to Langfuse and closes the attempt.
5. Report every step from the output. If a step failed, say exactly which one is unfinished; the data is kept,
   and running the command again continues from there (`flush` resends usage only).
