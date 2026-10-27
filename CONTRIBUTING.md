# Contributing

Thanks for contributing.

## Prerequisites

- JDK 21
- Android SDK
- .NET SDK 10 and the Android workload
- `rg`, `curl`, `unzip`, `sha256sum` or `shasum`, and `jar`

## Local validation

```sh
bash src/Kapusch.TikTokApisForAndroidComponents/Native/Android/build.sh
dotnet pack src/Kapusch.TikTokApisForAndroidComponents/Kapusch.TikTokApisForAndroidComponents.csproj \
  -c Release \
  -o artifacts/nuget
scripts/validate-package.sh
```

Business package:

```sh
bash src/Kapusch.TikTok.Business.Android/Native/Android/build.sh
dotnet pack src/Kapusch.TikTok.Business.Android/Kapusch.TikTok.Business.Android.csproj \
  -c Release \
  -o artifacts/nuget
scripts/validate-business-package.sh
dotnet build samples/Kapusch.TikTok.Business.Android.Sample/Kapusch.TikTok.Business.Android.Sample.csproj \
  -c Release -p:RuntimeIdentifier=android-arm64
python3 -I scripts/verify-public.py
```

`verify-public.py` checks tracked files for pasted App Secrets and checks that the sample
`TikTokBusiness.template.json` keeps placeholder values. It also rejects private terms, such as
the names of private consuming apps, that must never be written in this public tree. Those terms
are never tracked: the scanner reads them at run time, one per line or comma-separated, from the
`KAPUSCH_PRIVATE_TERMS` environment variable and from the untracked file `.git/info/private-terms`
(`#` starts a comment). Failures name the file and `private term #N`, never the term. The check
is skipped with a notice when no term is configured. CI reads the variable from the
`KAPUSCH_PRIVATE_TERMS` repository secret.

For direct project consumption, see [`Docs/SourceMode.md`](Docs/SourceMode.md) and
[`Docs/Business/SourceMode.md`](Docs/Business/SourceMode.md). Releases follow
[`Docs/Release.md`](Docs/Release.md).

## Pull requests

- Keep changes focused.
- Never commit TikTok credentials, app secrets or signing material.
- Update `DependencyLocks/Android/lockstate.txt`, Gradle verification metadata and third-party notices together when changing OpenSDK or runtime dependencies.
- Update `DependencyLocks/Android/business-lockstate.txt`, the Business Gradle verification metadata, `THIRD_PARTY_NOTICES.Business.md`, `BundledSdkVersion` and the validator together when changing the Business SDK. Regenerate verification metadata with `gradle --write-verification-metadata sha256` and review the diff.
- Validate both bundled-runtime and consumer-provided-runtime modes when changing package injection.

Contributions are licensed under the repository MIT license.
