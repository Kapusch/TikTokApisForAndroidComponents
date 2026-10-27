#!/usr/bin/env python3
"""Fail when a tracked file contains a real-looking credential, a filled sample template or a private term.

This repository is a public, generic wrapper. The generic checks below are tracked. Terms that
must never appear (for example the names of private consuming apps) are not: they are read at run
time from an untracked source so the scanner never publishes them.

Private terms, one per line or comma-separated, matched case-insensitively as literal text:
  - the KAPUSCH_PRIVATE_TERMS environment variable (CI reads it from a repository secret);
  - the file <git dir>/info/private-terms ('#' starts a comment).
The private-term check is skipped, with a notice, when neither source provides a term.
Failures name the file and the term's position, never the term itself.

Sample TikTokBusiness.template.json files must also keep placeholder values: real ids and the App
Secret belong in the untracked TikTokBusiness.local.json created from the template.
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[1]

PATTERNS = {
    # A TikTok App Secret or Events API access token pasted into a sample, doc or JSON file.
    "App Secret or access token": re.compile(
        rb"(?i)(?:access_?token|app_?secret)[\"']?\s*[:=]\s*[\"'][0-9a-f]{32,}[\"']"
    ),
}

# Keys of the sample template that must stay placeholders such as "YOUR_TIKTOK_APP_ID".
TEMPLATE_SECRET_KEYS = ("tikTokAppId", "accessToken")
PLACEHOLDER = re.compile(r"^YOUR_[A-Z0-9_]+$")


def split_terms(text: str) -> list[str]:
    terms = []
    for line in text.splitlines():
        line = line.split("#", 1)[0]
        terms.extend(part.strip() for part in line.split(","))
    return [term for term in terms if term]


def load_private_terms() -> list[str]:
    terms = split_terms(os.environ.get("KAPUSCH_PRIVATE_TERMS", ""))
    git_dir = subprocess.check_output(["git", "rev-parse", "--absolute-git-dir"], cwd=ROOT, text=True).strip()
    terms_file = Path(git_dir) / "info" / "private-terms"
    if terms_file.is_file():
        terms.extend(split_terms(terms_file.read_text(encoding="utf-8")))
    return list(dict.fromkeys(terms))


def filled_sample_keys(name: str, data: bytes) -> str | None:
    posix = PurePosixPath(name)
    if posix.parts[0] != "samples" or posix.name != "TikTokBusiness.template.json":
        return None
    try:
        template = json.loads(data)
    except ValueError:
        return "unreadable JSON"
    filled = sorted(
        key
        for key in TEMPLATE_SECRET_KEYS
        if not isinstance(template.get(key), str) or not PLACEHOLDER.match(template[key])
    )
    return f"non-placeholder {', '.join(filled)}" if filled else None


def main() -> int:
    files = [f for f in subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT).decode().split("\0") if f]
    private = [
        (f"private term #{index}", re.compile(re.escape(term.encode()), re.IGNORECASE))
        for index, term in enumerate(load_private_terms(), start=1)
    ]
    errors = []
    for name in files:
        path = ROOT / name
        if not path.is_file():
            continue
        data = path.read_bytes()
        for label, pattern in [*private, *PATTERNS.items()]:
            if pattern.search(data):
                errors.append(f"{name}: {label}")
        template = filled_sample_keys(name, data)
        if template:
            errors.append(f"{name}: {template}")
    if errors:
        print("Public tree scan failed:\n  " + "\n  ".join(errors), file=sys.stderr)
        return 1
    if private:
        scope = f"{len(private)} private term(s)"
    else:
        scope = "no private terms configured (set KAPUSCH_PRIVATE_TERMS or .git/info/private-terms)"
    print(f"Public tree scan passed ({len(files)} tracked files, {scope})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
