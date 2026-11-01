#!/usr/bin/env python3
"""Measures the AI agent's resource consumption per task phase and exports it for Langfuse.

The agent session writes one JSONL transcript per session (plus one per subagent). Every model
reply carries token usage. This script:

  mark <phase>      appends a phase boundary (UTC now) to phases.json; the previous phase ends there.
  snapshot          copies every reply found in the transcripts into ledger.jsonl (deduplicated by id).
  report            snapshot + aggregates usage per phase and writes usage_summary.json, REPORT.md and
                    otel/batch_NNN.json (OTLP/JSON spans for Langfuse's OpenTelemetry endpoint).
  import-batch F    seeds ledger.jsonl from an earlier legacy langfuse_batch.json.

Claude Code rewrites the session transcript when it compacts the context, so replies before the
compaction disappear from it. ledger.jsonl keeps them: run `snapshot` before compaction, or let a
Claude Code PreCompact hook run `snapshot --hook` (see Tools/Langfuse/README.md). Time ranges with no data are listed
in REPORT.md as coverage gaps.

Send the spans with Send-ToLangfuse.ps1 (Windows PowerShell). Langfuse accepts traces only over OpenTelemetry
(/api/public/otel/v1/traces); the legacy /api/public/ingestion endpoint no longer takes traces.

Effective cost weights (relative to one uncached input token) follow Anthropic's pricing ratios:
cache read 0.1x, cache write (1h TTL) 2x, output 5x.
"""
import argparse
import datetime as dt
import glob
import json
import os
import sys
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
PHASES_FILE = os.path.join(HERE, "phases.json")
LEDGER_FILE = os.path.join(HERE, "ledger.jsonl")
GAP_MINUTES = 30
WEIGHTS = {"input": 1.0, "cache_read": 0.1, "cache_write": 2.0, "output": 5.0}
TRACE_NAMESPACE = uuid.UUID("6f0d7c1e-0b7a-4c8e-9a51-7a2f3c9e1d42")


def utc_now():
    return dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def parse_ts(value):
    return dt.datetime.fromisoformat(value.replace("Z", "+00:00"))


def load_phases():
    if not os.path.exists(PHASES_FILE):
        return {"task": "", "issue": "", "transcriptGlob": "", "phases": []}
    with open(PHASES_FILE, encoding="utf-8") as f:
        return json.load(f)


