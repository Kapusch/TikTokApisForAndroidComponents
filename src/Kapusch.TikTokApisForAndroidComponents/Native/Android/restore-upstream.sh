#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$ROOT_DIR/../../../.." && pwd)"
LOCK_FILE="$REPO_ROOT/DependencyLocks/Android/lockstate.txt"
UPSTREAM_DIR="$ROOT_DIR/upstream"

read -r url expected_sha filename < <(grep -vE '^[[:space:]]*(#|$)' "$LOCK_FILE" | head -n 1)
tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT
curl -fsSL --retry 3 --retry-delay 1 -o "$tmp_dir/$filename" "$url"
actual_sha="$(shasum -a 256 "$tmp_dir/$filename" | awk '{print $1}')"
if [ "$actual_sha" != "$expected_sha" ]; then
  echo "TikTok OpenSDK source checksum mismatch. Expected $expected_sha, got $actual_sha." >&2
  exit 1
fi
rm -rf "$UPSTREAM_DIR"
mkdir -p "$UPSTREAM_DIR"
tar -xzf "$tmp_dir/$filename" --strip-components=1 -C "$UPSTREAM_DIR"
cp "$UPSTREAM_DIR/LICENSE" "$ROOT_DIR/upstream-LICENSE"

