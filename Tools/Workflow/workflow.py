#!/usr/bin/env python3
"""Linear + Langfuse implementation workflow for Claude Code (Python 3.8+, standard library only).

An *attempt* is one stretch of implementation work on one Linear issue. It starts with `start` and ends with a
successful `handoff` (or `abandon`). Usage of the model is accounted per attempt and sent to Langfuse as one trace.

Commands
  start ISSUE         Fetch the issue, move it to In Progress, switch to its branch, open a new attempt.
  handoff             Run checks, push, create or update the PR (a draft becomes ready for review), move the issue
                      to In Review, send usage, close.
  status              Show the active attempt, handoff steps, collected usage and unsent Langfuse payloads.
  flush               Resend Langfuse payloads that could not be sent earlier.
  abandon             Close the active attempt without handoff (usage is still sent; Linear is not touched).
  collect             Copy usage from the attempt's transcripts into its ledger (hooks do this automatically).
  install / uninstall Add or remove the Claude Code hooks and slash commands, the git hooks (commit-msg,
                      pre-commit) and the merge -> Done GitHub Action (install only; existing hooks are kept).
  (Stop hook)         After each agent reply, new commits on the issue branch are pushed and a draft PR is created
                      if none is open (config "auto_draft_pr"). Not a handoff: status and attempt stay as they are.
  hook EVENT          Entry point for Claude Code and git hooks (session-start, collect, guard, commit-msg,
                      pre-commit).

State lives in .agent-workflow/ (git-ignored; outside .claude/ so remote tools can read it). Secrets come from the environment or the git-ignored .env file:
LINEAR_API_KEY, LANGFUSE_PUBLIC_KEY, LANGFUSE_SECRET_KEY, LANGFUSE_HOST. Set CLAUDE_WORKFLOW_DISABLED=1 to turn
all hooks into no-ops.

Privacy: only identifiers (issue, attempt, session, branch, PR), model names, timestamps and token counts are sent
to Langfuse. Prompts, replies, code and tool inputs/outputs are never read into the payload. No cost is computed
here; Langfuse derives cost from its own model price table if it has one for the model.
"""
import argparse
import base64
import datetime as dt
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.environ.get("CLAUDE_WORKFLOW_ROOT") or os.path.join(HERE, "..", ".."))
STATE = os.path.join(ROOT, ".agent-workflow")
ACTIVE_FILE = os.path.join(STATE, "active.json")
ATTEMPTS_DIR = os.path.join(STATE, "attempts")
USAGE_DIR = os.path.join(STATE, "usage")
OUTBOX_DIR = os.path.join(STATE, "outbox")
ISSUES_DIR = os.path.join(STATE, "issues")
LOG_FILE = os.path.join(STATE, "hook.log")
CONFIG_FILE = os.path.join(HERE, "config.json")
TEMPLATES = os.path.join(HERE, "templates")
HOOK_MARK = "Tools/Workflow/wf.sh"
NAMESPACE = uuid.UUID("3b1f4f5e-8c3e-4d1e-9a0e-5f2b7c9d1e60")
ISSUE_RE = re.compile(r"(?<![A-Za-z0-9])([A-Za-z][A-Za-z0-9]{0,9}-\d+)(?![A-Za-z0-9])")  # fallback, see team_key_re()
SECRET_PATTERNS = [
    re.compile(r"sk-lf-[A-Za-z0-9-]{8,}"),
    re.compile(r"pk-lf-[A-Za-z0-9-]{8,}"),
    re.compile(r"lin_api_[A-Za-z0-9]{16,}"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}"),
    re.compile(r"github_pat_[A-Za-z0-9_]{20,}"),
    re.compile(r"sk-ant-[A-Za-z0-9_-]{16,}"),
    re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----"),
]
GIT_HOOK_MARK = "# managed by Tools/Workflow/workflow.py"


class WorkflowError(Exception):
    pass


# ---------------------------------------------------------------- utilities

def now_iso():
    return dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def parse_ts(value):
    return dt.datetime.fromisoformat(value.replace("Z", "+00:00"))


def read_json(path, default=None):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        return default


def write_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = f"{path}.{os.getpid()}.tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")
    os.replace(tmp, path)


class Lock:
    """Cross-process lock (hooks for Stop and SubagentStop may run at the same time)."""

    def __init__(self, name="state", timeout=20.0):
        self.path = os.path.join(STATE, f".{name}.lock")
        self.timeout = timeout

    def __enter__(self):
        os.makedirs(STATE, exist_ok=True)
        deadline = time.time() + self.timeout
        while True:
            try:
                self.fd = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
                self.token = f"{os.getpid()}-{uuid.uuid4().hex}"
                os.write(self.fd, self.token.encode())
                return self
            except FileExistsError:
                try:
                    if time.time() - os.path.getmtime(self.path) > 300:  # stale lock from a killed process
                        os.remove(self.path)
                        continue
                except OSError:
                    pass
                if time.time() > deadline:
                    raise WorkflowError(f"state is locked ({self.path}); remove it if no workflow command runs")
                time.sleep(0.1)

    def __exit__(self, *exc):
        os.close(self.fd)
        try:
            with open(self.path, encoding="utf-8") as f:
                mine = f.read() == self.token
            if mine:  # never delete a lock another process took over after ours went stale
                os.remove(self.path)
        except OSError:
            pass


def log(message):
    os.makedirs(STATE, exist_ok=True)
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(f"{now_iso()} {message}\n")


def team_key_re(cfg=None):
    """Matches issue ids of the configured Linear team only (`PART-2`, `UTF-8` are not issues)."""
    key = ((cfg or load_config()).get("linear") or {}).get("team_key")
    if not key:
        return ISSUE_RE
    return re.compile(rf"(?<![A-Za-z0-9])({re.escape(key)}-\d+)(?![A-Za-z0-9])", re.I)


def load_config():
    cfg = read_json(CONFIG_FILE, {}) or {}
    cfg.setdefault("linear", {}).setdefault("statuses", {})
    cfg["linear"]["statuses"].setdefault("in_progress", "In Progress")
    cfg["linear"]["statuses"].setdefault("in_review", "In Review")
    cfg["linear"]["statuses"].setdefault("done", "Done")
    cfg.setdefault("git", {}).setdefault("base", "main")
    cfg["git"].setdefault("remote", "origin")
    cfg.setdefault("langfuse", {}).setdefault("environment", "default")
    cfg.setdefault("checks", [])
    cfg.setdefault("auto_draft_pr", True)
    cfg.setdefault("context_warning_tokens", 150000)
    cfg.setdefault("guarded_paths", ["Assets/", "Packages/", "ProjectSettings/"])
    return cfg


