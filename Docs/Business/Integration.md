# Kapusch.TikTok.Business.Android integration

`Kapusch.TikTok.Business.Android` wraps the TikTok Business SDK for Android 1.7.1
(also called the TikTok App Events SDK). It is independent of the
`Kapusch.TikTok.Android` OpenSDK Share package: the two can be used separately or
together.

## Requirements

- .NET 10 for Android, minimum Android API 23. The AndroidX lifecycle 2.10 family
  the package depends on, like Google Play Billing 8.3, requires API 23.
- An app connected to TikTok Events Manager with **SDK** as the data source. You
  need three values from Events Manager:
  - **App ID**: on Android, the application package name;
  - **TikTok App ID**: a numeric id per ad account; several can be passed,
    separated by commas;
  - **App Secret**: passed as `AccessToken`, the name the native SDK uses.

Never commit the app secret. Load it from your own build-time or remote
configuration.

## What the package contains

- `android/aar/tiktok-business-android-sdk-1.7.1.aar`: the upstream AAR, unchanged
  and checked against its SHA-256 lock.
- `android/aar/ktk-business-interop-release.aar`: a Kotlin facade called through
  JNI, with consumer R8 rules.
- Managed dependencies on `Xamarin.AndroidX.Annotation`,
  `Xamarin.AndroidX.Lifecycle.Process` and `Xamarin.Kotlin.StdLib`. Shared Java
  classes therefore unify with the rest of the app instead of being duplicated.

Consumer builds never download native artifacts from this package.

### Dependencies the application owns

The upstream SDK declares these libraries as compile-only. The package does not
bundle them, so they never duplicate a copy the application already ships.

