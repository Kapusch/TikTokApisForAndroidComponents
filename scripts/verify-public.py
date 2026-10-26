"""Fails when a tracked file contains a private application identifier.

The pattern is written with hex escapes so this scanner does not itself contain
the names it rejects. Keep every doc, sample, test and script generic.
"""
from pathlib import Path
import re
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
files = subprocess.check_output(["git", "ls-files"], cwd=root, text=True).splitlines()
private = re.compile(
    rb"(?i)\x62\x6c\x6f\x6f\x6d[ &_-]*(?:\x61\x6e\x64[ _-]*)?(?:\x66\x6c\x79|\x79)"
    rb"|\x62\x6c\x6f\x6f\x6d\x79\x61\x70\x70\x73"
)
failures = []
for name in files:
    path = root / name
    if not path.is_file():
        continue
    if private.search(path.read_bytes()):
        failures.append(name)
if failures:
    sys.exit("Private identifier found in: " + ", ".join(failures))
print(f"Public tree scan passed ({len(files)} tracked files)")