def load_env():
    """Environment variables win; the git-ignored .env at the project root fills the gaps."""
    env = {}
    path = os.path.join(ROOT, ".env")
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            for line in f:
                m = re.match(r"^\s*([A-Z0-9_]+)\s*=\s*(.*?)\s*$", line)
                if m and not line.lstrip().startswith("#"):
                    env[m.group(1)] = m.group(2).strip("\"'")
    env.update({k: v for k, v in os.environ.items() if v})
    return env


def run(cmd, check=True, cwd=None, timeout=600):
    proc = subprocess.run(cmd, cwd=cwd or ROOT, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", timeout=timeout)
    if check and proc.returncode != 0:
        raise WorkflowError(f"{' '.join(cmd)} failed ({proc.returncode}): {(proc.stderr or proc.stdout).strip()}")
    return proc


def git(*args, check=True):
    return run(["git", *args], check=check).stdout.strip()


def http_json(url, payload, headers, timeout=20):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, method="POST",
                                 headers={"Content-Type": "application/json", **headers})
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        body = resp.read().decode("utf-8") or "{}"
        return resp.status, json.loads(body)


# ---------------------------------------------------------------- attempts

def attempt_path(attempt_id):
    return os.path.join(ATTEMPTS_DIR, f"{attempt_id}.json")


def active_attempt():
    active = read_json(ACTIVE_FILE)
    if not active:
        return None
    return read_json(attempt_path(active["attempt_id"]))


def save_attempt(attempt):
    """Writes the attempt; transcripts registered meanwhile by another hook (Stop, SubagentStop) are kept."""
    try:
        disk = read_json(attempt_path(attempt["attempt_id"]), {}) or {}
    except (OSError, ValueError):
        disk = {}
    known = attempt.setdefault("transcripts", [])
    have = {t["path"] for t in known}
    known.extend(t for t in disk.get("transcripts", []) if t["path"] not in have)
    write_json(attempt_path(attempt["attempt_id"]), attempt)


def issue_record(identifier):
    return read_json(os.path.join(ISSUES_DIR, f"{identifier}.json"),
                     {"issue": identifier, "attempts": [], "prs": []})


def save_issue_record(rec):
    write_json(os.path.join(ISSUES_DIR, f"{rec['issue']}.json"), rec)


# ---------------------------------------------------------------- Linear

class Linear:
    def __init__(self, env):
        self.key = env.get("LINEAR_API_KEY")
        self.url = env.get("LINEAR_API_URL", "https://api.linear.app/graphql")
        if not self.key:
            raise WorkflowError("LINEAR_API_KEY is not set (environment or .env); see Docs/agent-workflow.md")

    def query(self, query, variables):
        try:
            status, body = http_json(self.url, {"query": query, "variables": variables}, {"Authorization": self.key})
        except urllib.error.HTTPError as e:
            raise WorkflowError(f"Linear API HTTP {e.code}: {e.read().decode('utf-8', 'replace')[:300]}")
        except (urllib.error.URLError, TimeoutError, OSError) as e:
            raise WorkflowError(f"Linear API unreachable: {e}")
        if body.get("errors"):
            raise WorkflowError(f"Linear API error: {body['errors'][0].get('message')}")
        return body["data"]

    def issue(self, identifier):
        data = self.query("""query($id: String!) { issue(id: $id) {
            id identifier title url branchName state { id name type }
            team { id key name states { nodes { id name type position } } } } }""", {"id": identifier})
        if not data.get("issue"):
            raise WorkflowError(f"Linear issue {identifier} not found")
        return data["issue"]

    def set_state(self, issue_id, state_id):
        data = self.query("""mutation($id: String!, $s: String!) { issueUpdate(id: $id, input: { stateId: $s }) {
            success issue { state { name } } } }""", {"id": issue_id, "s": state_id})
        if not data["issueUpdate"]["success"]:
            raise WorkflowError("Linear refused the status change")
        return data["issueUpdate"]["issue"]["state"]["name"]


def resolve_states(issue, cfg):
    """Maps the workflow roles to the team's real statuses: configured name first, then the status type."""
    states = sorted(issue["team"]["states"]["nodes"], key=lambda s: s.get("position", 0))
    names = cfg["linear"]["statuses"]
    wanted = {"in_progress": "started", "in_review": "started", "done": "completed"}
    result = {}
    for role, stype in wanted.items():
        match = next((s for s in states if s["name"].lower() == names[role].lower()), None)
        if not match and role != "in_review":  # In Review has no distinct type; never guess it
            match = next((s for s in states if s["type"] == stype), None)
        if not match:
            raise WorkflowError(f"team {issue['team']['key']} has no status for '{role}' "
                                f"(looked for '{names[role]}'); set it in Tools/Workflow/config.json")
        result[role] = match
    return result


# ---------------------------------------------------------------- GitHub (gh)

def gh(*args, check=True):
    try:
        # which() honours PATHEXT, so a gh.cmd shim is found on Windows as well
        return run([shutil.which("gh") or "gh", *args], check=check)
    except FileNotFoundError:
        raise WorkflowError("GitHub CLI 'gh' is not installed or not on PATH")


def open_pr_for_branch(branch):
    out = gh("pr", "list", "--head", branch, "--state", "open", "--json", "number,url,title,isDraft").stdout
    prs = json.loads(out or "[]")
    return prs[0] if prs else None


# ---------------------------------------------------------------- usage collection

def transcript_files(path, session_id=None):
    files = [path] if path and os.path.exists(path) else []
    if path:
        base = os.path.dirname(path)
        sid = session_id or os.path.splitext(os.path.basename(path))[0]
        files += sorted(glob.glob(os.path.join(base, sid, "subagents", "*.jsonl")))
    return files


def read_replies(path):
    """Yields one record per assistant API reply. Only id, model, time and usage are read; content is skipped."""
    agent = "subagent" if os.sep + "subagents" + os.sep in path or "/subagents/" in path else "main"
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue
            if entry.get("type") != "assistant":
                continue
            msg = entry.get("message") or {}
            mid = msg.get("id")
            if not mid or msg.get("model") == "<synthetic>" or not entry.get("timestamp"):
                continue
            yield {"id": mid, "timestamp": entry["timestamp"], "model": msg.get("model"),
                   "usage": usage_fields(msg.get("usage")), "agent": agent,
                   "session_id": entry.get("sessionId")}


def read_events(path):
    """Yields the user turns and context compactions of a main transcript. Only the entry id, kind and time are
    kept; message content is looked at for its type (a tool result is not a user turn) and never stored."""
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue
            kind = None
            if entry.get("type") == "user" and not entry.get("isMeta") and not entry.get("isCompactSummary"):
                content = (entry.get("message") or {}).get("content")
                if isinstance(content, str) or (isinstance(content, list) and not any(
                        isinstance(c, dict) and c.get("type") == "tool_result" for c in content)):
                    kind = "user_message"
            elif entry.get("type") == "system" and entry.get("subtype") == "compact_boundary":
                kind = "compaction"
            if kind and entry.get("uuid") and entry.get("timestamp"):
                yield {"id": entry["uuid"], "kind": kind, "timestamp": entry["timestamp"]}


