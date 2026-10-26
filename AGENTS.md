# Kapusch.TikTokApisForAndroidComponents — AI Working Agreement

## Goals

- Produce a reproducible `Kapusch.TikTok.Android` NuGet for TikTok OpenSDK Core + Share 2.3.1.
- Produce a reproducible `Kapusch.TikTok.Business.Android` NuGet for the TikTok Business SDK 1.7.1.
- Keep the two packages independent: no shared native artifact, separate locks, validators and release tags (`v*` and `business-v*`).
- Do not commit client keys, app secrets, signing material, or app-specific secrets.

## Packaging constraints

- Public OSS repo: keep docs and samples generic and not app-specific.
- Consumers must never download native dependencies. Build and package all required AAR/JAR artifacts here.
- Keep the OpenSDK version, source archive and checksums pinned in `DependencyLocks/Android/lockstate.txt` and Gradle verification metadata.
- Keep the Business SDK AAR, its license and the Gradle distribution pinned in `DependencyLocks/Android/business-lockstate.txt`, plus the Business Gradle verification metadata.
- The Business package ships only the upstream AAR and the interop AAR. Shared AndroidX/Kotlin classes come from managed NuGets; Google Play Billing and Install Referrer stay consumer-owned. Never bundle them.
- Nothing may initialize the Business SDK or reach the network before `TikTokBusinessSdk.InitializeAsync` is called.
- Do not patch upstream TikTok sources silently. Document and checksum-audit every necessary patch.

## Repo layout

- `src/Kapusch.TikTokApisForAndroidComponents/` — Share package: managed API, native wrapper and buildTransitive assets.
- `src/Kapusch.TikTok.Business.Android/` — Business package: managed API, Kotlin JNI facade and buildTransitive assets.
- `DependencyLocks/Android/` — locked upstream inputs for both packages.
- `scripts/` — deterministic package validation.
- `Docs/` — integration and source-mode documentation.
- `samples/` — secret-free integration example.

## Safety

- Do not add dependency ingestion paths without documenting and locking them.
- When package contents change, update `scripts/validate-package.sh` (Share) or `scripts/validate-business-package.sh` (Business) in the same commit.
- Keep every tracked file generic: `scripts/verify-public.py` must pass.
