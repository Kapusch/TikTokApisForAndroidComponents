#!/usr/bin/env bash
# Builds the Kapusch TikTok Business interop AAR against the checksum-locked
# upstream TikTok Business SDK AAR and stages the package payload in build/.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$ROOT_DIR/build"
UPSTREAM_DIR="$ROOT_DIR/upstream"
VERIFICATION_METADATA="$ROOT_DIR/gradle/verification-metadata.xml"
SDK_VERSION="1.7.1"
SDK_AAR="tiktok-business-android-sdk-$SDK_VERSION.aar"
GRADLE_VERSION="8.7"
GRADLE_HOME="$ROOT_DIR/.gradle/gradle-$GRADLE_VERSION"
GRADLE_BIN="$GRADLE_HOME/bin/gradle"
GRADLE_USER_HOME="${GRADLE_USER_HOME:-$ROOT_DIR/.gradle/user-home}"
export GRADLE_USER_HOME

if [ ! -f "$VERIFICATION_METADATA" ]; then
  echo "Missing Gradle verification metadata: $VERIFICATION_METADATA" >&2
  exit 1
fi

ANDROID_SDK="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
if [ -z "$ANDROID_SDK" ]; then
  [ -d "$HOME/Library/Android/sdk" ] && ANDROID_SDK="$HOME/Library/Android/sdk"
  [ -z "$ANDROID_SDK" ] && [ -d "$HOME/Android/Sdk" ] && ANDROID_SDK="$HOME/Android/Sdk"
fi
if [ -z "$ANDROID_SDK" ]; then
  echo "ANDROID_SDK_ROOT (or ANDROID_HOME) is required." >&2
  exit 1
fi

"$ROOT_DIR/restore-upstream.sh"

# The interop module compiles against the upstream classes only; the upstream
# AAR itself is shipped unchanged next to the interop AAR.
rm -rf "$UPSTREAM_DIR/compile"
mkdir -p "$UPSTREAM_DIR/compile"
unzip -q -o "$UPSTREAM_DIR/$SDK_AAR" classes.jar -d "$UPSTREAM_DIR/compile"

mkdir -p "$ROOT_DIR/.gradle" "$GRADLE_USER_HOME"
printf 'sdk.dir=%s\n' "$ANDROID_SDK" > "$ROOT_DIR/local.properties"
if [ ! -x "$GRADLE_BIN" ]; then
  unzip -q -o "$UPSTREAM_DIR/gradle-$GRADLE_VERSION-bin.zip" -d "$ROOT_DIR/.gradle"
fi

rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/aar" "$BUILD_DIR/licenses"
"$GRADLE_BIN" --no-daemon -p "$ROOT_DIR" :businessinterop:assembleRelease
cp "$ROOT_DIR/businessinterop/build/outputs/aar/businessinterop-release.aar" "$BUILD_DIR/aar/ktk-business-interop-release.aar"
cp "$UPSTREAM_DIR/$SDK_AAR" "$BUILD_DIR/aar/$SDK_AAR"
cp "$UPSTREAM_DIR/TIKTOK-BUSINESS-SDK-LICENSE" "$BUILD_DIR/licenses/TIKTOK-BUSINESS-SDK-LICENSE"
echo "TikTok Business Android native assets staged in $BUILD_DIR"
