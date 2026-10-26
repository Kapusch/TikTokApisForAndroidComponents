# TikTokApisForAndroidComponents

.NET Android packages for two independent TikTok SDKs. Native sources and
artifacts are restored, checksum-verified and built in this repository; NuGet
consumers never download native artifacts.

| Package | Upstream SDK | Purpose | Release tags |
|---|---|---|---|
| `Kapusch.TikTok.Android` | TikTok OpenSDK 2.3.1 Core + Share | Share images to TikTok | `vX.Y.Z` |
| `Kapusch.TikTok.Business.Android` | TikTok Business SDK (App Events) 1.7.1 | Install and in-app event attribution for TikTok ads | `business-vX.Y.Z` |

The two packages share no native artifact and can be used separately or together.

## Kapusch.TikTok.Android (OpenSDK Share)

The wrapper converts local image paths to read-only `FileProvider` URIs, grants
TikTok access, and maps the TikTok callback to stable status values. It delegates
the launch to the official `ShareApi` and recovers if the user returns to the
caller without a TikTok callback.

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

See [`Docs/Integration.md`](Docs/Integration.md) and [`Docs/SourceMode.md`](Docs/SourceMode.md).

## Kapusch.TikTok.Business.Android (Business SDK)

A static `TikTokBusinessSdk` API over the official SDK:

- consent-gated `InitializeAsync`: nothing native loads before it is called;
- automatic-event and attribution opt-outs;
- `TrackEvent` plus typed `StartTrial`, `Subscribe`, `Purchase`, `Registration`
  and `Login` helpers, with `event_id` deduplication;
- constants for every standard event, property and currency of the SDK;
- `Identify`, `Logout`, `Flush` and `StartTrack`.

The upstream AAR ships unchanged next to a thin Kotlin JNI facade. Shared AndroidX
and Kotlin classes come from managed NuGet dependencies. Google Play Billing and
Install Referrer stay owned by the application. Minimum Android API 23.

```sh
bash src/Kapusch.TikTok.Business.Android/Native/Android/build.sh
dotnet pack src/Kapusch.TikTok.Business.Android/Kapusch.TikTok.Business.Android.csproj -c Release -o artifacts/nuget
scripts/validate-business-package.sh
```

Read [`Docs/Business/Integration.md`](Docs/Business/Integration.md) before
shipping. It covers the consent recipe, automatic purchase double counting,
permissions and ATT / SKAdNetwork guidance.

## Repository guides

- [`Docs/Integration.md`](Docs/Integration.md): Share package integration contract.
- [`Docs/Business/Integration.md`](Docs/Business/Integration.md): Business package integration contract.
- [`Docs/SourceMode.md`](Docs/SourceMode.md) and [`Docs/Business/SourceMode.md`](Docs/Business/SourceMode.md): consume a sibling checkout without package-time downloads.
- [`Docs/Release.md`](Docs/Release.md): tag families and release channels.
- [`samples/`](samples/): secret-free integration examples.
- [`CONTRIBUTING.md`](CONTRIBUTING.md): local validation and dependency-update rules.