def context_tokens(usage):
    """Prompt size of one reply: everything the model read, cached or not."""
    return sum(usage.get(k, 0) for k in ("input", "cache_read_input_tokens", "cache_creation_input_tokens"))


def usage_fields(u):
    if not isinstance(u, dict):
        return None
    keys = {"input_tokens": "input", "output_tokens": "output",
            "cache_read_input_tokens": "cache_read_input_tokens",
            "cache_creation_input_tokens": "cache_creation_input_tokens"}
    out = {dst: u[src] for src, dst in keys.items() if isinstance(u.get(src), int)}
    return out or None


def collect_usage(attempt):
    """Merges replies inside the attempt window into the attempt ledger (keyed by message id, so re-runs and
    re-sent hooks never double count). Returns the ledger."""
    ledger_file = os.path.join(USAGE_DIR, f"{attempt['attempt_id']}.json")
    ledger = read_json(ledger_file, {}) or {}
    events = read_json(events_path(attempt), {}) or {}
    start = parse_ts(attempt["started_at"])
    end = parse_ts(attempt["ended_at"]) if attempt.get("ended_at") else None
    outside = lambda value: parse_ts(value) < start or (end and parse_ts(value) > end)
    for t in attempt.get("transcripts", []):
        for path in transcript_files(t.get("path"), t.get("session_id")):
            for rec in read_replies(path):
                if outside(rec["timestamp"]):
                    continue
                prev = ledger.get(rec["id"])
                # Streaming writes the same reply several times; keep the entry with the most output tokens.
                if prev and (prev.get("usage") or {}).get("output", -1) > (rec.get("usage") or {}).get("output", -1):
                    continue
                ledger[rec["id"]] = rec
        if t.get("path") and os.path.exists(t["path"]):
            events.update({ev["id"]: ev for ev in read_events(t["path"]) if not outside(ev["timestamp"])})
    write_json(ledger_file, ledger)
    write_json(events_path(attempt), events)
    return ledger


def events_path(attempt):
    return os.path.join(USAGE_DIR, f"{attempt['attempt_id']}.events.json")


def activity_summary(attempt):
    """User turns (a rework signal) and context compactions; None when no transcript was ever registered."""
    if not attempt.get("transcripts"):
        return None
    kinds = [ev["kind"] for ev in (read_json(events_path(attempt), {}) or {}).values()]
    return {"user_messages": kinds.count("user_message"), "compactions": kinds.count("compaction")}


def diff_stats(cfg, branch):
    """Files and lines the issue branch changes since it left the base branch; None when git can't tell."""
    base = f"{cfg['git']['remote']}/{cfg['git']['base']}"
    proc = run(["git", "diff", "--shortstat", f"{base}...{branch}"], check=False)
    if proc.returncode != 0:
        return None
    found = {kind: int(n) for n, kind in re.findall(r"(\d+) (file|insertion|deletion)", proc.stdout)}
    return {"files": found.get("file", 0), "insertions": found.get("insertion", 0),
            "deletions": found.get("deletion", 0)}


def register_transcript(attempt, path, session_id):
    if not path:
        return False
    known = attempt.setdefault("transcripts", [])
    if any(t["path"] == path for t in known):
        return False
    known.append({"path": path, "session_id": session_id, "registered_at": now_iso()})
    return True


