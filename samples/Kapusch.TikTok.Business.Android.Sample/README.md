# TikTok Business SDK sample

A plain .NET Android app that consumes the **packed** `Kapusch.TikTok.Business.Android`
package from `../../artifacts/nuget`, like an application would. It shows the
consent-gated recipe: nothing reaches the SDK until the user taps **Accept**.

## Configure (never commit credentials)

```sh
cp Assets/TikTokBusiness.template.json Assets/TikTokBusiness.local.json
# edit appId (the app package name), tikTokAppId (one id, or several separated by commas)
# and accessToken (Events Manager app secret)
```

`Assets/*.local.json` is git-ignored. Without it the app starts and reports the
missing configuration.

## Build and run

```sh
bash src/Kapusch.TikTok.Business.Android/Native/Android/build.sh
dotnet pack src/Kapusch.TikTok.Business.Android/Kapusch.TikTok.Business.Android.csproj -c Release -o artifacts/nuget
dotnet build samples/Kapusch.TikTok.Business.Android.Sample/Kapusch.TikTok.Business.Android.Sample.csproj -c Release -p:RuntimeIdentifier=android-arm64
```

Run from the repository root. Release builds use R8, so they also prove that the
consumer rules keep what the SDK needs and that no class is duplicated.

- `-p:SampleWithoutOptionalAndroidDeps=true` drops Google Play Billing and Install
  Referrer to prove the package links without them.
- After repacking the same version, clear `~/.nuget/packages/kapusch.tiktok.business.android`
  or pack a new version and pass `-p:KapuschTikTokBusinessAndroidVersion=<version>`.
- For a scripted smoke test, start the activity with `--ez auto_consent true`.
  The sample logs under the `KtkBusinessSample` tag.

Use Events Manager **Test events** with `debugMode: true` to see events arrive.
The sample sets `DisablePaymentTracking` and sends `StartTrial` and `Subscribe`
manually with a transaction id as `event_id`.
