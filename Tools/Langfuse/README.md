# Agent resource tracking (Langfuse)

Measures how many tokens the AI agent spent on a task, split by task phase, and sends the result to Langfuse as
one trace: a span per phase and a generation per model reply (model, token usage, tools called).

| File | What |
|---|---|
| `usage_report.py` | `mark`, `snapshot`, `report`, `import-batch` (Python 3, standard library only) |
| `phases.json` | Task name, issue, transcript glob and phase boundaries (UTC) |
| `ledger.jsonl` | Every reply seen so far, deduplicated by id; survives context compaction |
| `REPORT.md`, `usage_summary.json` | Human and machine readable report |
| `langfuse_batch.json` | Events for the Langfuse public ingestion API |
| `Send-ToLangfuse.ps1` | Posts the batch to Langfuse |

## Measure a task

```bash
python3 Tools/Langfuse/usage_report.py mark "4. Implementation"   # starts a phase, closes the previous one
python3 Tools/Langfuse/usage_report.py snapshot                   # copies replies into ledger.jsonl
python3 Tools/Langfuse/usage_report.py report                     # snapshot + REPORT.md + langfuse_batch.json
```

Effective tokens weight every token by its price relative to an uncached input token: cache read 0.1×,
cache write 2×, output 5×.

Claude Code rewrites the session transcript when it compacts the context, so earlier replies vanish from it.
`report` and `snapshot` keep them in `ledger.jsonl`, but only if they run before the compaction. To make that
automatic, add a hook to your Claude Code settings (`.claude/settings.json` or `~/.claude/settings.json`):

```json
{
  "hooks": {
    "PreCompact": [
      { "hooks": [ { "type": "command",
        "command": "python3 \"$CLAUDE_PROJECT_DIR/Tools/Langfuse/usage_report.py\" snapshot --hook" } ] }
    ]
  }
}
```

Ranges longer than 30 minutes without data are listed in `REPORT.md` as coverage gaps.

## Send to Langfuse

1. Copy `.env.example` to `.env` at the project root and fill in `LANGFUSE_PUBLIC_KEY`, `LANGFUSE_SECRET_KEY`
   (Langfuse → Project settings → API keys) and `LANGFUSE_HOST`. `.env` is git-ignored.
2. Run `powershell -ExecutionPolicy Bypass -File Tools\Langfuse\Send-ToLangfuse.ps1`.

Event ids are deterministic per trace, phase and reply, so sending again updates the same trace.