def usage_summary(ledger):
    totals = {"input": 0, "output": 0, "cache_read_input_tokens": 0, "cache_creation_input_tokens": 0}
    with_usage = [r for r in ledger.values() if r.get("usage")]
    for r in with_usage:
        for k in totals:
            totals[k] += r["usage"].get(k, 0)
    models = sorted({r["model"] for r in ledger.values() if r.get("model")})
    sub = [r for r in ledger.values() if r.get("agent") == "subagent"]
    sub_tokens = {k: sum(r["usage"].get(k, 0) for r in sub if r.get("usage")) for k in totals}
    contexts = [context_tokens(r["usage"]) for r in with_usage if r.get("agent") != "subagent"]
    return {"replies": len(ledger), "replies_with_usage": len(with_usage),
            "replies_without_usage": len(ledger) - len(with_usage), "models": models,
            "tokens": totals if with_usage else None,
            "subagents": {"replies": len(sub), "share": round(len(sub) / len(ledger), 3),
                          "tokens": sub_tokens if any(r.get("usage") for r in sub) else None} if ledger else None,
            "context": {"avg": sum(contexts) // len(contexts), "max": max(contexts)} if contexts else None,
            "usage_status": "available" if with_usage and len(with_usage) == len(ledger)
            else ("partial" if with_usage else "unavailable")}


# ---------------------------------------------------------------- Langfuse (OTLP/JSON)

def nanos(ts):
    t = parse_ts(ts)
    return str(int(t.timestamp()) * 1_000_000_000 + t.microsecond * 1000)


def otlp_value(v):
    if isinstance(v, bool):
        return {"boolValue": v}
    if isinstance(v, int):
        return {"intValue": str(v)}
    if isinstance(v, (list, tuple)):
        return {"arrayValue": {"values": [otlp_value(x) for x in v]}}
    if isinstance(v, dict):
        return {"stringValue": json.dumps(v)}
    return {"stringValue": str(v)}


def span_id(attempt_id, *parts):
    return uuid.uuid5(NAMESPACE, "|".join((attempt_id,) + parts)).hex[:16]


def build_payload(attempt, ledger, cfg):
    """One trace per attempt: an agent span for the attempt and a generation per model reply.

    Attributes are identifiers, model, timestamps and token counts only. Ids are derived from the attempt and
    message ids, so a resend carries the same span ids instead of new ones."""
    aid = attempt["attempt_id"]
    trace_id = uuid.uuid5(NAMESPACE, aid).hex
    summary = usage_summary(ledger)
    end = attempt.get("ended_at") or now_iso()
    common = {
        "langfuse.trace.name": f"{attempt['issue']} implementation",
        "langfuse.session.id": attempt["issue"],
        "langfuse.trace.tags": ["claude-code", "implementation-attempt", attempt["issue"]],
        "langfuse.environment": cfg["langfuse"]["environment"],
        "langfuse.trace.metadata.issue": attempt["issue"],
        "langfuse.trace.metadata.attempt_id": aid,
        "langfuse.trace.metadata.branch": attempt.get("branch") or "",
        "langfuse.trace.metadata.pr_url": (attempt.get("pr") or {}).get("url") or "",
        "langfuse.trace.metadata.usage_status": summary["usage_status"],
    }
    root = span_id(aid, "root")
    root_attrs = {**common, "langfuse.observation.type": "agent",
                  "langfuse.observation.metadata.outcome": attempt.get("outcome") or "open",
                  "langfuse.observation.metadata.replies": summary["replies"],
                  "langfuse.observation.metadata.replies_without_usage": summary["replies_without_usage"],
                  "langfuse.observation.metadata.models": summary["models"]}
    # Missing data stays "unavailable" instead of turning into zeros.
    for key in ("tokens", "subagents", "context"):
        if summary[key] is not None:
            root_attrs[f"langfuse.observation.metadata.{key}"] = summary[key]
    activity = activity_summary(attempt)
    for key in ("user_messages", "compactions"):
        root_attrs[f"langfuse.observation.metadata.{key}"] = activity[key] if activity else "unavailable"
    if "diff" in attempt:  # measured when the attempt ends
        root_attrs["langfuse.observation.metadata.diff"] = attempt["diff"] or "unavailable"
    spans = [{"traceId": trace_id, "spanId": root, "name": f"{attempt['issue']} attempt {aid[:8]}", "kind": 1,
              "startTimeUnixNano": nanos(attempt["started_at"]), "endTimeUnixNano": nanos(end),
              "attributes": [{"key": k, "value": otlp_value(v)} for k, v in root_attrs.items()]}]
    for rec in sorted(ledger.values(), key=lambda r: r["timestamp"]):
        attrs = {**common, "langfuse.observation.type": "generation",
                 "langfuse.observation.model.name": rec.get("model") or "unknown",
                 "langfuse.observation.metadata.agent": rec.get("agent"),
                 "langfuse.observation.metadata.session_id": rec.get("session_id") or ""}
        if rec.get("usage"):
            attrs["langfuse.observation.usage_details"] = rec["usage"]
        else:
            attrs["langfuse.observation.metadata.usage_status"] = "unavailable"  # missing, not zero
        spans.append({"traceId": trace_id, "spanId": span_id(aid, rec["id"]), "parentSpanId": root,
                      "name": "model-reply", "kind": 1, "startTimeUnixNano": nanos(rec["timestamp"]),
                      "endTimeUnixNano": nanos(rec["timestamp"]),
                      "attributes": [{"key": k, "value": otlp_value(v)} for k, v in attrs.items()]})
    resource = {"attributes": [{"key": "service.name", "value": {"stringValue": "claude-code-workflow"}}]}
    scope = {"name": "rosie.workflow", "version": "1"}
    return [{"resourceSpans": [{"resource": resource, "scopeSpans": [{"scope": scope, "spans": spans[i:i + 200]}]}]}
            for i in range(0, len(spans), 200)]


def queue_payload(attempt, ledger, cfg):
    write_json(os.path.join(OUTBOX_DIR, f"{attempt['attempt_id']}.json"),
               {"attempt_id": attempt["attempt_id"], "issue": attempt["issue"], "queued_at": now_iso(),
                "chunks": build_payload(attempt, ledger, cfg)})


def send_outbox(env, only=None, timeout=20):
    """Sends queued payloads; a payload is removed only after Langfuse accepted every chunk."""
    results = []
    files = sorted(glob.glob(os.path.join(OUTBOX_DIR, "*.json")))
    if only:
        files = [f for f in files if os.path.basename(f) == f"{only}.json"]
    if not files:
        return results
    pk, sk = env.get("LANGFUSE_PUBLIC_KEY"), env.get("LANGFUSE_SECRET_KEY")
    if not pk or not sk:
        return [(os.path.basename(f), False, "LANGFUSE_PUBLIC_KEY / LANGFUSE_SECRET_KEY are not set") for f in files]
    url = env.get("LANGFUSE_HOST", "https://cloud.langfuse.com").rstrip("/") + "/api/public/otel/v1/traces"
    auth = "Basic " + base64.b64encode(f"{pk}:{sk}".encode()).decode()
    for path in files:
        try:
            item = read_json(path)
            if not isinstance(item, dict) or "chunks" not in item:
                raise ValueError("unexpected payload")
        except (OSError, ValueError) as e:
            results.append((os.path.basename(path)[:-5], False, f"unreadable outbox file {path}: {e}"))
            continue
        try:
            for chunk in item["chunks"]:
                status, body = http_json(url, chunk, {"Authorization": auth, "x-langfuse-ingestion-version": "4"},
                                         timeout=timeout)
                rejected = int(((body or {}).get("partialSuccess") or {}).get("rejectedSpans") or 0)
                if rejected:
                    raise WorkflowError(f"Langfuse rejected {rejected} spans: "
                                        f"{body['partialSuccess'].get('errorMessage')}")
            sent_dir = os.path.join(STATE, "sent")
            os.makedirs(sent_dir, exist_ok=True)
            item["sent_at"] = now_iso()
            write_json(os.path.join(sent_dir, os.path.basename(path)), item)
            os.remove(path)
            results.append((item["attempt_id"], True, "sent"))
        except urllib.error.HTTPError as e:
            results.append((item["attempt_id"], False, f"HTTP {e.code}: {e.read().decode('utf-8', 'replace')[:200]}"))
        except (urllib.error.URLError, TimeoutError, OSError, WorkflowError) as e:
            results.append((item["attempt_id"], False, str(e)))
    return results


# ---------------------------------------------------------------- commands

def cmd_start(args):
    cfg, env = load_config(), load_env()
    identifier = args.issue.upper()
    with Lock():
        current = active_attempt()
        if current and current["issue"] != identifier:
            raise WorkflowError(f"attempt {current['attempt_id'][:8]} for {current['issue']} is still active; "
                                f"run handoff or abandon first")
    linear = Linear(env)
    issue = linear.issue(identifier)
    states = resolve_states(issue, cfg)
    print(f"{issue['identifier']}: {issue['title']} [{issue['state']['name']}] {issue['url']}")
    print("statuses: " + ", ".join(f"{k}={v['name']}" for k, v in states.items()))

    if current and current["issue"] == identifier:
        print(f"attempt {current['attempt_id']} is already active on {current['branch']}; nothing to do")
        return

    remote, base = cfg["git"]["remote"], cfg["git"]["base"]
    branch = issue.get("branchName") or identifier.lower()
    dirty = git("status", "--porcelain", "--untracked-files=no")
    if dirty and not args.allow_dirty:
        raise WorkflowError("working tree has uncommitted changes; commit or stash them first "
                            "(they would leak into this issue's branch)")

    if issue["state"]["id"] != states["in_progress"]["id"]:
        name = linear.set_state(issue["id"], states["in_progress"]["id"])
        print(f"Linear: {identifier} -> {name}")

    try:
        if git("branch", "--list", branch):
            git("switch", branch)
        else:
            fetched = run(["git", "fetch", remote, base], check=False).returncode == 0
            if run(["git", "ls-remote", "--exit-code", "--heads", remote, branch], check=False).returncode == 0:
                git("fetch", remote, branch)
                git("switch", "-c", branch, "--track", f"{remote}/{branch}")
            else:
                git("switch", "-c", branch, f"{remote}/{base}" if fetched else base)
    except WorkflowError as e:
        raise WorkflowError(f"Linear status is In Progress, but the branch could not be prepared: {e}")
    print(f"git: on branch {branch}")

    rec = issue_record(identifier)
    pr = None
    try:
        pr = open_pr_for_branch(branch)
    except WorkflowError as e:
        print(f"note: could not look up an open PR ({e})")
    if not pr and rec["prs"]:
        pr = rec["prs"][-1]
    attempt = {"attempt_id": str(uuid.uuid4()), "issue": identifier, "issue_uuid": issue["id"],
               "title": issue["title"], "url": issue["url"], "branch": branch, "started_at": now_iso(),
               "ended_at": None, "outcome": None, "pr": pr, "transcripts": [], "handoff": {},
               "previous_attempts": list(rec["attempts"]), "states": {k: v["name"] for k, v in states.items()}}
    if os.environ.get("CLAUDE_TRANSCRIPT_PATH"):
        register_transcript(attempt, os.environ["CLAUDE_TRANSCRIPT_PATH"], os.environ.get("CLAUDE_SESSION_ID"))
    with Lock():
        save_attempt(attempt)
        write_json(ACTIVE_FILE, {"attempt_id": attempt["attempt_id"], "issue": identifier})
        rec["attempts"].append(attempt["attempt_id"])
        save_issue_record(rec)
        queue_payload(attempt, {}, cfg)  # opens the attempt in Langfuse right away (updated at handoff)
    print(f"attempt {attempt['attempt_id']} started" + (f" (continues PR {pr['url']})" if pr else ""))
    for aid, ok, detail in send_outbox(env, only=attempt["attempt_id"], timeout=10):
        print(f"Langfuse: {'attempt opened' if ok else 'not reachable, queued locally (' + detail + ')'}")


def run_checks(cfg, reported):
    results = []
    for check in cfg["checks"]:
        name = check["name"]
        missing = [v for v in check.get("requires_env", []) if not os.environ.get(v)]
        lock = check.get("skip_if_exists")
        if missing:
            results.append({"name": name, "status": "skipped", "detail": f"{', '.join(missing)} not set"})
            continue
        if lock and os.path.exists(os.path.join(ROOT, lock)):
            results.append({"name": name, "status": "skipped", "detail": f"{lock} exists ({check.get('skip_reason', '')})"})
            continue
        try:
            cmd = [os.path.expandvars(a) for a in check["command"]]
            proc = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True,
                                  encoding="utf-8", errors="replace", timeout=check.get("timeout", 1800))
            tail = (proc.stdout + proc.stderr).strip().splitlines()[-5:]
            results.append({"name": name, "status": "passed" if proc.returncode == 0 else "failed",
                            "detail": f"exit {proc.returncode}: " + " | ".join(tail)})
        except subprocess.TimeoutExpired:
            results.append({"name": name, "status": "failed", "detail": "timed out"})
        except OSError as e:
            results.append({"name": name, "status": "failed", "detail": f"could not run: {e}"})
    for text in reported or []:
        name, _, detail = text.partition(":")
        # "0 failed" / "failures: 0" are passing summaries; only a non-zero count or a bare "fail" word is a failure
        cleaned = re.sub(r"\b0+\s+(?:failed|failures?|errors?)\b|\b(?:failed|failures?|errors?)\s*[:=]\s*0+\b",
                         "", detail, flags=re.I)
        status = "failed" if re.search(r"\bfail", cleaned, re.I) else "reported"
        results.append({"name": name.strip(), "status": status, "detail": detail.strip() + " (reported by agent)"})
    return results


