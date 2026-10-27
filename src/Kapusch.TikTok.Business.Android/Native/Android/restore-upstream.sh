#!/usr/bin/env bash
# Downloads every input listed in DependencyLocks/Android/business-lockstate.txt
# into Native/Android/upstream/ and fails on any SHA-256 mismatch. Files that are
# already present with the locked checksum are reused.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$ROOT_DIR/../../../.." && pwd)"
LOCK_FILE="$REPO_ROOT/DependencyLocks/Android/business-lockstate.txt"
UPSTREAM_DIR="$ROOT_DIR/upstream"

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

mkdir -p "$UPSTREAM_DIR"
count=0
while read -r url expected_sha filename; do
  [ -z "${url:-}" ] && continue
  case "$url" in \#*) continue ;; esac
  if [ -z "${expected_sha:-}" ] || [ -z "${filename:-}" ]; then
    echo "Malformed lock line: $url" >&2
    exit 1
  fi
  target="$UPSTREAM_DIR/$filename"
  if [ -f "$target" ] && [ "$(sha256_of "$target")" = "$expected_sha" ]; then
    count=$((count + 1))
    continue
  fi
  tmp="$target.download"
  curl -fsSL --retry 3 --retry-delay 1 -o "$tmp" "$url"
  actual_sha="$(sha256_of "$tmp")"
  if [ "$actual_sha" != "$expected_sha" ]; then
    rm -f "$tmp"
    echo "Checksum mismatch for $filename. Expected $expected_sha, got $actual_sha." >&2
    exit 1
  fi
  mv "$tmp" "$target"
  count=$((count + 1))
done < "$LOCK_FILE"

if [ "$count" -eq 0 ]; then
  echo "No locked inputs found in $LOCK_FILE" >&2
  exit 1
fi
echo "Restored $count locked TikTok Business inputs into $UPSTREAM_DIR"