def save_phases(data):
    with open(PHASES_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


def cmd_mark(args):
    data = load_phases()
    now = args.at or utc_now()
    if data["phases"] and not data["phases"][-1].get("end"):
        data["phases"][-1]["end"] = now
    if args.phase != "-":
        data["phases"].append({"name": args.phase, "start": now, "end": None})
    save_phases(data)
    print(f"marked '{args.phase}' at {now}")


def read_messages(paths):
    """Returns one record per API reply (deduplicated by message id; the last entry holds final usage)."""
    by_id = {}
    for path in paths:
        agent = "main" if os.path.basename(os.path.dirname(path)) == "projects" or "/subagents/" not in path else "subagent"
        source = os.path.splitext(os.path.basename(path))[0]
        with open(path, encoding="utf-8") as f:
            for line in f:
                try:
                    entry = json.loads(line)
                except json.JSONDecodeError:
                    continue
                if entry.get("type") != "assistant":
                    continue
                msg = entry.get("message") or {}
                usage = msg.get("usage")
                mid = msg.get("id")
                if not usage or not mid or msg.get("model") == "<synthetic>":
                    continue
                rec = by_id.setdefault(mid, {"id": mid, "tools": [], "agent": agent, "source": source})
                rec.update(timestamp=entry["timestamp"], model=msg.get("model"), usage=usage)
                for block in msg.get("content") or []:
                    if isinstance(block, dict) and block.get("type") == "tool_use":
                        rec["tools"].append(block.get("name"))
    return sorted(by_id.values(), key=lambda r: r["timestamp"])


def usage_numbers(u):
    cw = u.get("cache_creation_input_tokens") or 0
    out = {
        "input": u.get("input_tokens") or 0,
        "output": u.get("output_tokens") or 0,
        "cache_read": u.get("cache_read_input_tokens") or 0,
        "cache_write": cw,
        "thinking": (u.get("output_tokens_details") or {}).get("thinking_tokens") or 0,
        "web_search": (u.get("server_tool_use") or {}).get("web_search_requests") or 0,
        "web_fetch": (u.get("server_tool_use") or {}).get("web_fetch_requests") or 0,
    }
    out["effective"] = sum(out[k] * w for k, w in WEIGHTS.items())
    return out


def phase_of(ts, phases):
    t = parse_ts(ts)
    for p in phases:
        start = parse_ts(p["start"])
        end = parse_ts(p["end"]) if p.get("end") else None
        if t >= start and (end is None or t < end):
            return p["name"]
    return "(unassigned)"


def tool_group(name):
    if not name:
        return "other"
    if name.startswith("mcp__"):
        parts = name.split("__")
        return "mcp:" + (parts[1] if len(parts) > 1 else name)
    return name


def read_ledger():
    by_id = {}
    if os.path.exists(LEDGER_FILE):
        with open(LEDGER_FILE, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line:
                    rec = json.loads(line)
                    by_id[rec["id"]] = rec
    return by_id


def write_ledger(by_id):
    with open(LEDGER_FILE, "w", encoding="utf-8") as f:
        for rec in sorted(by_id.values(), key=lambda r: r["timestamp"]):
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")


def collect(pattern):
    """Merges the replies still in the transcripts into the ledger and returns all known replies."""
    paths = sorted(set(glob.glob(os.path.expanduser(pattern), recursive=True))) if pattern else []
    ledger = read_ledger()
    if not paths and not ledger:
        sys.exit(f"no transcripts match {pattern!r} and the ledger is empty")
    for rec in read_messages(paths):
        ledger[rec["id"]] = rec
    write_ledger(ledger)
    return paths, sorted(ledger.values(), key=lambda r: r["timestamp"])


def cmd_snapshot(args):
    data = load_phases()
    pattern = args.transcripts or data.get("transcriptGlob")
    if args.hook:
        # Claude Code hook input (PreCompact/Stop): {"transcript_path": "...", ...} on stdin.
        hook = json.load(sys.stdin)
        pattern = hook.get("transcript_path") or pattern
    paths, msgs = collect(pattern)
    print(f"ledger: {len(msgs)} replies ({len(paths)} transcript(s) read)")


def cmd_import_batch(args):
    with open(args.file, encoding="utf-8") as f:
        batch = json.load(f)["batch"]
    ledger = read_ledger()
    added = 0
    for e in batch:
        if e.get("type") != "generation-create":
            continue
        b = e["body"]
        u = b.get("usageDetails") or {}
        meta = b.get("metadata") or {}
        rid = "batch:" + b["id"]
        if rid in ledger:
            continue
        ledger[rid] = {"id": rid, "tools": meta.get("tools") or [],
                       "agent": "main" if b.get("name") == "agent-turn" else "subagent", "source": "imported",
                       "timestamp": b["endTime"], "model": b.get("model"),
                       "usage": {"input_tokens": u.get("input", 0), "output_tokens": u.get("output", 0),
                                 "cache_read_input_tokens": u.get("cache_read_input_tokens", 0),
                                 "cache_creation_input_tokens": u.get("cache_creation_input_tokens", 0),
                                 "output_tokens_details": {"thinking_tokens": meta.get("thinking_tokens", 0)}}}
        added += 1
    write_ledger(ledger)
    print(f"imported {added} replies into the ledger ({len(ledger)} total)")


def coverage_gaps(msgs, phases):
    """Time ranges inside the task window longer than GAP_MINUTES without any recorded reply."""
    if not msgs:
        return []
    marks = [parse_ts(m["timestamp"]) for m in msgs]
    start = min([parse_ts(p["start"]) for p in phases] + [marks[0]])
    gaps = []
    prev = start
    for t in marks:
        if (t - prev).total_seconds() > GAP_MINUTES * 60:
            gaps.append((prev, t))
        prev = max(prev, t)
    return [{"from": a.strftime("%Y-%m-%dT%H:%M:%SZ"), "to": b.strftime("%Y-%m-%dT%H:%M:%SZ"),
             "minutes": round((b - a).total_seconds() / 60)} for a, b in gaps]


def cmd_report(args):
    data = load_phases()
    paths, msgs = collect(args.transcripts or data.get("transcriptGlob"))
    phases = data["phases"]
    keys = ["input", "output", "cache_read", "cache_write", "thinking", "web_search", "web_fetch", "effective"]

    summary = {"task": data.get("task"), "issue": data.get("issue"), "generatedAt": utc_now(),
               "weights": WEIGHTS, "transcripts": len(paths), "replies": len(msgs), "phases": [], "total": {}}
    total = {k: 0 for k in keys}
    per_phase = {}
    for m in msgs:
        name = phase_of(m["timestamp"], phases)
        m["phase"] = name
        n = usage_numbers(m["usage"])
        p = per_phase.setdefault(name, {"replies": 0, "tools": {}, "models": {}, **{k: 0 for k in keys},
                                        "first": m["timestamp"], "last": m["timestamp"]})
        p["replies"] += 1
        p["last"] = m["timestamp"]
        p["models"][m["model"]] = p["models"].get(m["model"], 0) + 1
        for t in m["tools"]:
            g = tool_group(t)
            p["tools"][g] = p["tools"].get(g, 0) + 1
        for k in keys:
            p[k] += n[k]
            total[k] += n[k]

    order = [p["name"] for p in phases] + [k for k in per_phase if k not in {p["name"] for p in phases}]
    for name in order:
        if name not in per_phase:
            continue
        p = per_phase[name]
        ph = next((x for x in phases if x["name"] == name), {})
        start = ph.get("start") or p["first"]
        end = ph.get("end") or p["last"]
        p.update(name=name, start=start, end=end,
                 wallMinutes=round((parse_ts(end) - parse_ts(start)).total_seconds() / 60, 1),
                 effective=round(p["effective"]))
        summary["phases"].append(p)
    total["effective"] = round(total["effective"])
    summary["total"] = total
    summary["gaps"] = coverage_gaps(msgs, phases)

    out_dir = args.out or HERE
    os.makedirs(out_dir, exist_ok=True)
    with open(os.path.join(out_dir, "usage_summary.json"), "w", encoding="utf-8") as f:
        json.dump(summary, f, indent=2)
    otel_dir = os.path.join(out_dir, "otel")
    os.makedirs(otel_dir, exist_ok=True)
    for old in glob.glob(os.path.join(otel_dir, "batch_*.json")):
        os.remove(old)
    chunks = build_otlp(summary, msgs, data)
    for i, payload in enumerate(chunks):
        with open(os.path.join(otel_dir, f"batch_{i:03d}.json"), "w", encoding="utf-8") as f:
            json.dump(payload, f, ensure_ascii=False)
    legacy = os.path.join(out_dir, "langfuse_batch.json")
    if os.path.exists(legacy):
        os.remove(legacy)
    with open(os.path.join(out_dir, "REPORT.md"), "w", encoding="utf-8") as f:
        f.write(build_markdown(summary))
    print(f"{len(msgs)} replies from {len(paths)} transcript(s); effective tokens {total['effective']:,}")


OTLP_CHUNK = 150


def nanos(ts):
    t = parse_ts(ts)
    return str(int(t.timestamp()) * 1_000_000_000 + t.microsecond * 1000)


def otlp_value(v):
    if isinstance(v, bool):
        return {"boolValue": v}
    if isinstance(v, int):
        return {"intValue": str(v)}
    if isinstance(v, float):
        return {"doubleValue": v}
    if isinstance(v, (list, tuple)):
        return {"arrayValue": {"values": [otlp_value(x) for x in v]}}
    if isinstance(v, dict):
        return {"stringValue": json.dumps(v, ensure_ascii=False)}
    return {"stringValue": "" if v is None else str(v)}


def otlp_span(trace_id, span_id, parent, name, start, end, attrs):
    span = {"traceId": trace_id, "spanId": span_id, "name": name, "kind": 1,
            "startTimeUnixNano": nanos(start), "endTimeUnixNano": nanos(end),
            "attributes": [{"key": k, "value": otlp_value(v)} for k, v in attrs.items() if v is not None]}
    if parent:
        span["parentSpanId"] = parent
    return span


def build_otlp(summary, msgs, data):
    """OTLP/JSON payloads: a root agent span for the task, a span per phase, a generation span per reply.

    Langfuse reads langfuse.* span attributes (observation type, model, usage details, trace name and tags).
    Ids are derived from the issue, phase and reply ids, so sending again updates the same trace.
    """
    key = (data.get("issue") or "task") + "|" + (data.get("task") or "")
    trace_id = uuid.uuid5(TRACE_NAMESPACE, key).hex
    sid = lambda *parts: uuid.uuid5(TRACE_NAMESPACE, "|".join((key,) + parts)).hex[:16]
    trace_name = f"{data.get('issue')} {data.get('task')}".strip()
    common = {"langfuse.trace.name": trace_name,
              "langfuse.trace.tags": ["claude-agent", "resource-consumption", data.get("issue") or "task"],
              "langfuse.environment": "default"}
    phases = summary["phases"]
    first = min([p["start"] for p in phases] + ([msgs[0]["timestamp"]] if msgs else [])) if (phases or msgs) else utc_now()
    last = max([p["end"] for p in phases] + ([msgs[-1]["timestamp"]] if msgs else [])) if (phases or msgs) else first
    root = sid("root")
    t = summary["total"]
    spans = [otlp_span(trace_id, root, None, trace_name, first, last, {
        **common, "langfuse.observation.type": "agent",
        "langfuse.observation.output": {"total": t, "replies": summary["replies"], "gaps": summary.get("gaps", [])},
        "langfuse.observation.metadata.weights": WEIGHTS,
        "langfuse.observation.metadata.effective_tokens": t["effective"]})]
    phase_ids = {}
    for p in phases:
        phase_ids[p["name"]] = sid("phase", p["name"])
        spans.append(otlp_span(trace_id, phase_ids[p["name"]], root, p["name"], p["start"], p["end"], {
            **common, "langfuse.observation.type": "span",
            "langfuse.observation.output": {k: p[k] for k in ("replies", "input", "output", "cache_read",
                                                               "cache_write", "thinking", "effective")},
            "langfuse.observation.metadata.tools": p["tools"],
            "langfuse.observation.metadata.models": p["models"],
            "langfuse.observation.metadata.wall_minutes": p["wallMinutes"]}))
    prev_end = {}
    for m in msgs:
        n = usage_numbers(m["usage"])
        end = m["timestamp"]
        start = prev_end.get(m["source"], end)
        if parse_ts(start) > parse_ts(end):
            start = end
        prev_end[m["source"]] = end
        spans.append(otlp_span(trace_id, sid("reply", m["id"]), phase_ids.get(m["phase"], root),
                               "agent-turn" if m["agent"] == "main" else "subagent-turn", start, end, {
            **common, "langfuse.observation.type": "generation",
            "langfuse.observation.model.name": m["model"],
            "langfuse.observation.usage_details": {
                "input": n["input"], "output": n["output"],
                "cache_read_input_tokens": n["cache_read"], "cache_creation_input_tokens": n["cache_write"]},
            "langfuse.observation.metadata.tools": m["tools"],
            "langfuse.observation.metadata.thinking_tokens": n["thinking"],
            "langfuse.observation.metadata.effective_tokens": round(n["effective"], 1),
            "langfuse.observation.metadata.phase": m["phase"]}))
    resource = {"attributes": [{"key": "service.name", "value": {"stringValue": "claude-agent-usage"}}]}
    scope = {"name": "rosie.usage_report", "version": "2"}
    return [{"resourceSpans": [{"resource": resource, "scopeSpans": [{"scope": scope, "spans": spans[i:i + OTLP_CHUNK]}]}]}
            for i in range(0, len(spans), OTLP_CHUNK)]


def build_markdown(s):
    fmt = "{:,}".format
    lines = [f"# Resource consumption: {s['issue']} {s['task']}", "",
             f"Generated {s['generatedAt']} from {s['transcripts']} transcript(s), {s['replies']} model replies.", "",
             "Effective tokens weight each kind by its price relative to an uncached input token: "
             "cache read 0.1x, cache write 2x, output 5x.", "",
             "| Phase | Wall time, min | Replies | Output | Cache read | Cache write | Effective | Share |",
             "|---|---:|---:|---:|---:|---:|---:|---:|"]
    tot = s["total"]["effective"] or 1
    for p in s["phases"]:
        lines.append(f"| {p['name']} | {p['wallMinutes']} | {p['replies']} | {fmt(p['output'])} | "
                     f"{fmt(p['cache_read'])} | {fmt(p['cache_write'])} | {fmt(p['effective'])} | "
                     f"{p['effective'] * 100 // tot}% |")
    t = s["total"]
    lines += [f"| **Total** | | {s['replies']} | {fmt(t['output'])} | {fmt(t['cache_read'])} | "
              f"{fmt(t['cache_write'])} | {fmt(t['effective'])} | 100% |", ""]
    if s.get("gaps"):
        lines += ["## Coverage gaps", "",
                  f"No reply data for these ranges (longer than {GAP_MINUTES} min). Either the agent was idle "
                  "(waiting for the user) or the context was compacted before a snapshot, which drops earlier "
                  "replies from the transcript. The totals above are a lower bound.", ""]
        lines += [f"- {g['from']} → {g['to']} ({g['minutes']} min)" for g in s["gaps"]]
        lines.append("")
    lines += ["## Tool calls per phase", ""]
    for p in s["phases"]:
        top = sorted(p["tools"].items(), key=lambda kv: -kv[1])
        lines.append(f"- **{p['name']}**: " + (", ".join(f"{k} ×{v}" for k, v in top) or "none"))
    lines.append("")
    return "\n".join(lines)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    m = sub.add_parser("mark")
    m.add_argument("phase", help="phase name, or '-' to close the current phase")
    m.add_argument("--at", help="ISO UTC timestamp instead of now")
    sn = sub.add_parser("snapshot")
    sn.add_argument("--transcripts", help="glob of transcript JSONL files (defaults to phases.json transcriptGlob)")
    sn.add_argument("--hook", action="store_true", help="read transcript_path from Claude Code hook JSON on stdin")
    ib = sub.add_parser("import-batch")
    ib.add_argument("file", help="an earlier legacy langfuse_batch.json")
    m.set_defaults(func=cmd_mark)
    sn.set_defaults(func=cmd_snapshot)
    ib.set_defaults(func=cmd_import_batch)
    r = sub.add_parser("report")
    r.add_argument("--transcripts", help="glob of transcript JSONL files (defaults to phases.json transcriptGlob)")
    r.add_argument("--out", help="output folder (default: this folder)")
    r.set_defaults(func=cmd_report)
    args = ap.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
