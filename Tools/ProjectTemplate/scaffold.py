#!/usr/bin/env python3
"""Creates the baseline folder layout and assembly definitions of Rosie's Corner Shop in a Unity project.

Usage: python Tools/ProjectTemplate/scaffold.py <unity project> [--root Game] [--with-rules]

The layout is described in `Docs/Code Rules/unity-project-structure-rules.md` ("Baseline for new projects").
Existing files are never overwritten, so re-running only adds what is missing. Unity creates the .meta files on
the next import. `--with-rules` also copies `Docs/Code Rules/` and `AGENTS.md` without its project-specific part.
Standard library only.
"""
import argparse
import json
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.abspath(os.path.join(HERE, "..", ".."))
PROJECT_SECTION = "\n## This project"

FOLDERS = [
    "Bootstrap/Composition",
    "Bootstrap/Editor",
    "Flow/States",
    "Features",
    "Services/Assets",
    "Services/Pooling",
    "Services/Save",
    "Services/UI/Elements",
    "Content/Data",
    "Content/Prefabs",
    "Content/Scenes",
    "Content/Screens/Shared",
    "Content/Sounds",
    "Content/Visual/Atlases",
    "Content/Visual/Fonts",
    "Content/Visual/UI",
    "Tests/EditMode/Features",
    "Tests/EditMode/Flow",
    "Tests/EditMode/Services",
]

# The stack every runtime assembly references (tech stack rules); dependency direction is
# Bootstrap -> Flow -> Features -> Services, never back.
STACK = ["UniTask", "UniTask.Addressables", "UniTask.DOTween", "UniRx", "VContainer", "DOTween.Modules",
         "Unity.Addressables", "Unity.ResourceManager"]
ASSEMBLIES = [
    ("Services", "Services", [], {}),
    ("Features", "Features", ["Services"], {}),
    ("Flow", "Flow", ["Features", "Services"], {}),
    ("Bootstrap", "Bootstrap", ["Flow", "Features", "Services"], {}),
    ("Tests/EditMode", "Tests.EditMode", ["Services", "Features", "Flow"],
     {"includePlatforms": ["Editor"], "overrideReferences": True, "precompiledReferences": ["nunit.framework.dll"],
      "defineConstraints": ["UNITY_INCLUDE_TESTS"],
      "extra": ["UnityEngine.TestRunner", "UnityEditor.TestRunner"]}),
]


def asmdef(root, name, own, options):
    full = f"{root}.{name}"
    extra = options.get("extra", [])
    return {
        "name": full,
        "rootNamespace": full,
        "references": [f"{root}.{o}" for o in own] + STACK + extra,
        "includePlatforms": options.get("includePlatforms", []),
        "excludePlatforms": [],
        "allowUnsafeCode": False,
        "overrideReferences": options.get("overrideReferences", False),
        "precompiledReferences": options.get("precompiledReferences", []),
        "autoReferenced": False,
        "defineConstraints": options.get("defineConstraints", []),
        "versionDefines": [],
        "noEngineReferences": False,
    }


def write_new(path, text, created):
    if os.path.exists(path):
        return
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    created.append(path)


def scaffold(project, root, with_rules):
    created = []
    base = os.path.join(project, "Assets", root)
    for folder in FOLDERS:
        path = os.path.join(base, *folder.split("/"))
        if not os.path.isdir(path):
            os.makedirs(path)
            created.append(path + os.sep)
    for folder, name, own, options in ASSEMBLIES:
        path = os.path.join(base, *folder.split("/"), f"{root}.{name}.asmdef")
        write_new(path, json.dumps(asmdef(root, name, own, options), indent=4) + "\n", created)
    if with_rules:
        rules = os.path.join(SOURCE, "Docs", "Code Rules")
        for name in sorted(os.listdir(rules)):
            if name.endswith(".md"):
                with open(os.path.join(rules, name), encoding="utf-8") as f:
                    write_new(os.path.join(project, "Docs", "Code Rules", name), f.read(), created)
        with open(os.path.join(SOURCE, "AGENTS.md"), encoding="utf-8") as f:
            agents = f.read().split(PROJECT_SECTION)[0].rstrip()
        write_new(os.path.join(project, "AGENTS.md"),
                  agents + "\n\n---\n\n## This project\n\n- Add the project's spec, conventions and tools here.\n",
                  created)
    return created


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("project", help="Unity project folder (the one that holds Assets/)")
    parser.add_argument("--root", default="Game", help="root code folder and namespace (default: Game)")
    parser.add_argument("--with-rules", action="store_true", help="copy Docs/Code Rules and the generic AGENTS.md")
    args = parser.parse_args()
    if not os.path.isdir(os.path.join(args.project, "Assets")):
        sys.exit(f"{args.project} has no Assets/ folder; create the Unity project first")
    created = scaffold(args.project, args.root, args.with_rules)
    for path in created:
        print("created", os.path.relpath(path, args.project).replace(os.sep, "/"))
    print(f"{len(created)} item(s) created" if created else "nothing to do: the layout is already in place")


if __name__ == "__main__":
    main()
