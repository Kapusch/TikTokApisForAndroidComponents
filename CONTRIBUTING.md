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

For direct project consumption, see [`Docs/SourceMode.md`](Docs/SourceMode.md).

## Pull requests

- Keep changes focused.
- Never commit TikTok credentials or signing material.
- Update `DependencyLocks/Android/lockstate.txt`, Gradle verification metadata and third-party notices together when changing OpenSDK or runtime dependencies.
- Validate both bundled-runtime and consumer-provided-runtime modes when changing package injection.

Contributions are licensed under the repository MIT license.