def pr_body(attempt, checks):
    lines = [f"Linear: [{attempt['issue']}]({attempt['url']}) {attempt['title']}", "",
             "## Checks", ""]
    lines += [f"- **{c['name']}**: {c['status']} — {c['detail']}" for c in checks] or ["- none configured or reported"]
    attempts = attempt.get("previous_attempts", []) + [attempt["attempt_id"]]
    lines += ["", f"Implementation attempts: {', '.join(a[:8] for a in attempts)}", "",
              "🤖 Generated with [Claude Code](https://claude.com/claude-code)"]
    return "\n".join(lines)


def step(attempt, name, status, detail=""):
    attempt["handoff"][name] = {"status": status, "detail": detail, "at": now_iso()}
    with Lock():
        save_attempt(attempt)
    print(f"[{status:>7}] {name}" + (f": {detail}" if detail else ""))


def cmd_handoff(args):
    cfg, env = load_config(), load_env()
    attempt = active_attempt()
    if not attempt:
        pending = send_outbox(env)
        for aid, ok, detail in pending:
            print(f"Langfuse {aid[:8]}: {'sent' if ok else 'still pending: ' + detail}")
        if not pending:
            raise WorkflowError("no active attempt; run start ISSUE first")
        return
    if args.transcript:
        register_transcript(attempt, args.transcript, None)
        with Lock():
            save_attempt(attempt)
    remote, base = cfg["git"]["remote"], cfg["git"]["base"]
    branch = git("rev-parse", "--abbrev-ref", "HEAD")
    if branch != attempt["branch"]:
        raise WorkflowError(f"current branch is {branch}, the attempt works on {attempt['branch']}")
    attempt["handoff"] = {}

    checks = run_checks(cfg, args.check_result)
    failed = [c for c in checks if c["status"] == "failed"]
    step(attempt, "checks", "failed" if failed else "ok",
         "; ".join(f"{c['name']}={c['status']}" for c in checks) or "none configured or reported")
    attempt["checks"] = checks
    with Lock():
        save_attempt(attempt)
    if failed and not args.allow_failed_checks:
        raise WorkflowError("checks failed; fix them or pass --allow-failed-checks. Issue stays In Progress.")

    if git("status", "--porcelain", "--untracked-files=no"):
        step(attempt, "commits", "failed", "uncommitted changes")
        raise WorkflowError("commit (or stash) your changes before handoff")
    run(["git", "fetch", remote, base], check=False)
    ahead = git("rev-list", "--count", f"{remote}/{base}..HEAD", check=False) or "0"
    if ahead == "0":
        step(attempt, "commits", "failed", f"no commits ahead of {remote}/{base}")
        raise WorkflowError("nothing to hand off")
    step(attempt, "commits", "ok", f"{ahead} commit(s) ahead of {remote}/{base}")

    try:
        git("push", "-u", remote, branch)
        step(attempt, "push", "ok")
    except WorkflowError as e:
        step(attempt, "push", "failed", str(e))
        raise WorkflowError("push failed; the issue stays In Progress, run handoff again")

    try:
        body = pr_body(attempt, checks)
        pr = open_pr_for_branch(branch)
        if pr:
            gh("pr", "edit", str(pr["number"]), "--body", body)
            was_draft = bool(pr.get("isDraft"))
            if was_draft:
                gh("pr", "ready", str(pr["number"]))
                pr["isDraft"] = False
            step(attempt, "pr", "ok", f"updated {pr['url']}" + (" (draft marked ready for review)" if was_draft else ""))
        else:
            out = gh("pr", "create", "--base", base, "--head", branch,
                     "--title", f"{attempt['issue']}: {attempt['title']}", "--body", body).stdout.strip()
            pr = open_pr_for_branch(branch) or {"url": out.splitlines()[-1] if out else ""}
            step(attempt, "pr", "ok", f"created {pr['url']}")
        attempt["pr"] = pr
        save_attempt(attempt)
        rec = issue_record(attempt["issue"])
        if pr.get("url") and all(p.get("url") != pr["url"] for p in rec["prs"]):
            rec["prs"].append(pr)
            save_issue_record(rec)
    except WorkflowError as e:
        step(attempt, "pr", "failed", str(e))
        raise WorkflowError("PR could not be created/updated; the issue stays In Progress, run handoff again")

    try:
        linear = Linear(env)
        issue = linear.issue(attempt["issue"])
        target = resolve_states(issue, cfg)["in_review"]
        name = target["name"] if issue["state"]["id"] == target["id"] else linear.set_state(issue["id"], target["id"])
        step(attempt, "linear", "ok", f"{attempt['issue']} -> {name}")
    except WorkflowError as e:
        step(attempt, "linear", "failed", str(e))
        raise WorkflowError("PR is ready but the Linear status was not changed; run handoff again")

    with Lock():
        attempt["ended_at"] = now_iso()
        attempt["outcome"] = "handed_off"
        attempt["diff"] = diff_stats(cfg, branch)
        ledger = collect_usage(attempt)
        save_attempt(attempt)
        queue_payload(attempt, ledger, cfg)
        os.remove(ACTIVE_FILE)
    summary = usage_summary(ledger)
    print(f"usage: {summary['replies']} replies, status {summary['usage_status']}, tokens {summary['tokens']}")
    for aid, ok, detail in send_outbox(env, only=attempt["attempt_id"]):
        if ok:
            step(attempt, "langfuse", "ok", "usage sent, attempt closed")
        else:
            step(attempt, "langfuse", "failed", f"queued in .agent-workflow/outbox ({detail}); run flush later")


