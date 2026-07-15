# Kapusch.TikTok.Android

.NET Android wrapper for TikTok OpenSDK 2.3.1 Core + Share.

Native source and dependencies are restored and built in this repository. The
NuGet consumer never downloads native artifacts. The wrapper converts local
image paths to read-only `FileProvider` URIs, grants TikTok access, and maps the
TikTok callback to stable status values.

```sh
bash src/Kapusch.TikTokApisForAndroidComponents/Native/Android/build.sh
dotnet pack src/Kapusch.TikTokApisForAndroidComponents/Kapusch.TikTokApisForAndroidComponents.csproj -c Release
```

The consuming Android app must provide `tiktokClientKey` as an Android manifest
placeholder through its own local or CI configuration.

The package bundles the OpenSDK runtime dependency graph by default so a plain
consumer remains reproducible and does not download native artifacts. Apps that
already provide a compatible AndroidX/Kotlin/Google Play Services graph can set
`KapuschTikTokAndroidIncludeBundledRuntimeDependencies=false`. Use this opt-out
only after validating the complete application dependency graph, because it
transfers runtime dependency ownership back to the consuming app.

## Repository guides

- [`Docs/Integration.md`](Docs/Integration.md) — application integration contract.
- [`Docs/SourceMode.md`](Docs/SourceMode.md) — consume a sibling checkout without package-time downloads.
- [`samples/`](samples/) — secret-free minimal integration notes.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) — local validation and dependency-update rules.

Tags reachable from `master` publish to NuGet.org. Manual runs without a version publish a preview package to GitHub Packages.
