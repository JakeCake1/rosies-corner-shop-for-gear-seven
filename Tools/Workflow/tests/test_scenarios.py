#!/usr/bin/env python3
"""End-to-end scenarios for workflow.py against a fake Linear, a fake Langfuse, a fake `gh` and a local bare origin.

Run: python3 Tools/Workflow/tests/test_scenarios.py   (no network, no credentials, nothing outside a temp folder)

Covers: start, usage collection with duplicates, restart (new session transcript), Langfuse down at handoff and
later flush, handoff re-run without a duplicate PR, return from review with a new attempt on the same PR, guard
and git hooks, missing usage reported as unavailable instead of zero.
"""
import http.server
import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading

WORKFLOW = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "workflow.py"))
STATES = [{"id": "s-todo", "name": "Todo", "type": "unstarted", "position": 1},
          {"id": "s-prog", "name": "In Progress", "type": "started", "position": 2},
          {"id": "s-rev", "name": "In Review", "type": "started", "position": 3},
          {"id": "s-done", "name": "Done", "type": "completed", "position": 4}]


class Fake(http.server.BaseHTTPRequestHandler):
    state = {"issue_state": "s-todo", "langfuse_down": False, "spans": {}, "posts": 0, "transitions": []}

    def log_message(self, *a):
        pass

    def reply(self, code, body):
        data = json.dumps(body).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        st = Fake.state
        if self.path == "/graphql":
            if "issueUpdate" in body["query"]:
                st["issue_state"] = body["variables"]["s"]
                st["transitions"].append(st["issue_state"])
                name = next(s["name"] for s in STATES if s["id"] == st["issue_state"])
                return self.reply(200, {"data": {"issueUpdate": {"success": True, "issue": {"state": {"name": name}}}}})
            cur = next(s for s in STATES if s["id"] == st["issue_state"])
            return self.reply(200, {"data": {"issue": {
                "id": "uuid-12", "identifier": "EVG-12", "title": "Rework SaveManager", "url": "https://linear/EVG-12",
                "branchName": "pirit699/evg-12-rework-savemanager", "state": cur,
                "team": {"id": "t", "key": "EVG", "name": "DEV", "states": {"nodes": STATES}}}}})
        if self.path == "/api/public/otel/v1/traces":
            if st["langfuse_down"]:
                return self.reply(503, {"message": "down"})
            st["posts"] += 1
            for rs in body["resourceSpans"]:
                for ss in rs["scopeSpans"]:
                    for sp in ss["spans"]:
                        attrs = {a["key"]: a["value"] for a in sp["attributes"]}
                        for k in attrs:
                            assert "input" not in k.split(".")[-1] and "output" not in k.split(".")[-1], k
                        st["spans"][(sp["traceId"], sp["spanId"])] = attrs  # keyed like Langfuse: same id = same span
            return self.reply(200, {})
        self.reply(404, {})


FAKE_GH = r'''#!/usr/bin/env python3
import json, os, sys
db = os.environ["FAKE_GH_DB"]
prs = json.load(open(db)) if os.path.exists(db) else []
a = sys.argv[1:]
def opt(name):
    return a[a.index(name) + 1] if name in a else None
if a[:2] == ["pr", "list"]:
    print(json.dumps([p for p in prs if p["head"] == opt("--head") and p["state"] == "open"]))
elif a[:2] == ["pr", "create"]:
    n = len(prs) + 1
    prs.append({"number": n, "url": f"https://github/pr/{n}", "title": opt("--title"), "head": opt("--head"),
                "state": "open", "body": opt("--body")})
    print(f"https://github/pr/{n}")
elif a[:2] == ["pr", "edit"]:
    next(p for p in prs if p["number"] == int(a[2]))["body"] = opt("--body")
else:
    sys.exit(f"fake gh: unsupported {a}")
json.dump(prs, open(db, "w"))
'''