def cmd_abandon(args):
    cfg, env = load_config(), load_env()
    with Lock():
        attempt = active_attempt()
        if not attempt:
            raise WorkflowError("no active attempt")
        attempt["ended_at"] = now_iso()
        attempt["outcome"] = "abandoned"
        attempt["diff"] = diff_stats(cfg, attempt["branch"])
        ledger = collect_usage(attempt)
        save_attempt(attempt)
        queue_payload(attempt, ledger, cfg)
        os.remove(ACTIVE_FILE)
    print(f"attempt {attempt['attempt_id']} abandoned; Linear status unchanged")
    for aid, ok, detail in send_outbox(env, only=attempt["attempt_id"]):
        print(f"Langfuse: {'sent' if ok else 'queued (' + detail + ')'}")


def cmd_collect(args):
    with Lock():
        attempt = active_attempt()
        if not attempt:
            print("no active attempt")
            return
        if args.transcript:
            register_transcript(attempt, args.transcript, None)
            save_attempt(attempt)
        s = usage_summary(collect_usage(attempt))
    print(json.dumps(s, indent=2))


def cmd_status(args):
    attempt = active_attempt()
    if attempt:
        ledger = read_json(os.path.join(USAGE_DIR, f"{attempt['attempt_id']}.json"), {}) or {}
        print(json.dumps({"active": {k: attempt[k] for k in ("attempt_id", "issue", "branch", "started_at", "pr",
                                                             "handoff", "previous_attempts")},
                          "auto_draft_pr": attempt.get("auto_pr"),
                          "transcripts": len(attempt.get("transcripts", [])), "usage": usage_summary(ledger)},
                         indent=2, ensure_ascii=False))
    else:
        print("no active attempt")
    pending = sorted(glob.glob(os.path.join(OUTBOX_DIR, "*.json")))
    print(f"unsent Langfuse payloads: {len(pending)}" + "".join(f"\n  {os.path.basename(p)}" for p in pending))


def cmd_flush(args):
    results = send_outbox(load_env())
    for aid, ok, detail in results:
        print(f"{aid}: {'sent' if ok else 'pending: ' + detail}")
    if not results:
        print("nothing to send")
    if any(not ok for _, ok, _ in results):
        sys.exit(1)


# ---------------------------------------------------------------- hooks

def hook_input():
    try:
        return json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        return {}


def rel_path(path):
    if not path:
        return ""
    full = os.path.abspath(path if os.path.isabs(path) else os.path.join(ROOT, path))
    try:
        return os.path.relpath(full, ROOT).replace("\\", "/")
    except ValueError:  # another drive on Windows
        return full.replace("\\", "/")


def hook_session_start(data):
    with Lock():
        attempt = active_attempt()
        if attempt and register_transcript(attempt, data.get("transcript_path"), data.get("session_id")):
            save_attempt(attempt)
    for aid, ok, detail in send_outbox(load_env(), timeout=5):
        log(f"session-start flush {aid}: {'sent' if ok else detail}")
    if attempt:
        branch = git("rev-parse", "--abbrev-ref", "HEAD", check=False)
        note = "" if branch == attempt["branch"] else f" WARNING: current branch is {branch}."
        print(f"[workflow] Active implementation attempt {attempt['attempt_id'][:8]} for {attempt['issue']} "
              f"({attempt['title']}) on branch {attempt['branch']}, started {attempt['started_at']}.{note} "
              f"Commit messages must contain {attempt['issue']}. Hand off with /handoff.")
    else:
        print("[workflow] No active implementation attempt. Discussion, planning and research need none; before "
              "changing project code run /impl-start <ISSUE-ID>.")


def hook_collect(data):
    with Lock():
        attempt = active_attempt()
        if not attempt:
            return
        if register_transcript(attempt, data.get("transcript_path"), data.get("session_id")):
            save_attempt(attempt)
        ledger = collect_usage(attempt)
        warning = context_warning(attempt, ledger, data) if data.get("hook_event_name") == "Stop" else None
    if warning:
        print(json.dumps({"systemMessage": warning}))
    if data.get("hook_event_name") == "Stop" and load_config()["auto_draft_pr"]:
        auto_draft_pr(attempt)


