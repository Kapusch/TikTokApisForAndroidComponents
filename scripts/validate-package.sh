#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package="${1:-$repo_root/artifacts/nuget/Kapusch.TikTok.Android.1.0.0.nupkg}"
if [ ! -f "$package" ]; then
  echo "Package not found: $package" >&2
  exit 1
fi

entries="$(unzip -Z1 "$package")"
for expected in \
  "android/aar/ktk-tiktokinterop-release.aar" \
  "android/aar/tiktok-open-sdk-core-2.3.1.aar" \
  "android/aar/tiktok-open-sdk-share-2.3.1.aar" \
  "buildTransitive/Kapusch.TikTok.Android.props" \
  "buildTransitive/Kapusch.TikTok.Android.targets"; do
  if ! rg -Fxq "$expected" <<<"$entries"; then
    echo "Missing package entry: $expected" >&2
    exit 1
  fi
done

if rg -q "tiktok-open-sdk-auth|TikTokAuth" <<<"$entries"; then
  echo "Unexpected TikTok Auth module in Share package." >&2
  exit 1
fi

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT
unzip -q "$package" "android/aar/*.aar" -d "$tmp_dir"
unzip -p "$tmp_dir/android/aar/ktk-tiktokinterop-release.aar" AndroidManifest.xml > "$tmp_dir/wrapper-manifest.xml"
rg -q 'TikTokShareActivity' "$tmp_dir/wrapper-manifest.xml"
rg -q '\$\{tiktokClientKey\}' "$tmp_dir/wrapper-manifest.xml"
unzip -p "$tmp_dir/android/aar/tiktok-open-sdk-share-2.3.1.aar" classes.jar > "$tmp_dir/share-classes.jar"
jar tf "$tmp_dir/share-classes.jar" | rg -q '^com/tiktok/open/sdk/share/ShareApi.class$'

rg -q 'v2\.3\.1' "$repo_root/DependencyLocks/Android/lockstate.txt"
verification="$repo_root/src/Kapusch.TikTokApisForAndroidComponents/Native/Android/gradle/verification-metadata.xml"
rg -q '<sha256 value="[0-9a-f]{64}"' "$verification"
echo "TikTok Android package validation passed: $package"

