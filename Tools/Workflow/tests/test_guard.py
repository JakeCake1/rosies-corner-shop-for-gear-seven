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

# ---------------------------------------------------------------- code graph nudge
import subprocess  # noqa: E402
import tempfile  # noqa: E402

w.NUDGE_DIR = os.path.join(tempfile.mkdtemp(), "nudges")


def guard_output(command, session="s1"):
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        w.hook_guard({"tool_name": "Bash", "session_id": session, "tool_input": {"command": command}})
    return json.loads(out.getvalue()) if out.getvalue() else None


for command in ("grep -rn 'PlayerService' Assets --include=*.cs", "rg -n 'IFooService' -t cs Assets/",
                r"cd x && grep -rn 'LevelModel\|LevelState' Assets/ | head"):
    found = list(w.parse_text_search(command))
    assert found and found[0][2] and w.looks_like_symbol(found[0][1]), (command, found)

import json  # noqa: E402

hint = guard_output("grep -rn 'PlayerService' Assets --include=*.cs", "a")
assert hint and "search_graph" in hint["hookSpecificOutput"]["additionalContext"]
assert "permissionDecision" not in hint["hookSpecificOutput"]  # never a deny
assert guard_output("grep -rn 'LevelModel' Assets/", "a") is None  # once per session
assert guard_output("grep -rn 'LevelModel' Assets/", "b") is not None  # a new session gets its own hint
for command in ("grep -rn 'play_button' Assets/Game/Content --include=*.uss", "grep -rn 'todo' Assets/ --include=*.cs",
                "grep -rn 'EVG-12' Docs/", "grep -rn 'PlayerService' Docs/", "ls Assets", "git log --grep=Foo"):
    assert guard_output(command, "c") is None, command
print("NUDGE TESTS PASSED")

# ---------------------------------------------------------------- code index refresh
state = tempfile.mkdtemp()
repo = tempfile.mkdtemp()  # a throwaway git repo: the test does not depend on where the kit is checked out
for args in (["init", "-q"], ["-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "--allow-empty", "-m", "x"]):
    subprocess.run(["git", *args], cwd=repo, check=True)
w.ROOT = repo
w.STATE, w.LOG_FILE = state, os.path.join(state, "hook.log")
w.INDEX_HEAD_FILE, w.INDEX_LOCK_FILE = os.path.join(state, "index-head"), os.path.join(state, "reindex.lock")
assert w.index_is_stale()  # never indexed
w.index_settings = lambda: {"command": sys.executable, "name": "x", "timeout": 60}
w.run_orig = w.run
w.run = lambda cmd, **kw: w.run_orig([cmd[0], "-c", "pass"], **kw)  # a succeeding fake `codebase-memory-mcp cli ...`
w.cmd_reindex(None)
assert not w.index_is_stale() and not os.path.exists(w.INDEX_LOCK_FILE)
w.run = lambda cmd, **kw: w.run_orig([cmd[0], "-c", "import sys; sys.exit(3)"], **kw)
with open(w.INDEX_HEAD_FILE, "w") as f:
    f.write("old")
w.cmd_reindex(None)  # failure: logged, index head kept, nothing raised
def logged():
    with open(w.LOG_FILE, encoding="utf-8") as f:
        return f.read()


assert w.index_is_stale() and "reindex failed" in logged()
def missing_binary(cmd, **kw):
    if cmd[0] == "git":
        return w.run_orig(cmd, **kw)
    raise FileNotFoundError("no server")


w.run = missing_binary
w.cmd_reindex(None)
assert "reindex failed: FileNotFoundError" in logged()
assert not os.path.exists(w.INDEX_LOCK_FILE)
print("REINDEX TESTS PASSED")
