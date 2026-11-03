#!/usr/bin/env python3
"""Checks linear_done_on_merge.py: merged vs closed, several PRs per issue, already done. No network."""
import json
import os
import sys
import tempfile

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import linear_done_on_merge as m  # noqa: E402


def run_case(merged, open_prs, state_type="started", key="k", head_repo="o/r"):
    calls = []

    def fake_post(url, payload, headers):
        calls.append(payload["query"].split("(")[0].strip())
        if "issueUpdate" in payload["query"]:
            return {"data": {"issueUpdate": {"success": True}}}
        return {"data": {"issue": {"id": "u", "state": {"name": "In Review", "type": state_type},
                                   "team": {"states": {"nodes": [
                                       {"id": "r", "name": "In Review", "type": "started", "position": 1},
                                       {"id": "d", "name": "Done", "type": "completed", "position": 2}]}}}}}

    m.post, m.get = fake_post, lambda url, token: open_prs
    event = {"pull_request": {"number": 5, "merged": merged, "title": "EVG-12: Rework SaveManager",
                              "head": {"ref": "pirit699/evg-12-rework-savemanager",
                                       "repo": {"full_name": head_repo} if head_repo else None}}}
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False) as f:
        json.dump(event, f)
    os.environ.update(GITHUB_EVENT_PATH=f.name, GITHUB_TOKEN="t", GITHUB_REPOSITORY="o/r")
    os.environ.pop("LINEAR_API_KEY", None)
    if key:
        os.environ["LINEAR_API_KEY"] = key
    m.main()
    return [c for c in calls if c == "mutation"]


assert run_case(False, []) == []                                   # closed without merge
assert run_case(True, []) == ["mutation"]                          # merged, only PR -> Done
other = [{"number": 6, "title": "EVG-12: follow-up", "head": {"ref": "x"}}]
assert run_case(True, other) == []                                 # another open PR for the issue
assert run_case(True, [{"number": 7, "title": "EVG-13", "head": {"ref": "y"}}]) == ["mutation"]
assert run_case(True, [], state_type="completed") == []            # already done
assert run_case(True, [], key=None, head_repo="fork/r") == []      # fork PR without the secret: skipped
assert run_case(True, [], key=None, head_repo=None) == []          # deleted fork: skipped, no crash
try:
    run_case(True, [], key=None)                                   # same-repo PR without the secret: loud failure
    raise AssertionError("a missing secret on a same-repo PR must fail")
except SystemExit as e:
    assert "LINEAR_API_KEY" in str(e)
print("DONE-ON-MERGE TESTS PASSED")