def reply_line(mid, ts, out=10, usage=True):
    msg = {"id": mid, "model": "claude-opus-5-5", "content": [{"type": "text", "text": "SECRET PROMPT TEXT"}]}
    if usage:
        msg["usage"] = {"input_tokens": 3, "output_tokens": out, "cache_read_input_tokens": 100,
                        "cache_creation_input_tokens": 7}
    return json.dumps({"type": "assistant", "timestamp": ts, "sessionId": "sess", "message": msg}) + "\n"


def main():
    tmp = tempfile.mkdtemp(prefix="wf-test-")
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Fake)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    base = f"http://127.0.0.1:{server.server_port}"
    origin, repo, bindir = (os.path.join(tmp, d) for d in ("origin.git", "repo", "bin"))
    os.makedirs(bindir)
    with open(os.path.join(bindir, "gh"), "w") as f:
        f.write(FAKE_GH)
    os.chmod(os.path.join(bindir, "gh"), 0o755)
    env = dict(os.environ, PATH=bindir + os.pathsep + os.environ["PATH"], CLAUDE_WORKFLOW_ROOT=repo,
               FAKE_GH_DB=os.path.join(tmp, "prs.json"), LINEAR_API_KEY="lin_test", LINEAR_API_URL=base + "/graphql",
               LANGFUSE_PUBLIC_KEY="pk", LANGFUSE_SECRET_KEY="sk", LANGFUSE_HOST=base,
               GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t", GIT_COMMITTER_EMAIL="t@t")
    env.pop("UNITY_EDITOR", None)

    def sh(*cmd, ok=True, inp=None):
        p = subprocess.run(cmd, cwd=repo, env=env, capture_output=True, text=True, input=inp)
        if ok is not None and (p.returncode == 0) != ok:
            raise AssertionError(f"{cmd} -> {p.returncode}\n{p.stdout}\n{p.stderr}")
        return p.stdout + p.stderr

    def wf(*args, ok=True, inp=None):
        return sh(sys.executable, WORKFLOW, *args, ok=ok, inp=inp)

    subprocess.run(["git", "init", "--bare", "-b", "main", origin], check=True, capture_output=True)
    subprocess.run(["git", "clone", origin, repo], check=True, capture_output=True)
    shutil.copytree(os.path.dirname(WORKFLOW), os.path.join(repo, "Tools", "Workflow"),
                    ignore=shutil.ignore_patterns("__pycache__", "tests"))
    os.makedirs(os.path.join(repo, ".claude"))
    existing = {"hooks": {"PreCompact": [{"hooks": [{"type": "command", "command": "python3 old_snapshot.py"}]}]},
                "permissions": {"allow": ["Bash(ls:*)"]}}
    json.dump(existing, open(os.path.join(repo, ".claude", "settings.json"), "w"))
    with open(os.path.join(repo, ".gitignore"), "w") as f:
        f.write(".agent-workflow/\n.env\n")
    sh("git", "add", ".")
    sh("git", "commit", "-m", "init")
    sh("git", "push", "-u", "origin", "main")
    print(wf("install"))
    print(wf("install"))  # idempotent
    settings = json.load(open(os.path.join(repo, ".claude", "settings.json")))
    assert settings["permissions"] == existing["permissions"]
    pre = [h["command"] for g in settings["hooks"]["PreCompact"] for h in g["hooks"]]
    assert pre[0] == "python3 old_snapshot.py" and len(pre) == 2, pre
    assert all(len(settings["hooks"][e]) == 1 for e in ("SessionStart", "Stop", "SubagentStop", "PreToolUse"))
    assert os.path.exists(os.path.join(repo, ".claude", "commands", "impl-start.md"))
    assert os.path.exists(os.path.join(repo, ".github", "workflows", "linear-done-on-merge.yml"))
    sh("git", "add", ".")
    sh("git", "commit", "-m", "Install agent workflow")  # no attempt, no project code: allowed
    sh("git", "push")

    transcripts = os.path.join(tmp, "projects")
    os.makedirs(os.path.join(transcripts, "sess1", "subagents"))
    t1 = os.path.join(transcripts, "sess1.jsonl")
    open(t1, "w").write(reply_line("old", "2020-01-01T00:00:00.000Z"))  # before the attempt: must be ignored
    hook = lambda event, t, sid: wf("hook", event, inp=json.dumps({"transcript_path": t, "session_id": sid}))

    # --- guard: no attempt -> code edits denied, docs allowed
    out = wf("hook", "guard", inp=json.dumps({"tool_name": "Edit", "tool_input": {"file_path": os.path.join(repo, "Assets/A.cs")}}))
    assert '"deny"' in out, out
    out = wf("hook", "guard", inp=json.dumps({"tool_name": "Edit", "tool_input": {"file_path": os.path.join(repo, "Docs/x.md")}}))
    assert out.strip() == "", out
    out = wf("hook", "guard", inp=json.dumps({"tool_name": "Bash", "tool_input": {"command": "gh pr merge 3 --squash"}}))
    assert '"deny"' in out
    hook("session-start", t1, "sess1")  # session start alone changes nothing
    assert Fake.state["transitions"] == [] and not os.path.exists(os.path.join(repo, ".agent-workflow/active.json"))

    # --- start
    out = wf("start", "EVG-12")
    assert "In Progress" in out and Fake.state["issue_state"] == "s-prog", out
    assert sh("git", "rev-parse", "--abbrev-ref", "HEAD").strip() == "pirit699/evg-12-rework-savemanager"
    out = wf("start", "EVG-12")  # repeated call is a no-op
    assert "already active" in out
    assert Fake.state["transitions"] == ["s-prog"]
    wf("start", "EVG-99", ok=False)  # another issue while one is active
    hook("session-start", t1, "sess1")
    active = json.load(open(os.path.join(repo, ".agent-workflow/active.json")))
    attempt1 = active["attempt_id"]

    import datetime as dt
    now = lambda: dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    with open(t1, "a") as f:
        f.write(reply_line("m1", now(), out=5))
        f.write(reply_line("m1", now(), out=50))  # streaming duplicate, final usage wins
        f.write(reply_line("m2", now(), out=20))
    open(os.path.join(transcripts, "sess1", "subagents", "agent-a.jsonl"), "w").write(reply_line("sub1", now(), 4))
    hook("collect", t1, "sess1")
    hook("collect", t1, "sess1")  # re-sent hook must not double count

    # --- restart: new session, new transcript, usage keeps going into the same attempt
    t2 = os.path.join(transcripts, "sess2.jsonl")
    open(t2, "w").write(reply_line("m3", now(), out=30) + reply_line("m4", now(), usage=False))
    out = hook("session-start", t2, "sess2")
    assert attempt1[:8] in out and "EVG-12" in out, out
    hook("collect", t2, "sess2")
    st = json.loads(wf("collect").split("no active")[0])
    assert st["replies"] == 5 and st["replies_without_usage"] == 1, st
    assert st["tokens"]["output"] == 50 + 20 + 4 + 30, st
    assert st["usage_status"] == "partial"

    # --- git hooks: message without issue id, secret, then a good commit
    os.makedirs(os.path.join(repo, "Assets"), exist_ok=True)
    open(os.path.join(repo, "Assets", "Save.cs"), "w").write("class Save {}\n")
    sh("git", "add", ".")
    sh("git", "commit", "-m", "rework save manager", ok=False)
    open(os.path.join(repo, "Assets", "Key.cs"), "w").write('const string K = "sk-lf-1234567890abcdef";\n')
    sh("git", "add", ".")
    sh("git", "commit", "-m", "EVG-12: key", ok=False)
    sh("git", "rm", "--cached", "-q", "Assets/Key.cs")
    os.remove(os.path.join(repo, "Assets", "Key.cs"))
    sh("git", "commit", "-m", "EVG-12: split profile into chunks")

    # --- handoff with Langfuse down: PR + In Review done, usage kept locally
    Fake.state["langfuse_down"] = True
    out = wf("handoff", "--check-result", "EditMode tests (Unity MCP): 12 passed")
    assert "created https://github/pr/1" in out and Fake.state["issue_state"] == "s-rev", out
    assert "queued" in out, out
    assert os.path.exists(os.path.join(repo, ".agent-workflow/outbox", attempt1 + ".json"))
    assert not os.path.exists(os.path.join(repo, ".agent-workflow/active.json"))
    # usage after handoff does not belong to the closed attempt
    with open(t2, "a") as f:
        f.write(reply_line("late", now(), out=999))
    hook("collect", t2, "sess2")
    # Langfuse back: flush (and a repeated flush) sends once, ids stable
    Fake.state["langfuse_down"] = False
    print(wf("flush"))
    assert "nothing to send" in wf("flush")
    spans1 = {k: v for k, v in Fake.state["spans"].items()}
    gens = [v for v in spans1.values() if v["langfuse.observation.type"]["stringValue"] == "generation"]
    assert len(gens) == 5, len(gens)
    assert not any("999" in json.dumps(v) for v in gens)
    assert sum(1 for v in gens if "langfuse.observation.usage_details" not in v) == 1  # unavailable, not zero
    assert "SECRET PROMPT TEXT" not in json.dumps(list(spans1.values()))

    # --- return from review: new attempt, same issue and PR, status back to In Progress
    out = wf("start", "EVG-12")
    assert Fake.state["issue_state"] == "s-prog" and "continues PR https://github/pr/1" in out, out
    attempt2 = json.load(open(os.path.join(repo, ".agent-workflow/active.json")))["attempt_id"]
    assert attempt2 != attempt1
    hook("session-start", t2, "sess2")
    with open(t2, "a") as f:
        f.write(reply_line("m5", now(), out=8))
    open(os.path.join(repo, "Assets", "Save.cs"), "a").write("// review fix\n")
    sh("git", "commit", "-am", "EVG-12: address review")

    # --- handoff where the PR step fails, then a re-run: no duplicate PR
    real_db = env["FAKE_GH_DB"]
    env["FAKE_GH_DB"] = "/nonexistent/dir/prs.json"
    out = wf("handoff", ok=False)
    assert Fake.state["issue_state"] == "s-prog", "status must not change when the PR step fails"
    env["FAKE_GH_DB"] = real_db
    out = wf("handoff")
    assert "updated https://github/pr/1" in out, out
    prs = json.load(open(real_db))
    assert len(prs) == 1 and attempt1[:8] in prs[0]["body"] and attempt2[:8] in prs[0]["body"]
    assert Fake.state["issue_state"] == "s-rev"
    traces = {k[0] for k in Fake.state["spans"]}
    assert len(traces) == 2, traces  # one trace per attempt
    sessions = {v["langfuse.session.id"]["stringValue"] for v in Fake.state["spans"].values()}
    assert sessions == {"EVG-12"}

    # --- attempt with no usage at all is reported as unavailable
    wf("start", "EVG-12")
    out = wf("abandon")
    last = [v for v in Fake.state["spans"].values() if v.get("langfuse.observation.metadata.outcome", {}).get("stringValue") == "abandoned"]
    assert last and last[0]["langfuse.trace.metadata.usage_status"]["stringValue"] == "unavailable"
    assert "langfuse.observation.metadata.tokens" not in last[0]

    # --- disabled workflow: hooks are no-ops
    print(wf("uninstall"))
    settings = json.load(open(os.path.join(repo, ".claude", "settings.json")))
    assert settings["hooks"] == existing["hooks"] and settings["permissions"] == existing["permissions"], settings
    assert not os.path.exists(os.path.join(repo, ".git", "hooks", "commit-msg"))
    env["CLAUDE_WORKFLOW_DISABLED"] = "1"
    out = wf("hook", "guard", inp=json.dumps({"tool_name": "Edit", "tool_input": {"file_path": os.path.join(repo, "Assets/A.cs")}}))
    assert out.strip() == ""
    server.shutdown()
    shutil.rmtree(tmp, ignore_errors=True)
    print("ALL SCENARIOS PASSED")


if __name__ == "__main__":
    main()