def context_warning(attempt, ledger, data):
    """Once per session and context window: a note when the last main reply read more than the configured
    context size, so the next phase starts after /compact or /clear instead of re-reading everything."""
    limit = load_config()["context_warning_tokens"]
    session = data.get("session_id")
    mine = [r for r in ledger.values() if r.get("usage") and r.get("agent") != "subagent"
            and (not session or r.get("session_id") == session)]
    if not limit or not mine:
        return None
    size = context_tokens(max(mine, key=lambda r: r["timestamp"])["usage"])
    key = f"{session}:{(activity_summary(attempt) or {}).get('compactions', 0)}"
    if size < limit or attempt.get("context_warned") == key:
        return None
    attempt["context_warned"] = key
    save_attempt(attempt)
    return (f"[workflow] Context is at {size // 1000}k tokens (limit {limit // 1000}k). Run /compact or /clear "
            f"before the next phase; every further reply re-reads the whole context.")


def auto_draft_pr(attempt):
    """After an agent reply: if the issue branch has commits that are not on the remote yet, push them and make
    sure a draft PR exists. This is not a handoff: the issue status, the attempt and Langfuse are not touched, and
    the PR becomes ready for review only in `handoff`. Failures are recorded and never block the session."""
    cfg = load_config()
    remote, base = cfg["git"]["remote"], cfg["git"]["base"]
    result = {"at": now_iso()}
    try:
        branch = git("rev-parse", "--abbrev-ref", "HEAD")
        if branch != attempt["branch"]:
            return
        if run(["git", "rev-parse", "--verify", "--quiet", f"{remote}/{base}"], check=False).returncode != 0:
            run(["git", "fetch", remote, base], check=False, timeout=30)
        if (git("rev-list", "--count", f"{remote}/{base}..HEAD", check=False) or "0") == "0":
            return  # nothing committed for the issue yet
        head = git("rev-parse", "HEAD")
        pushed = run(["git", "rev-parse", "--verify", "--quiet", f"{remote}/{branch}"], check=False).stdout.strip()
        known_pr = (attempt.get("pr") or {}).get("url")
        if head == pushed and known_pr and open_pr_for_branch(branch):
            return  # already pushed and its PR is still open: nothing new
        if head != pushed:
            git("push", "-u", remote, branch)
            result["pushed"] = head[:10]
        pr = open_pr_for_branch(branch)
        if pr:
            result.update(status="ok", detail=f"pushed to open PR {pr['url']}")
        else:
            body = ("Draft: implementation in progress, created automatically after an agent reply. It becomes ready "
                    "for review with the explicit handoff.\n\n" + pr_body(attempt, []))
            gh("pr", "create", "--draft", "--base", base, "--head", branch,
               "--title", f"{attempt['issue']}: {attempt['title']}", "--body", body)
            pr = open_pr_for_branch(branch) or {}
            result.update(status="ok", detail=f"created draft PR {pr.get('url')}")
        with Lock():
            fresh = active_attempt()
            if fresh and fresh["attempt_id"] == attempt["attempt_id"]:
                fresh["pr"] = pr or fresh.get("pr")
                fresh["auto_pr"] = result
                save_attempt(fresh)
                rec = issue_record(fresh["issue"])
                if pr.get("url") and all(p.get("url") != pr["url"] for p in rec["prs"]):
                    rec["prs"].append(pr)
                    save_issue_record(rec)
    except (WorkflowError, subprocess.TimeoutExpired) as e:
        result.update(status="failed", detail=str(e))
        log(f"auto draft PR failed: {e}")
        with Lock():
            fresh = active_attempt()
            if fresh and fresh["attempt_id"] == attempt["attempt_id"]:
                fresh["auto_pr"] = result
                save_attempt(fresh)


def deny(reason):
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny",
                                             "permissionDecisionReason": reason}}))


UNITY_READONLY_TOOLS = {"read_console", "find_gameobjects", "find_in_file", "get_sha", "validate_script",
                        "get_test_job", "debug_request_context", "unity_docs", "unity_reflect",
                        "manage_script_capabilities"}
UNITY_READONLY_ACTION = re.compile(r"^(get|list|find|read|search|query|inspect|describe|info|status|check|"
                                   r"validate|stats|view|show|preview|screenshot|capture)(?![a-z])", re.I)


def is_unity_mcp_readonly(tool, tin):
    """Unity MCP tools edit the project only through some actions; decide by the action field."""
    name = tool.lower().split("__", 2)[-1]
    if name in UNITY_READONLY_TOOLS:
        return True
    action = str(tin.get("action") or "").strip()
    return bool(action) and bool(UNITY_READONLY_ACTION.match(action))


def hook_guard(data):
    cfg = load_config()
    tool = data.get("tool_name", "")
    tin = data.get("tool_input") or {}
    if tool in ("Bash", "PowerShell"):
        cmd = tin.get("command", "")
        if re.search(r"\bgh\s+pr\s+merge\b", cmd) or re.search(r"\bgh\s+api\b.*\/merge\b", cmd):
            return deny("Merging is never automatic in this workflow; the user merges PRs on GitHub.")
        if re.search(r"\bgit\s+push\b.*(--force\b|\s-f\b)", cmd) and re.search(rf"\b{cfg['git']['base']}\b", cmd):
            return deny(f"Force-pushing {cfg['git']['base']} is not allowed.")
        return
    path = tin.get("file_path") or tin.get("notebook_path") or tin.get("path") or ""
    if tool.lower().startswith("mcp__unitymcp__"):
        if is_unity_mcp_readonly(tool, tin):
            return
        rel = "Assets/"  # Unity MCP write actions edit Assets/
    else:
        rel = rel_path(path)
    if any(rel.startswith(p) for p in cfg["guarded_paths"]) and not active_attempt():
        deny(f"No active implementation attempt. Changes to {rel} are implementation work: identify the Linear "
             f"issue and run /impl-start <ISSUE-ID> first (discussion and planning need no attempt).")


def hook_commit_msg(path):
    cfg = load_config()
    with open(path, encoding="utf-8", errors="replace") as f:
        text = "".join(l for l in f if not l.startswith("#"))
    attempt = active_attempt()
    staged = git("diff", "--cached", "--name-only", check=False).splitlines()
    if attempt:
        merge = text.startswith("Merge ")
        if not merge and not re.search(rf"(?<![A-Za-z0-9]){re.escape(attempt['issue'])}(?![A-Za-z0-9])", text, re.I):
            sys.stderr.write(f"workflow: commit message must contain {attempt['issue']} (active attempt)\n")
            sys.exit(1)
    elif any(any(s.startswith(p) for p in cfg["guarded_paths"]) for s in staged):
        if not text.startswith("Merge ") and not team_key_re(cfg).search(text):
            sys.stderr.write("workflow: project code is staged but there is no active attempt and no issue id in "
                             "the message; run start ISSUE first\n")
            sys.exit(1)


