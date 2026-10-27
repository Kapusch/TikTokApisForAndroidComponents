#!/usr/bin/env bash
# Validates a packed Kapusch.TikTok.Business.Android nupkg. Update this script in
# the same commit as any change to the Business package contents.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
sdk_version="1.7.1"
sdk_aar="tiktok-business-android-sdk-$sdk_version.aar"
lock_file="$repo_root/DependencyLocks/Android/business-lockstate.txt"
verification="$repo_root/src/Kapusch.TikTok.Business.Android/Native/Android/gradle/verification-metadata.xml"

package="${1:-}"
if [ -z "$package" ]; then
  shopt -s nullglob
  candidates=("$repo_root"/artifacts/nuget/Kapusch.TikTok.Business.Android.*.nupkg)
  shopt -u nullglob
  if [ "${#candidates[@]}" -ne 1 ]; then
    echo "Expected exactly one Kapusch.TikTok.Business.Android nupkg in artifacts/nuget, found ${#candidates[@]}. Pass the path explicitly." >&2
    exit 1
  fi
  package="${candidates[0]}"
fi
if [ ! -f "$package" ]; then
  echo "Package not found: $package" >&2
  exit 1
fi

fail() {
  echo "Business package validation failed: $*" >&2
  exit 1
}

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

entries="$(unzip -Z1 "$package")"
for expected in \
  "android/aar/ktk-business-interop-release.aar" \
  "android/aar/$sdk_aar" \
  "buildTransitive/Kapusch.TikTok.Business.Android.props" \
  "buildTransitive/Kapusch.TikTok.Business.Android.targets" \
  "lib/net10.0-android36.0/Kapusch.TikTok.Business.Android.dll" \
  "licenses/TIKTOK-BUSINESS-SDK-LICENSE" \
  "THIRD_PARTY_NOTICES.md" \
  "nuget-readme.md"; do
  grep -Fxq "$expected" <<<"$entries" || fail "missing package entry $expected"
done

# Exactly two vendor AARs and no bundled runtime dependencies: shared AndroidX,
# Kotlin, Billing and Install Referrer classes must never be repackaged here.
aar_count="$(grep -Ec '^android/aar/[^/]+\.aar$' <<<"$entries")"
[ "$aar_count" -eq 2 ] || fail "expected exactly 2 AARs under android/aar, found $aar_count"
if grep -Eq '^android/deps/' <<<"$entries"; then
  fail "the Business package must not bundle runtime dependencies (android/deps)"
fi
if grep -Eiq '\.(aar|jar)$' <<<"$(grep -Ev '^android/aar/(ktk-business-interop-release|tiktok-business-android-sdk-[0-9.]+)\.aar$' <<<"$entries")"; then
  fail "unexpected Java archive in package"
fi
# The Share package owns the OpenSDK; never mix the two.
if grep -Eq 'tiktok-open-sdk|ktk-tiktokinterop|Kapusch\.TikTok\.Android\.(props|targets)' <<<"$entries"; then
  fail "OpenSDK Share artifacts found in the Business package"
fi

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT
unzip -q "$package" "android/aar/*.aar" "*.nuspec" -d "$tmp_dir"

# The upstream AAR is shipped byte-for-byte as locked.
expected_sha="$(awk -v f="$sdk_aar" '$3 == f {print $2}' "$lock_file")"
[ -n "$expected_sha" ] || fail "$sdk_aar is not locked in $lock_file"
actual_sha="$(sha256_of "$tmp_dir/android/aar/$sdk_aar")"
[ "$actual_sha" = "$expected_sha" ] || fail "$sdk_aar checksum $actual_sha does not match lock $expected_sha"

unzip -p "$tmp_dir/android/aar/$sdk_aar" classes.jar > "$tmp_dir/sdk-classes.jar"
jar tf "$tmp_dir/sdk-classes.jar" | grep -Fxq 'com/tiktok/TikTokBusinessSdk.class' || fail "upstream AAR lacks TikTokBusinessSdk"

unzip -p "$tmp_dir/android/aar/ktk-business-interop-release.aar" classes.jar > "$tmp_dir/interop-classes.jar"
interop_classes="$(jar tf "$tmp_dir/interop-classes.jar" | grep '\.class$')"
grep -Fxq 'com/kapusch/tiktok/business/androidinterop/TikTokBusinessInterop.class' <<<"$interop_classes" || fail "interop AAR lacks TikTokBusinessInterop"
if grep -Ev '^com/kapusch/tiktok/business/androidinterop/' <<<"$interop_classes" | grep -q .; then
  fail "interop AAR repackages classes outside its own namespace"
