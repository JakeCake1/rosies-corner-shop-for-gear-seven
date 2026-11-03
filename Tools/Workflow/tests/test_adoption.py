#!/usr/bin/env python3
"""Checks adoption.py on a synthetic transcript: graph vs symbol text searches, fallback rate, subagent types."""
import json
import os
import sys
import tempfile

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import adoption as a  # noqa: E402


def call(name, **tin):
    return {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": name, "input": tin}]}}


calls = [
    call("mcp__codebase-memory-mcp__search_graph", name_pattern="PlayerService"),
    call("Grep", pattern="PlayerService"),                       # fallback on the same thing
    call("mcp__codebase-memory-mcp__trace_path", function_name="LevelModel"),
    call("Read", file_path="Assets/Game/Other/Unrelated.cs"),    # not a fallback
    call("Bash", command="grep -rn 'OtherThing(' Assets --include=*.cs"),
    call("Bash", command="grep -rn 'play_button' Assets --include=*.uss"),
    call("Agent", subagent_type="Explore", prompt="x"),
    call("Agent", prompt="x"),
    call("mcp__codebase-memory-mcp__index_repository", repo_path="."),  # not a search
]
folder = tempfile.mkdtemp()
with open(os.path.join(folder, "abc12345.jsonl"), "w", encoding="utf-8") as f:
    f.write("\n".join(json.dumps(c) for c in calls) + "\nnot json\n")
s = a.analyse(folder)["abc12345"]
assert (s["graph"], s["text_symbol"], s["text_other"], s["fallback"]) == (2, 2, 1, 1), s
assert s["agents"] == {"Explore": 1, "general-purpose": 1}, s
assert a.share(s) == 0.5
print("ADOPTION TESTS PASSED")
