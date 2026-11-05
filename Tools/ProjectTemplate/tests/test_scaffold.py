#!/usr/bin/env python3
"""Checks scaffold.py in a temp folder: layout, asmdef references and direction, re-run, --with-rules.

Run: python Tools/ProjectTemplate/tests/test_scaffold.py
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile

SCRIPT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "scaffold.py"))


def run(*args, ok=True):
    p = subprocess.run([sys.executable, SCRIPT, *args], capture_output=True, text=True)
    assert (p.returncode == 0) == ok, p.stdout + p.stderr
    return p.stdout + p.stderr


def main():
    tmp = tempfile.mkdtemp(prefix="scaffold-test-")
    try:
        run(tmp, ok=False)  # no Assets/: refuses
        os.makedirs(os.path.join(tmp, "Assets"))
        run(tmp, "--root", "My-Game", ok=False)  # not a C# identifier
        out = run(tmp, "--root", "Tetra")
        base = os.path.join(tmp, "Assets", "Tetra")
        for folder in ("Bootstrap/Composition", "Flow/States", "Services/UI/Elements", "Content/Screens/Shared",
                       "Tests/EditMode/Services"):
            assert os.path.isdir(os.path.join(base, folder)), folder
        refs = {}
        for folder, name in (("Services", "Services"), ("Features", "Features"), ("Flow", "Flow"),
                             ("Bootstrap", "Bootstrap"), ("Tests/EditMode", "Tests.EditMode")):
            data = json.load(open(os.path.join(base, folder, f"Tetra.{name}.asmdef")))
            assert data["name"] == f"Tetra.{name}" and "VContainer" in data["references"], data
            refs[name] = {r for r in data["references"] if r.startswith("Tetra.")}
        assert refs["Services"] == set()
        assert refs["Features"] == {"Tetra.Services"}
        assert "Tetra.Bootstrap" not in refs["Flow"] | refs["Features"]
        assert "created Assets/Tetra/Services/Tetra.Services.asmdef" in out, out
        assert not os.path.exists(os.path.join(tmp, "AGENTS.md"))
        assert not os.path.exists(os.path.join(base, "Bootstrap", "Editor"))  # needs its own Editor asmdef first

        # re-run: nothing overwritten
        path = os.path.join(base, "Services", "Tetra.Services.asmdef")
        open(path, "w").write("{}")
        assert "nothing to do" in run(tmp, "--root", "Tetra")
        assert open(path).read() == "{}"

        run(tmp, "--root", "Tetra", "--with-rules")
        agents = open(os.path.join(tmp, "AGENTS.md"), encoding="utf-8").read()
        assert "Rosie" not in agents and "## Files" in agents and "## This project" in agents, agents[-300:]
        assert os.path.exists(os.path.join(tmp, "Docs", "Code Rules", "unity-project-structure-rules.md"))
        assert os.path.exists(os.path.join(tmp, "Docs", "Code Rules", "digest.md"))
        assert "@AGENTS.md" in open(os.path.join(tmp, "CLAUDE.md"), encoding="utf-8").read()
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    print("SCAFFOLD TESTS PASSED")


if __name__ == "__main__":
    main()
