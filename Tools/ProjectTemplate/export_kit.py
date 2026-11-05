#!/usr/bin/env python3
"""Exports the reusable part of this project as the `unity-agent-kit` repository layout.

Usage: python Tools/ProjectTemplate/export_kit.py <target folder> [--force]

Kit layout (what is copied from where):
  rules/       <- Docs/Code Rules/*.md
  AGENTS.md    <- AGENTS.md without its "This project" section, plus an empty one
  workflow/    <- Tools/Workflow (installed by a project as Tools/Workflow); config.json is written generic
  scaffold/    <- Tools/ProjectTemplate without this script
  docs/        <- the generic Docs/*.md (agent-workflow, save-system, localization)
  kit.json, README.md

Project-only content (game code, `Features/Gameplay`, art and audio docs, mockups) is never exported. The export
fails when an exported non-test file still mentions the source project, so the kit cannot pick up project names.
The target must be empty or missing; `--force` rewrites the kit folders in an existing target (a `.git` folder in
it is kept). Standard library only.
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.abspath(os.path.join(HERE, "..", ".."))
PROJECT_SECTION = "\n## This project"
KIT_VERSION = "0.1.0"
GENERIC_DOCS = ["agent-workflow.md", "save-system.md", "localization.md"]
KIT_FOLDERS = ["rules", "workflow", "scaffold", "docs"]
# project names that must not leak into kit files; test fixtures may use issue ids of the source team
LEAK = re.compile(r"rosie|JakeCake1|\bEVG-\d+|screens\.rar|PrototypeParity", re.I)
SKIP_DIRS = {"__pycache__", ".pytest_cache"}
SKIP_FILES = {"export_kit.py", "test_export_kit.py", "config.json"}

README = """# unity-agent-kit

Rules, agent workflow and project scaffold for Unity projects on one stack: VContainer, UniTask, UniRx, DOTween,
Addressables, UI Toolkit. Extracted from a finished project; `version` is in `kit.json`.

| Folder | What it is | Goes into a project as |
|---|---|---|
| `rules/` | Code rules and the everyday `digest.md` | `Docs/Code Rules/` |
| `AGENTS.md` | Entry point for agents: which rule file to load, priorities. Ends with an empty "This project" section | `AGENTS.md` |
| `workflow/` | Linear issue + Langfuse usage workflow, hooks, `/impl-start`, `/handoff`, merge-to-Done action | `Tools/Workflow/` |
| `scaffold/` | `scaffold.py`: folders and assembly definitions of the baseline layout | run from here |
| `docs/` | Setup of the workflow, save system, localization | `Docs/` |

New project: create the Unity project, then

```
python scaffold/scaffold.py <unity project> --root <Root> --with-rules
```

and copy `workflow/` to `<project>/Tools/Workflow/`, set `linear.team_key` in its `config.json` and follow
`docs/agent-workflow.md` ("Install"). `kit init` and `kit sync` that do this in one step are planned.

Tests: `python workflow/tests/test_scenarios.py`, `test_done_on_merge.py`, `test_guard.py`, `test_adoption.py`,
`python scaffold/tests/test_scaffold.py`.
"""


def copy_tree(src, dst):
    for base, dirs, files in os.walk(src):
        dirs[:] = sorted(d for d in dirs if d not in SKIP_DIRS)
        rel = os.path.relpath(base, src)
        out = dst if rel == "." else os.path.join(dst, rel)
        os.makedirs(out, exist_ok=True)
        for name in sorted(files):
            if name in SKIP_FILES or name.endswith(".pyc"):
                continue
            shutil.copyfile(os.path.join(base, name), os.path.join(out, name))


def generic_config():
    with open(os.path.join(SOURCE, "Tools", "Workflow", "config.json"), encoding="utf-8") as f:
        cfg = json.load(f)
    cfg.get("linear", {}).pop("team_key", None)    # set per project; unset accepts any team's ids
    cfg.get("code_index", {}).pop("name", None)    # defaults to the project folder name
    return cfg


def generic_agents():
    with open(os.path.join(SOURCE, "AGENTS.md"), encoding="utf-8") as f:
        head = f.read().split(PROJECT_SECTION)[0].rstrip()
    return (head + "\n\n---\n\n## This project\n\n"
            "- Add the project's spec, conventions and tools here.\n"
            "- **Self-check** (extra checks of `/handoff` step 3): list the project's own checks here "
            "(parity tests, mockup comparison, tool tests).\n")


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def source_commit():
    try:
        return subprocess.run(["git", "rev-parse", "--short", "HEAD"], cwd=SOURCE, capture_output=True, text=True,
                              check=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def find_leaks(target):
    leaks = []
    for base, dirs, files in os.walk(target):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS and d != ".git" and d != "tests"]
        for name in files:
            path = os.path.join(base, name)
            try:
                with open(path, encoding="utf-8") as f:
                    for number, line in enumerate(f, 1):
                        if LEAK.search(line):
                            leaks.append(f"{os.path.relpath(path, target).replace(os.sep, '/')}:{number}: {line.strip()[:100]}")
            except (UnicodeDecodeError, OSError):
                continue
    return leaks


def export(target, force=False):
    if os.path.isdir(target):
        existing = [n for n in os.listdir(target) if n != ".git"]
        if existing and not force:
            sys.exit(f"{target} is not empty; use --force to rewrite the kit folders")
        for folder in KIT_FOLDERS:
            shutil.rmtree(os.path.join(target, folder), ignore_errors=True)
    os.makedirs(target, exist_ok=True)

    shutil.copytree(os.path.join(SOURCE, "Docs", "Code Rules"), os.path.join(target, "rules"))
    copy_tree(os.path.join(SOURCE, "Tools", "Workflow"), os.path.join(target, "workflow"))
    write(os.path.join(target, "workflow", "config.json"), json.dumps(generic_config(), indent=2) + "\n")
    copy_tree(os.path.join(SOURCE, "Tools", "ProjectTemplate"), os.path.join(target, "scaffold"))
    os.makedirs(os.path.join(target, "docs"), exist_ok=True)
    for name in GENERIC_DOCS:
        shutil.copyfile(os.path.join(SOURCE, "Docs", name), os.path.join(target, "docs", name))
    write(os.path.join(target, "AGENTS.md"), generic_agents())
    write(os.path.join(target, "kit.json"), json.dumps({"name": "unity-agent-kit", "version": KIT_VERSION,
                                                        "exported_from": source_commit()}, indent=2) + "\n")
    write(os.path.join(target, "README.md"), README)
    return find_leaks(target)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("target", help="folder of the kit repository (empty or missing)")
    parser.add_argument("--force", action="store_true", help="rewrite the kit folders in a non-empty target")
    args = parser.parse_args()
    leaks = export(os.path.abspath(args.target), args.force)
    if leaks:
        print("project names found in exported files:", file=sys.stderr)
        for leak in leaks:
            print("  " + leak, file=sys.stderr)
        sys.exit(1)
    print(f"kit {KIT_VERSION} exported to {args.target}")


if __name__ == "__main__":
    main()
