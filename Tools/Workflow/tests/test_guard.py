#!/usr/bin/env python3
"""Checks pure helpers of workflow.py: handoff result parsing, Unity MCP read-only guard, fallback issue regex."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import workflow as w  # noqa: E402

for text in ("Passed 120, Failed 0", "0 tests failed", "Failures: 0", "no failures", "0 errors", "12 passed"):
    assert not w.reported_failed(text), text
for text in ("3 errors", "2 failed", "Passed 10, Failed 12", "build failed"):
    assert w.reported_failed(text), text

ro = lambda tool, action: w.is_unity_mcp_readonly("mcp__UnityMCP__" + tool, {"action": action} if action else {})
assert ro("manage_scene", "get_hierarchy") and ro("manage_asset", "find") and ro("read_console", None)
assert not ro("manage_scene", "get_or_create") and not ro("manage_scene", "screenshot")
assert not ro("manage_gameobject", "create") and not ro("batch_execute", None) and not ro("execute_code", None)

assert [m for m in w.ISSUE_RE.findall("UTF-8 and EVG-12")] == ["EVG-12"]
import contextlib  # noqa: E402
import io  # noqa: E402


def denied(command):
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        w.hook_guard({"tool_name": "Bash", "tool_input": {"command": command}})
    return bool(out.getvalue())


for command in ("gh -R o/r pr merge 5", "gh.exe pr merge 5", "gh pr merge 5 --squash", "git push origin +main",
                "git push --force origin main"):
    assert denied(command), command
for command in ("gh pr view 5", "git push origin feature", "gh pr list --state open"):
    assert not denied(command), command
print("GUARD TESTS PASSED")