def hook_pre_commit():
    staged = git("diff", "--cached", "--name-only", "--diff-filter=ACMR", check=False).splitlines()
    problems = []
    for name in staged:
        base = os.path.basename(name)
        if base == ".env" or base.startswith(".env."):
            problems.append(f"{name}: env files must not be committed")
        if name.startswith(".agent-workflow/"):
            problems.append(f"{name}: local workflow state must not be committed")
    diff = git("diff", "--cached", "-U0", "--no-color", check=False)
    current = None
    for line in diff.splitlines():
        if line.startswith("+++ b/"):
            current = line[6:]
        elif line.startswith("+") and not line.startswith("+++"):
            for pat in SECRET_PATTERNS:
                if pat.search(line):
                    problems.append(f"{current}: looks like a secret ({pat.pattern})")
    if problems:
        sys.stderr.write("workflow: commit blocked\n  " + "\n  ".join(sorted(set(problems))) + "\n")
        sys.exit(1)


def cmd_hook(args):
    if os.environ.get("CLAUDE_WORKFLOW_DISABLED") == "1":
        return
    try:
        if args.event == "commit-msg":
            return hook_commit_msg(args.file)
        if args.event == "pre-commit":
            return hook_pre_commit()
        data = hook_input()
        {"session-start": hook_session_start, "collect": hook_collect, "guard": hook_guard}[args.event](data)
    except SystemExit:
        raise
    except Exception as e:  # a broken hook must not break the session; record it instead
        log(f"hook {args.event} failed: {e!r}")
        sys.stderr.write(f"workflow hook {args.event} failed: {e}\n")


def git_hooks_dir():
    hooks_dir = git("rev-parse", "--git-path", "hooks")
    return hooks_dir if os.path.isabs(hooks_dir) else os.path.join(ROOT, hooks_dir)


def merge_settings(path, template):
    """Adds the workflow hooks to .claude/settings.json and keeps every hook and setting already there."""
    strip_settings(path)  # re-install replaces older workflow hooks instead of duplicating them
    settings = read_json(path, {}) or {}
    hooks = settings.setdefault("hooks", {})
    for event, groups in template["hooks"].items():
        hooks.setdefault(event, []).extend(groups)
    write_json(path, settings)
    return len(template["hooks"])


def strip_settings(path):
    settings = read_json(path)
    if not settings:
        return 0
    removed = 0
    for event in list(settings.get("hooks", {})):
        groups = []
        for g in settings["hooks"][event]:
            kept = [h for h in g.get("hooks", []) if HOOK_MARK not in h.get("command", "")]
            removed += len(g.get("hooks", [])) - len(kept)
            if kept:
                groups.append({**g, "hooks": kept})
        if groups:
            settings["hooks"][event] = groups
        else:
            del settings["hooks"][event]
    write_json(path, settings)
    return removed


def copy_file(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(src, encoding="utf-8") as f:
        text = f.read()
    with open(dst, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def cmd_install(args):
    """Run by the user once per clone: Claude Code hooks, slash commands, git hooks, merge automation."""
    settings_path = os.path.join(ROOT, ".claude", "settings.json")
    added = merge_settings(settings_path, read_json(os.path.join(TEMPLATES, "settings.hooks.json")))
    print(f"{settings_path}: workflow hooks for {added} event(s) installed, other hooks kept")
    for name in sorted(os.listdir(os.path.join(TEMPLATES, "commands"))):
        dst = os.path.join(ROOT, ".claude", "commands", name)
        copy_file(os.path.join(TEMPLATES, "commands", name), dst)
        print(f"installed /{os.path.splitext(name)[0]} -> {dst}")
    if not args.no_action:
        dst = os.path.join(ROOT, ".github", "workflows", "linear-done-on-merge.yml")
        copy_file(os.path.join(TEMPLATES, "linear-done-on-merge.yml"), dst)
        print(f"installed {dst} (commit it and add the LINEAR_API_KEY repository secret)")
    hooks_dir = git_hooks_dir()
    os.makedirs(hooks_dir, exist_ok=True)
    for name, call in (("commit-msg", 'hook commit-msg "$1"'), ("pre-commit", "hook pre-commit")):
        path = os.path.join(hooks_dir, name)
        if os.path.exists(path) and GIT_HOOK_MARK not in open(path, encoding="utf-8", errors="replace").read():
            raise WorkflowError(f"{path} exists and is not managed by this workflow; merge it by hand")
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(f'#!/bin/sh\n{GIT_HOOK_MARK}\nexec sh "$(git rev-parse --show-toplevel)/Tools/Workflow/wf.sh" {call}\n')
        os.chmod(path, 0o755)
        print(f"installed {path}")


def cmd_uninstall(args):
    """Removes the workflow hooks and slash commands; other hooks and settings stay. The GitHub Action file and
    .agent-workflow/ are left for the user to delete (see Docs/agent-workflow.md)."""
    settings_path = os.path.join(ROOT, ".claude", "settings.json")
    print(f"{settings_path}: {strip_settings(settings_path)} workflow hook(s) removed")
    for name in os.listdir(os.path.join(TEMPLATES, "commands")):
        path = os.path.join(ROOT, ".claude", "commands", name)
        if os.path.exists(path):
            os.remove(path)
            print(f"removed {path}")
    hooks_dir = git_hooks_dir()
    for name in ("commit-msg", "pre-commit"):
        path = os.path.join(hooks_dir, name)
        if os.path.exists(path) and GIT_HOOK_MARK in open(path, encoding="utf-8", errors="replace").read():
            os.remove(path)
            print(f"removed {path}")


def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except AttributeError:
            pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("start")
    p.add_argument("issue")
    p.add_argument("--allow-dirty", action="store_true")
    p.set_defaults(func=cmd_start)
    p = sub.add_parser("handoff")
    p.add_argument("--check-result", action="append", metavar="'NAME: RESULT'",
                   help="result of a check the agent ran itself (e.g. Unity tests through Unity MCP)")
    p.add_argument("--allow-failed-checks", action="store_true")
    p.add_argument("--transcript", help="transcript to include if no hook registered it")
    p.set_defaults(func=cmd_handoff)
    for name, func in (("status", cmd_status), ("flush", cmd_flush), ("abandon", cmd_abandon),
                       ("uninstall", cmd_uninstall)):
        sub.add_parser(name).set_defaults(func=func)
    p = sub.add_parser("install")
    p.add_argument("--no-action", action="store_true", help="don't copy the GitHub Action")
    p.set_defaults(func=cmd_install)
    p = sub.add_parser("collect")
    p.add_argument("--transcript")
    p.set_defaults(func=cmd_collect)
    p = sub.add_parser("hook")
    p.add_argument("event", choices=["session-start", "collect", "guard", "commit-msg", "pre-commit"])
    p.add_argument("file", nargs="?")
    p.set_defaults(func=cmd_hook)
    args = ap.parse_args()
    try:
        args.func(args)
    except WorkflowError as e:
        sys.stderr.write(f"workflow: {e}\n")
        sys.exit(1)


if __name__ == "__main__":
    main()