| Library | Needed for | How to provide it |
|---|---|---|
| Google Play Billing (v5 to v9) | Automatic in-app purchase tracking only | Usually already present through a subscription SDK. Otherwise `Xamarin.Android.Google.BillingClient`. |
| Google Play Install Referrer 2.2 | Play install-referrer attribution | `Xamarin.Google.Android.InstallReferrer` (it downloads the AAR from Google's Maven at build time). |
| Google Play services Ads Identifier | Optional | The SDK reads the GAID by reflection and falls back to binding the Play services ID service, so no library is required. |

The SDK detects each one at runtime and degrades gracefully when it is missing.
The consumer R8 rules include `-dontwarn` entries, so an R8 build still succeeds
without them.

## Consent-gated initialization

Nothing native is loaded, and no network request is made, until
`TikTokBusinessSdk.InitializeAsync` is called. TikTok's own compliance guidance
says not to initialize the SDK before consent. Holding back only the event
delivery is not enough, because the SDK requests its remote configuration from
`analytics.us.tiktok.com` during initialization, even with `DisableAutoStart`.

Recommended recipe:

1. Persist the user's choice (accepted, declined, or not asked yet).
2. On every launch, when the choice is **accepted**, call `InitializeAsync` early,
   ideally from `MainApplication.OnCreate` on the main thread. When the choice is
   **declined** or **not asked yet**, do not call it.
3. When the user accepts during the session, call `InitializeAsync` at that moment.
   The SDK still reports `InstallApp` on its first initialization on the device.
4. When the user withdraws consent, persist the choice and call
   `SetTrackingEnabled(false)`. On Android this stops the wrapper from forwarding
   new calls. The native SDK has no public API to pause delivery once it has
   started, so automatic and already queued events can still be sent until the
   process ends. From the next launch, step 2 keeps the SDK uninitialized.

```csharp
using Kapusch.TikTok.Business.Android;

var config = new TikTokBusinessConfig(
	appId: "com.example.app",
	tikTokAppId: settings.TikTokAppId,
	accessToken: settings.TikTokAppSecret)
{
	// Recommended for subscription apps, see below.
	DisablePaymentTracking = true,
	DisableEnhancedDataPostback = true,
	DebugMode = isDebugBuild,
};

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await TikTokBusinessSdk.InitializeAsync(config, timeout.Token);
if (!result.IsSuccess)
{
	logger.LogWarning("TikTok init failed: {Code} {Message}", result.ErrorCode, result.ErrorMessage);
}
```

`InitializeAsync` completes from the SDK's own completion callback. Concurrent and
repeated calls share one native initialization, because the SDK allows only one
per process. A failed initialization can be retried. Cancelling the token only
stops waiting. Calls to the tracking methods made before initialization are
ignored.

To queue events at startup but send them only after an explicit step, set
`DisableAutoStart = true` and later call `TikTokBusinessSdk.StartTrack()`.

### Result codes

| `ErrorCode` | Meaning |
|---|---|
| `invalid_app_id` | `AppId` is not an Android package name (validated with the SDK's own pattern). |
| `invalid_tiktok_app_id` | `TikTokAppId` is not a comma-separated list of numeric ids. |
| `missing_access_token` | `AccessToken` is empty. |
| `invalid_context` | No Android application context is available. |
| `native_unavailable` | The native classes are missing, for example because `DisableKapuschTikTokBusinessAndroidNativeAssets=true`. |
| `native_exception` | The native layer threw during initialization. |
| `tiktok_<code>` | The SDK's `TTInitCallback.fail(code, message)`. `NativeErrorCode` holds the raw code. |

## Configuration reference

| Property | Native call | Notes |
|---|---|---|
| `DebugMode` | `openDebugMode()` | Shows events in Events Manager **Test events**. |
| `LogLevel` | `setLogLevel` | `None` (default), `Info`, `Warn`, `Debug`. |
| `DisableAutoStart` | `disableAutoStart()` | Holds delivery until `StartTrack()`. |
| `DisableAutomaticTracking` | `disableAutoEvents()` | Disables `InstallApp`, `LaunchAPP` and `2Dretention`. On Android it does **not** disable automatic purchase tracking. |
| `DisableInstallTracking` / `DisableLaunchTracking` / `DisableRetentionTracking` | `disableInstallLogging()` / `disableLaunchLogging()` / `disableRetentionLogging()` | One automatic event each. |
| `DisablePaymentTracking` | `disableAutoIapTrack()` | See the double counting warning below. |
| `DisableAdvertiserIdCollection` | `disableAdvertiserIDCollection()` | Stops GAID collection. Also remove the `AD_ID` permission, see below. |
| `DisableEnhancedDataPostback` | `disableAutoEnhancedDataPostbackEvent()` | Stops automatic UI interaction capture. |
| `DisableMonitor` | `disableMonitor()` | Stops the SDK's own health metrics. |
| `LimitedDataUse` | `enableLimitedDataUse()` | Limited Data Use mode (US state privacy laws). |
| `DisableAppTrackingDialog`, `DisableSKAdNetworkSupport` | iOS only | Ignored on Android. They exist so shared code can configure both platforms. |

## Events

Use `TikTokBusinessEventNames` and `TikTokBusinessPropertyNames`. They mirror the
SDK 1.7.1 constants verbatim. TikTok discards custom property keys.

```csharp
// Free trial started: value is what was actually charged, usually 0.
TikTokBusinessSdk.TrackStartTrial(value: 0m, currency: "EUR", eventId: transactionId,
	contentId: productId, contentType: "subscription");

// Paid subscription started.
TikTokBusinessSdk.TrackSubscribe(value: 9.99m, currency: "EUR", eventId: transactionId,
	contentId: productId, contentType: "subscription");

// Any standard or custom event.
TikTokBusinessSdk.TrackEvent(TikTokBusinessEventNames.ViewContent, new Dictionary<string, object>
{
	[TikTokBusinessPropertyNames.ContentId] = "paywall",
	[TikTokBusinessPropertyNames.Contents] = new[] { new TikTokBusinessContent(ContentId: productId, Price: 9.99m, Quantity: 1) },
});
```

- Pass a stable `eventId`, for example the store transaction id, so TikTok can
  deduplicate retries. The SDK generates one when it is omitted.
- The typed helpers reject a currency the SDK does not support
  (`TikTokBusinessCurrencies.Supported`) and a value without a currency.
- Property values may be strings, booleans, numbers, `TikTokBusinessContent`,
  dictionaries or lists. `null` values are omitted.
- There is no `CompletePayment` app event. That name belongs to TikTok's web pixel.
  Use `Purchase`.
- `InstallApp`, `LaunchAPP` and `2Dretention` are automatic. Send them manually
  only if you disabled the automatic ones.
- The Android SDK spells the in-app ad impression event `InAppAdImpr`; the iOS SDK
  spells it `InAppADImpr`. Each package uses its own SDK's spelling.

`Identify(externalId, externalUserName, phoneNumber, email)` enables advanced
matching. The SDK hashes the values with SHA-256 on the device. `Logout()` clears
the identity. `Flush()` asks the SDK to send queued events now; it is not a
delivery receipt.

## Automatic in-app purchase tracking: avoid double counting

When Google Play Billing is in the app, the SDK's automatic in-app purchase
tracking reads purchases through its own `BillingClient` and reports each one as
`Purchase`. For a subscription with a trial phase it uses the price of the paid
phase. A free-trial start is therefore reported as a full-price `Purchase`, and
it is never reported as `StartTrial` or `Subscribe`. On iOS, the StoreKit
observer likewise reports every transaction, renewals included, as `Purchase` at
catalog price.

If you also send `StartTrial` / `Subscribe` / `Purchase` yourself, or forward
purchases from a server, the same conversion is counted twice with inflated
values.

Recommendation for subscription apps:

1. Set `DisablePaymentTracking = true`.
2. Send `StartTrial` and `Subscribe` (and `Purchase` for one-time products)
   manually, with the amount actually charged and the transaction id as `eventId`.
3. Verify in Events Manager **Test events** that no automatic `Purchase` arrives.
   The SDK only forwards `DisablePaymentTracking` to TikTok's configuration
   endpoint, and the server makes the final decision. There is no other local
   switch on Android: `DisableAutomaticTracking` does not cover purchases.

## Android permissions, R8 and Data Safety

- The SDK merges `INTERNET` and `com.google.android.gms.permission.AD_ID`. If you
  set `DisableAdvertiserIdCollection`, also remove the permission in the app
  manifest:

  ```xml
  <uses-permission android:name="com.google.android.gms.permission.AD_ID" tools:node="remove" />
  ```

- The interop AAR's consumer rules keep the facade, `com.tiktok.**`, the Install
  Referrer API, `androidx.lifecycle.**` and the reflective `AdvertisingIdClient`
  members. Validate your own Release build with R8 enabled.
- The SDK installs a chained default uncaught-exception handler during
  initialization. When you use other crash reporters, initialize them first and
  check that crashes are still reported.
- Declare the data the SDK sends to TikTok (device identifiers, app activity,
  purchase history when automatic purchase tracking is active) in Google Play
  Data Safety, as data **shared** with a third party.

## iOS counterpart: ATT and SKAdNetwork

These points apply to the iOS package and to shared app code:

- **ATT.** Ask for App Tracking Transparency only after the user accepts in your
  own consent flow, and never after a refusal. Without ATT authorization the SDK
  sends no IDFA, and events still flow with the IDFV.
- **Tracking domains.** Do not list `analytics.us.tiktok.com` in
  `NSPrivacyTrackingDomains`. iOS blocks every request to a listed domain when ATT
  is not granted, which would also stop install measurement for those users.
- **SKAdNetwork.** Only one SDK in the app should update SKAdNetwork conversion
  values. If another SDK owns them, set `DisableSKAdNetworkSupport = true` and
  upload the matching schema in Events Manager only if TikTok asks for one. An
  advertised app does not need `SKAdNetworkItems`.

## Coexistence with the OpenSDK Share package

The Business SDK and the OpenSDK use different Java packages (`com.tiktok.*`
versus `com.tiktok.open.sdk.*`) and share no artifact. The Share package bundles
its own runtime dependency graph by default. In an app that already gets AndroidX
and Kotlin from NuGet, such as any MAUI app, set
`KapuschTikTokAndroidIncludeBundledRuntimeDependencies=false` to avoid duplicate
classes, as described in [`../Integration.md`](../Integration.md).

## MSBuild properties

| Property | Default | Effect |
|---|---|---|
| `DisableKapuschTikTokBusinessAndroidNativeAssets` | `false` | Skips injecting both AARs. `InitializeAsync` then returns `native_unavailable`. |
| `UseKapuschTikTokBusinessAndroidInteropFromSource` | `false` | Uses a sibling checkout's native build. See [`SourceMode.md`](SourceMode.md). |