fi

# JNI entry points used by the managed layer.
javap -constants -cp "$tmp_dir/interop-classes.jar" com.kapusch.tiktok.business.androidinterop.TikTokBusinessInterop > "$tmp_dir/interop.javap"
for signature in \
  'public static final void initialize(android.content.Context, android.os.Bundle, android.webkit.ValueCallback<android.os.Bundle>);' \
  'public static final boolean isInitialized();' \
  'public static final boolean isTrackingEnabled();' \
  'public static final java.lang.String sdkVersion();' \
  'public static final java.lang.String testEventCode();' \
  'public static final boolean startTrack();' \
  'public static final boolean trackEvent(java.lang.String, java.lang.String, java.lang.String);' \
  'public static final boolean identify(java.lang.String, java.lang.String, java.lang.String, java.lang.String);' \
  'public static final boolean logout();' \
  'public static final boolean flush();' \
  'public static final boolean updateAccessToken(java.lang.String);'; do
  grep -Fq "$signature" "$tmp_dir/interop.javap" || fail "interop JNI signature changed or missing: $signature"
done

# Wrapper error codes reported by the Kotlin facade must match the managed constants.
init_result="$repo_root/src/Kapusch.TikTok.Business.Android/TikTokBusinessInitResult.cs"
for pair in \
  ERROR_INVALID_CONFIGURATION:InvalidConfigurationErrorCode \
  ERROR_CONFIGURATION_REJECTED:ConfigurationRejectedErrorCode \
  ERROR_INVALID_CONTEXT:InvalidContextErrorCode \
  ERROR_NATIVE_EXCEPTION:NativeExceptionErrorCode \
  ERROR_INIT_INCOMPLETE:InitIncompleteErrorCode; do
  kotlin_name="${pair%%:*}"
  managed_name="${pair#*:}"
  kotlin_value="$(sed -nE "s/.*public static final int $kotlin_name = (-?[0-9]+);.*/\1/p" "$tmp_dir/interop.javap")"
  managed_value="$(sed -nE "s/.*public const int $managed_name = (-?[0-9]+);.*/\1/p" "$init_result")"
  [ -n "$kotlin_value" ] && [ "$kotlin_value" = "$managed_value" ] \
    || fail "$kotlin_name ($kotlin_value) does not match TikTokBusinessInitResult.$managed_name ($managed_value)"
done

rules="$(unzip -p "$tmp_dir/android/aar/ktk-business-interop-release.aar" proguard.txt)"
for rule in \
  '-keep class com.kapusch.tiktok.business.androidinterop.TikTokBusinessInterop { public static *; }' \
  '-keep class com.tiktok.** { *; }' \
  '-dontwarn com.android.installreferrer.**' \
  '-dontwarn com.android.billingclient.**'; do
  grep -Fq -- "$rule" <<<"$rules" || fail "interop consumer rules lack: $rule"
done

unzip -p "$package" THIRD_PARTY_NOTICES.md | grep -Fq "TikTok Business SDK for Android $sdk_version" || fail "THIRD_PARTY_NOTICES.md does not describe the Business SDK $sdk_version"

nuspec="$(cat "$tmp_dir"/*.nuspec)"
for dependency in Xamarin.AndroidX.Annotation Xamarin.AndroidX.Lifecycle.Process Xamarin.Kotlin.StdLib; do
  grep -Fq "id=\"$dependency\"" <<<"$nuspec" || fail "missing managed dependency $dependency"
done
if grep -Eq 'id="[^"]*(BillingClient|InstallReferrer)[^"]*"' <<<"$nuspec"; then
  fail "Billing and Install Referrer must stay consumer-owned"
fi

grep -Eq "/$sdk_version/tiktok-business-android-sdk-$sdk_version\.aar [0-9a-f]{64} " "$lock_file" || fail "lock file does not pin SDK $sdk_version"
grep -Eq '<sha256 value="[0-9a-f]{64}"' "$verification" || fail "Gradle verification metadata lacks SHA-256 entries"
grep -Fq "BundledSdkVersion = \"$sdk_version\"" "$repo_root/src/Kapusch.TikTok.Business.Android/TikTokBusinessSdk.cs" || fail "BundledSdkVersion does not match $sdk_version"

echo "TikTok Business Android package validation passed: $package"
