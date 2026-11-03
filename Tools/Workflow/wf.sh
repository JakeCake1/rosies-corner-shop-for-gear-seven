#!/bin/sh
# Runs Tools/Workflow/workflow.py with the first working Python 3 (Git Bash on Windows, macOS, Linux).
DIR=$(cd "$(dirname "$0")" && pwd)
SCRIPT="$DIR/workflow.py"
if [ "$1" = "adoption" ]; then SCRIPT="$DIR/adoption.py"; shift; fi
for py in python3 python py; do
  if command -v "$py" >/dev/null 2>&1 && "$py" -c "import sys; sys.exit(sys.version_info < (3, 8))" >/dev/null 2>&1; then
    exec "$py" "$SCRIPT" "$@"
  fi
done
echo "workflow: Python 3.8+ not found on PATH" >&2
exit 1
