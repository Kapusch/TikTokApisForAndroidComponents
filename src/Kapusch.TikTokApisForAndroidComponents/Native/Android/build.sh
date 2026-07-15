#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$ROOT_DIR/build"
VERIFICATION_METADATA="$ROOT_DIR/gradle/verification-metadata.xml"
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
cp "$ROOT_DIR/tiktok-open-sdk-core.gradle.kts" "$ROOT_DIR/upstream/tiktok-open-sdk-core/build.gradle.kts"
cp "$ROOT_DIR/tiktok-open-sdk-share.gradle.kts" "$ROOT_DIR/upstream/tiktok-open-sdk-share/build.gradle.kts"
ln -sfn "$ROOT_DIR/upstream/tiktok-open-sdk-core" "$ROOT_DIR/tiktok-open-sdk-core"
ln -sfn "$ROOT_DIR/upstream/tiktok-open-sdk-share" "$ROOT_DIR/tiktok-open-sdk-share"

mkdir -p "$ROOT_DIR/.gradle" "$GRADLE_USER_HOME"
cat > "$ROOT_DIR/local.properties" <<EOF
sdk.dir=$ANDROID_SDK
EOF
if [ ! -x "$GRADLE_BIN" ]; then
  archive="$ROOT_DIR/.gradle/gradle-$GRADLE_VERSION-bin.zip"
  curl -fsSL -o "$archive" "https://services.gradle.org/distributions/gradle-$GRADLE_VERSION-bin.zip"
  unzip -q -o "$archive" -d "$ROOT_DIR/.gradle"
fi

rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/aar" "$BUILD_DIR/deps"
"$GRADLE_BIN" -p "$ROOT_DIR" :tiktokinterop:assembleRelease :tiktok-open-sdk-core:assembleRelease :tiktok-open-sdk-share:assembleRelease
cp "$ROOT_DIR/tiktokinterop/build/outputs/aar/tiktokinterop-release.aar" "$BUILD_DIR/aar/ktk-tiktokinterop-release.aar"
cp "$ROOT_DIR/tiktok-open-sdk-core/build/outputs/aar/tiktok-open-sdk-core-release.aar" "$BUILD_DIR/aar/tiktok-open-sdk-core-2.3.1.aar"
cp "$ROOT_DIR/tiktok-open-sdk-share/build/outputs/aar/tiktok-open-sdk-share-release.aar" "$BUILD_DIR/aar/tiktok-open-sdk-share-2.3.1.aar"

"$GRADLE_BIN" -p "$ROOT_DIR" :tiktokinterop:copyReleaseRuntimeDependencies
find "$ROOT_DIR/tiktokinterop/build/runtime-dependencies" -maxdepth 1 -type f \( -name '*.aar' -o -name '*.jar' \) -exec cp {} "$BUILD_DIR/deps/" \;
rm -f "$BUILD_DIR/deps"/tiktok-open-sdk-*.aar
cp "$ROOT_DIR/upstream-LICENSE" "$BUILD_DIR/TIKTOK-OPENSDK-LICENSE"
