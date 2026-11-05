#!/usr/bin/env python3
"""Checks export_kit.py in a temp folder: kit layout, no project names, generic config, scaffold from the kit.

Run: python Tools/ProjectTemplate/tests/test_export_kit.py
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
EXPORT = os.path.abspath(os.path.join(HERE, "..", "export_kit.py"))


def run(*args, ok=True):
    p = subprocess.run([sys.executable, *args], capture_output=True, text=True)
    assert (p.returncode == 0) == ok, p.stdout + p.stderr
    return p.stdout + p.stderr


def main():
    tmp = tempfile.mkdtemp(prefix="export-kit-test-")
    try:
        kit = os.path.join(tmp, "kit")
        run(EXPORT, kit)
        for path in ("rules/digest.md", "rules/unity-tech-stack-rules.md", "AGENTS.md", "README.md", "kit.json",
                     "workflow/workflow.py", "workflow/wf.sh", "workflow/templates/commands/handoff.md",
                     "workflow/templates/linear-done-on-merge.yml", "workflow/tests/test_scenarios.py",
                     "scaffold/scaffold.py", "scaffold/tests/test_scaffold.py", "docs/agent-workflow.md"):
            assert os.path.exists(os.path.join(kit, *path.split("/"))), path
        for path in ("scaffold/export_kit.py", "scaffold/tests/test_export_kit.py", "docs/audio.md",
                     "docs/art-resources.md"):
            assert not os.path.exists(os.path.join(kit, *path.split("/"))), path + " must not be exported"

        cfg = json.load(open(os.path.join(kit, "workflow", "config.json"), encoding="utf-8"))
        assert "team_key" not in cfg["linear"] and "name" not in cfg["code_index"], cfg
        assert cfg["guarded_paths"] and cfg["checks"], cfg
        assert json.load(open(os.path.join(kit, "kit.json")))["version"], "kit.json"
        agents = open(os.path.join(kit, "AGENTS.md"), encoding="utf-8").read()
        assert "## Files" in agents and agents.count("## This project") == 1 and "Rosie" not in agents

        out = run(EXPORT, kit, ok=False)  # non-empty target
        assert "--force" in out, out
        run(EXPORT, kit, "--force")

        # the kit's own scaffold finds its rules next to itself
        project = os.path.join(tmp, "project")
        os.makedirs(os.path.join(project, "Assets"))
        run(os.path.join(kit, "scaffold", "scaffold.py"), project, "--root", "Tetra", "--with-rules")
        assert os.path.exists(os.path.join(project, "Docs", "Code Rules", "digest.md"))
        assert os.path.exists(os.path.join(project, "Assets", "Tetra", "Services", "Tetra.Services.asmdef"))
        assert "@AGENTS.md" in open(os.path.join(project, "CLAUDE.md"), encoding="utf-8").read()
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    print("EXPORT KIT TESTS PASSED")


if __name__ == "__main__":
    main()
