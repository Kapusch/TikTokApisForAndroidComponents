# Kapusch.TikTokApisForAndroidComponents — AI Working Agreement

## Goals

- Produce a reproducible `Kapusch.TikTok.Android` NuGet for TikTok OpenSDK Core + Share 2.3.1.
- Do not commit client keys, signing material, or app-specific secrets.

## Packaging constraints

- Public OSS repo: keep docs and samples generic and not app-specific.
- Consumers must never download native dependencies. Build and package all required AAR/JAR artifacts here.
- Keep the OpenSDK version, source archive and checksums pinned in `DependencyLocks/Android/lockstate.txt` and Gradle verification metadata.
- Do not patch upstream TikTok sources silently. Document and checksum-audit every necessary patch.

## Repo layout

- `src/Kapusch.TikTokApisForAndroidComponents/` — managed API, native wrapper and buildTransitive assets.
- `DependencyLocks/Android/` — locked upstream source and native dependency inventory.
- `scripts/` — deterministic package validation.
- `Docs/` — integration and source-mode documentation.
- `samples/` — secret-free integration example.

## Safety

- Do not add dependency ingestion paths without documenting and locking them.
- When package contents change, update `scripts/validate-package.sh` in the same commit.
