#!/usr/bin/env python3
"""Reports how much agents use the codebase-memory code graph instead of text search, from local transcripts.

Run: sh Tools/Workflow/wf.sh adoption [--dir DIR] [--since YYYY-MM-DD] [--json]
     (or python3 Tools/Workflow/adoption.py ...)

Per session (main transcript + its subagents) and in total:
  * graph searches (codebase-memory-mcp search/trace/snippet/query/architecture/detect_changes calls)
  * text searches for a symbol-like pattern (Bash/PowerShell grep / rg / find -name, Grep, Glob)
  * graph share = graph / (graph + symbol-like text searches)
  * fallback rate: a graph search followed within FALLBACK_WINDOW tool calls by a text search or Read that
    mentions the same identifier
  * subagent types launched

Everything is read and printed locally. Only tool names and search patterns are looked at, nothing is stored or
sent anywhere (not to Langfuse either).
"""
import argparse
import datetime as dt
import glob
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import workflow as w  # noqa: E402

GRAPH_PREFIX = "mcp__codebase-memory-mcp__"
GRAPH_SEARCH_TOOLS = {"search_graph", "trace_path", "get_code_snippet", "query_graph", "search_code",
                      "get_architecture", "detect_changes"}
SHELL_TOOLS = {"Bash", "PowerShell"}
FALLBACK_WINDOW = 5
IDENT = re.compile(r"[A-Za-z_][A-Za-z0-9_]{3,}")


def project_dir():
    slug = re.sub(r"[^A-Za-z0-9]", "-", w.ROOT)
    return os.path.join(os.path.expanduser("~"), ".claude", "projects", slug)


def tool_calls(path):
    """Yields (tool name, input) of every tool call of one transcript, in order."""
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue
            content = (entry.get("message") or {}).get("content")
            if entry.get("type") != "assistant" or not isinstance(content, list):
                continue
            for block in content:
                if isinstance(block, dict) and block.get("type") == "tool_use":
                    yield block.get("name", ""), block.get("input") or {}


def identifiers(value):
    if isinstance(value, str):
        return {i.lower() for i in IDENT.findall(value)}
    if isinstance(value, dict):
        return set().union(*(identifiers(v) for v in value.values())) if value else set()
    if isinstance(value, list):
        return set().union(*(identifiers(v) for v in value)) if value else set()
    return set()


def text_searches(tool, tin):
    """Yields the patterns of the text searches a tool call makes (empty for other calls)."""
    if tool in SHELL_TOOLS:
        for kind, pattern, _ in w.parse_text_search(tin.get("command", "")):
            yield pattern
    elif tool == "Grep":
        yield str(tin.get("pattern", ""))
    elif tool == "Glob":
        yield re.sub(r"[*?{}\[\]/\\]|\.\w+$", " ", str(tin.get("pattern", ""))).strip()


def analyse_transcript(path):
    stats = {"graph": 0, "text_symbol": 0, "text_other": 0, "reads": 0, "fallback": 0, "agents": {}}
    pending = []  # [identifiers, calls left] of recent graph searches
    for tool, tin in tool_calls(path):
        touched = set()
        if tool.startswith(GRAPH_PREFIX):
            if tool[len(GRAPH_PREFIX):] in GRAPH_SEARCH_TOOLS:
                stats["graph"] += 1
                pending.append([identifiers(tin), FALLBACK_WINDOW])
            continue
        if tool in ("Agent", "Task"):
            kind = tin.get("subagent_type") or "general-purpose"
            stats["agents"][kind] = stats["agents"].get(kind, 0) + 1
        for pattern in text_searches(tool, tin):
            touched |= identifiers(pattern)
            stats["text_symbol" if w.looks_like_symbol(pattern) else "text_other"] += 1
        if tool == "Read":
            stats["reads"] += 1
            touched |= identifiers(os.path.splitext(os.path.basename(str(tin.get("file_path", ""))))[0])
        for item in pending[:]:
            if touched & item[0]:
                stats["fallback"] += 1
                pending.remove(item)
            else:
                item[1] -= 1
                if item[1] <= 0:
                    pending.remove(item)
    return stats


def merge(into, other):
    for key in ("graph", "text_symbol", "text_other", "reads", "fallback"):
        into[key] += other[key]
    for kind, n in other["agents"].items():
        into["agents"][kind] = into["agents"].get(kind, 0) + n


def analyse(directory, since=None):
    sessions = {}
    for main in sorted(glob.glob(os.path.join(directory, "*.jsonl"))):
        if since and dt.datetime.fromtimestamp(os.path.getmtime(main)) < since:
            continue
        sid = os.path.splitext(os.path.basename(main))[0]
        total = {"graph": 0, "text_symbol": 0, "text_other": 0, "reads": 0, "fallback": 0, "agents": {}}
        for path in w.transcript_files(main):
            merge(total, analyse_transcript(path))
        sessions[sid] = total
    return sessions


def share(s):
    n = s["graph"] + s["text_symbol"]
    return None if n == 0 else s["graph"] / n


def pct(value):
    return "-" if value is None else f"{value * 100:.0f}%"


def render(sessions):
    total = {"graph": 0, "text_symbol": 0, "text_other": 0, "reads": 0, "fallback": 0, "agents": {}}
    lines = [f"{'session':10} {'graph':>6} {'sym.text':>9} {'other.text':>10} {'share':>6} {'fallback':>9}  subagents"]
    for sid, s in sessions.items():
        merge(total, s)
        lines.append(f"{sid[:8]:10} {s['graph']:>6} {s['text_symbol']:>9} {s['text_other']:>10} "
                     f"{pct(share(s)):>6} {pct(s['fallback'] / s['graph'] if s['graph'] else None):>9}  "
                     f"{', '.join(f'{k} x{v}' for k, v in sorted(s['agents'].items())) or '-'}")
    lines.append(f"{'TOTAL':10} {total['graph']:>6} {total['text_symbol']:>9} {total['text_other']:>10} "
                 f"{pct(share(total)):>6} {pct(total['fallback'] / total['graph'] if total['graph'] else None):>9}  "
                 f"{', '.join(f'{k} x{v}' for k, v in sorted(total['agents'].items())) or '-'}")
    lines.append(f"sessions: {len(sessions)}; Read calls: {total['reads']}. Target: graph share above 50%.")
    return "\n".join(lines), total


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dir", default=project_dir(), help="folder with the project's transcripts")
    ap.add_argument("--since", help="only sessions whose transcript changed on or after this date (YYYY-MM-DD)")
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    if not os.path.isdir(args.dir):
        sys.exit(f"adoption: no transcripts in {args.dir}")
    sessions = analyse(args.dir, dt.datetime.strptime(args.since, "%Y-%m-%d") if args.since else None)
    text, total = render(sessions)
    print(json.dumps({"sessions": sessions, "total": total}, indent=2) if args.json else text)


if __name__ == "__main__":
    main()
