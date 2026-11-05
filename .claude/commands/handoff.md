---
description: Hand the active implementation attempt off to review (self-check, checks, PR, In Review, usage to Langfuse)
allowed-tools: Bash(sh Tools/Workflow/wf.sh:*)
---
Hand off the active implementation attempt.

1. Run `sh Tools/Workflow/wf.sh status` and confirm there is an active attempt and you are on its branch.
2. Make sure all work is committed in meaningful commits whose messages contain the issue id, and that no
   unrelated files or secrets are staged.
3. Self-check before review. Fixing a finding here is far cheaper than a follow-up review-fix issue later.
   Commit the fixes before step 4.
   - **Acceptance criteria:** re-read the issue (description and comments) and confirm each criterion against
     `git diff origin/main...HEAD`. A criterion you did not meet is either done now or named in the report.
   - **Review:** run `/code-review` on the branch diff and fix the confirmed findings. Skip it only for changes to
     docs alone.
   - **C# changes:** no compile errors in the Unity console; EditMode tests through Unity MCP (`run_tests`); when
     `Features/Gameplay/Model` changed, `PrototypeParityTests` must pass.
   - **UXML/USS changes:** take a Game view screenshot of each changed screen (panel 390×844) and compare it with
     the matching mockup from `screens.rar`; fix visible differences.
   - **Workflow changes** (`Tools/Workflow`): `python Tools/Workflow/tests/test_scenarios.py` and
     `python Tools/Workflow/tests/test_done_on_merge.py`.
   - **Project template changes** (`Tools/ProjectTemplate`, the baseline layout in the structure rules):
     `python Tools/ProjectTemplate/tests/test_scaffold.py`.
4. Run `sh Tools/Workflow/wf.sh handoff` and pass each check you ran yourself as
   `--check-result "<name>: <result>"` (the review and the mockup comparison count as checks). The script pushes,
   creates or updates the PR (never a second one; the automatic draft PR is marked ready for review),
   moves the issue to In Review only after the PR step succeeded, sends usage to Langfuse and closes the attempt.
5. Report every step from the output and any acceptance criterion left open. If a step failed, say exactly which
   one is unfinished; the data is kept, and running the command again continues from there (`flush` resends usage
   only).
