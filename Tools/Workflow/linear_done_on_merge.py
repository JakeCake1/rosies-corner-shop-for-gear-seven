#!/usr/bin/env python3
"""GitHub Action step: move the Linear issue(s) of a merged PR to Done (Python 3.8+, standard library only).

Runs on GitHub, so it works while Claude Code is closed. Rules:
  * only for merged PRs (a PR closed without merge never reaches this script; it re-checks `merged` anyway);
  * issue ids are read from the PR head branch and title (the workflow names them `<user>/evg-12-...` and
    `EVG-12: ...`);
  * if another PR for the same issue is still open, the issue stays as it is (Linear's own rule for several
    linked PRs: the issue completes when the last one is merged);
  * issues already in a completed or canceled status are left alone;
  * nothing is merged here.

Env: LINEAR_API_KEY (repo secret), GITHUB_TOKEN, GITHUB_REPOSITORY, GITHUB_EVENT_PATH; optional LINEAR_DONE_STATUS.
"""
import json
import os
import re
import sys
import urllib.parse
import urllib.request



def _team_key():
    try:
        with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.json"), encoding="utf-8") as f:
            return (json.load(f).get("linear") or {}).get("team_key")
    except (OSError, ValueError):
        return None


# only the configured team's ids count: `PART-2` and `UTF-8` in a branch or title are not issues
_KEY = os.environ.get("LINEAR_TEAM_KEY") or _team_key()
if _KEY:
    ISSUE_RE = re.compile(rf"(?<![A-Za-z0-9])({re.escape(_KEY)}-\d+)(?![A-Za-z0-9])", re.I)
else:
    ISSUE_RE = re.compile(r"(?<![A-Za-z0-9])([A-Za-z][A-Za-z0-9]{0,9}-\d+)(?![A-Za-z0-9])")


def post(url, payload, headers):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(), method="POST",
                                 headers={"Content-Type": "application/json", **headers})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode())


def get(url, token):
    req = urllib.request.Request(url, headers={"Authorization": f"Bearer {token}",
                                               "Accept": "application/vnd.github+json"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode())


def linear(query, variables, key):
    body = post("https://api.linear.app/graphql", {"query": query, "variables": variables}, {"Authorization": key})
    if body.get("errors"):
        raise RuntimeError(body["errors"][0].get("message"))
    return body["data"]


def issue_ids(*texts):
    return sorted({m.upper() for t in texts if t for m in ISSUE_RE.findall(t)})


def main():
    event = json.load(open(os.environ["GITHUB_EVENT_PATH"], encoding="utf-8"))
    pr = event["pull_request"]
    if not pr.get("merged"):
        print("PR closed without merge: nothing to do")
        return
    key, token, repo = os.environ.get("LINEAR_API_KEY"), os.environ["GITHUB_TOKEN"], os.environ["GITHUB_REPOSITORY"]
    head_repo = ((pr.get("head") or {}).get("repo") or {}).get("full_name")
    if not key and head_repo != repo:
        # A fork PR (or one whose fork was deleted, head.repo is null) never gets repository secrets. A PR from
        # this repository does, so a missing key there is a setup error and must fail loudly (see below).
        print("PR from a fork or a deleted fork: repository secrets are not available, nothing to do")
        return
    if not key:
        print(f"::error title=LINEAR_API_KEY is missing::This PR is from {repo} itself, so the secret should be "
              f"available. Add the LINEAR_API_KEY repository secret (Settings > Secrets and variables > Actions; "
              f"a renamed or recreated repository does not keep secrets) and re-run this job. Docs/agent-workflow.md.")
        sys.exit("LINEAR_API_KEY secret is not set; the issue was NOT moved to Done")
    done_name = os.environ.get("LINEAR_DONE_STATUS", "Done")
    ids = issue_ids(pr["head"]["ref"], pr["title"])
    if not ids:
        print("no Linear issue id in branch or title")
        return
    open_prs, page = [], 1
    while True:
        batch = get(f"https://api.github.com/repos/{repo}/pulls?state=open&per_page=100&page={page}", token)
        open_prs += batch
        if len(batch) < 100:
            break
        page += 1
    failed = False
    for ident in ids:
        others = [p for p in open_prs if p["number"] != pr["number"] and ident in issue_ids(p["head"]["ref"], p["title"])]
        if others:
            print(f"{ident}: still has open PR(s) {', '.join('#' + str(p['number']) for p in others)}; not moving to Done")
            continue
        try:
            data = linear("""query($id: String!) { issue(id: $id) { id state { name type }
                team { states { nodes { id name type position } } } } }""", {"id": ident}, key)
            issue = data.get("issue")
            if not issue:
                print(f"{ident}: not a Linear issue, skipped")
                continue
            if issue["state"]["type"] in ("completed", "canceled"):
                print(f"{ident}: already {issue['state']['name']}")
                continue
            states = sorted(issue["team"]["states"]["nodes"], key=lambda s: s.get("position", 0))
            target = next((s for s in states if s["name"].lower() == done_name.lower()), None) or \
                next(s for s in states if s["type"] == "completed")
            res = linear("""mutation($id: String!, $s: String!) { issueUpdate(id: $id, input: { stateId: $s }) {
                success } }""", {"id": issue["id"], "s": target["id"]}, key)
            print(f"{ident}: {issue['state']['name']} -> {target['name']} ({res['issueUpdate']['success']})")
        except Exception as e:  # keep going for the other issues, fail the job at the end
            print(f"{ident}: failed: {e}")
            failed = True
    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
