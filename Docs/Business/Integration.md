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
  - **TikTok App ID**: a numeric id per ad account, passed in `TikTokAppIds`;
    several can be passed;
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
`TikTokBusinessSdk.InitializeAsync` is called. Until a successful initialization
every other member returns `false` (or `null`), and nothing is queued. TikTok's own
compliance guidance says not to initialize the SDK before consent. Holding back
only the event delivery is not enough, because the SDK requests its remote
configuration from `analytics.us.tiktok.com` during initialization, even with
`DisableAutoStart`.

Recommended recipe:

1. Persist the user's choice (accepted, declined, or not asked yet).
2. On every launch, when the choice is **accepted**, call `InitializeAsync` early,
   ideally from `MainApplication.OnCreate`. It can be called from any thread: the
   wrapper runs the SDK initialization on the main thread. When the choice is
   **declined** or **not asked yet**, do not call it.
3. When the user accepts during the session, call `InitializeAsync` at that moment.
   The SDK still reports `InstallApp` on its first initialization on the device.
4. When the user withdraws consent, persist the choice, call
   `SetTrackingEnabled(false)` and `Logout()`. From the next launch, step 2 keeps
   the SDK uninitialized. See the Android limitation below.

```csharp
using Kapusch.TikTok.Business.Android;

var config = new TikTokBusinessConfig
{
	AppId = "com.example.app",
	TikTokAppIds = settings.TikTokAppIds,
	AccessToken = settings.TikTokAppSecret,
	// Recommended for subscription apps, see below.
	DisablePaymentTracking = true,
	DisableEnhancedDataPostback = true,
	DebugMode = isDebugBuild,
};

var result = await TikTokBusinessSdk.InitializeAsync(config);
if (!result.IsSuccess)
{
	logger.LogWarning("TikTok init failed: {Code} {Message}", result.ErrorCode, result.ErrorMessage);
}
```

`InitializeAsync` throws `ArgumentException` when a value is missing or
malformed: `AppId` must be an Android package name (checked with the SDK's own
pattern), each TikTok App ID must be numeric. Otherwise it completes from the
SDK's own completion callback, always within about 30 seconds:

- **One initialization per process.** The SDK is a singleton. Concurrent calls
  share one native initialization, and a call after a success returns the current
  state (`Started` or `InitializedTrackingDisabled`) without reconfiguring the SDK.
- **The wait is bounded.** SDK 1.7.1 returns silently, without calling back, from
  every `initializeSdk` call after the first, and its asynchronous init step
  swallows its own exceptions without calling back. The wrapper therefore never
  calls `initializeSdk` twice and reports `InitTimeoutErrorCode` (`1105`) when the
  SDK stays silent for 30 seconds.
- **Retries are deterministic.** After a failure before the SDK was entered
  (`NativeUnavailableErrorCode`, `InvalidContextErrorCode`), a retry runs the
  checks again; missing native classes stay missing for the life of the process.
  Once the SDK was entered, a retry returns the current state when a timed out
  initialization finished late (then `IsInitialized` is already `true`), and
  `InitIncompleteErrorCode` (`1104`) otherwise.
- **Deferred start.** With `DisableAutoStart = true` the result is
  `InitializedTrackingDisabled` with `ErrorCode` `0`, and the SDK holds delivery
  until `TikTokBusinessSdk.StartTrack()`. This is not a consent gate, see above.

### Threading and call order

Only `InitializeAsync` switches to the main thread. Every other member calls the
SDK synchronously on the calling thread and returns whether the SDK accepted the
call. `Identify` and `Logout` change the SDK identity before they return, so calls
made in sequence take effect in that order: an event tracked right after `Identify`
carries the identity, and one tracked after `Logout` does not.

Android limitation: the SDK copies the identity into an event when its own event
thread builds it, shortly after the tracking call. An event tracked right before
`Identify` or `Logout` may therefore carry the newer identity. The SDK offers no
public way to order identity changes behind queued events (its
`getAppEventLogger()` returns `null` outside SDK call stacks).

### Stopping tracking (Android limitation)

`SetTrackingEnabled(false)` cannot pause the Android SDK: version 1.7.1 has no
public API to stop delivery once it has started. On Android the wrapper then stops
forwarding `Track*`, `Identify` and `Flush` calls, which return `false` and are
dropped (on iOS they are kept in the SDK's local store). Automatic events and the
events the SDK already holds are still sent until the process ends.
`IsTrackingEnabled` returns `false`. `Logout` still works. `SetTrackingEnabled(true)`
is the same as `StartTrack()`.

### Result codes

`TikTokBusinessInitResult` has a `Status` (`Started`, `InitializedTrackingDisabled`
or `Failed`), an `ErrorCode` that is `0` unless the status is `Failed`, and an
`ErrorMessage` for logs. The codes are constants on `TikTokBusinessInitResult`.

| `ErrorCode` | Constant | Meaning |
|---|---|---|
| `-2` | (SDK) | `TTInitCallback.fail`: the SDK received an empty App ID or TikTok App ID. SDK 1.7.1 reports no other code. |
| `1001` | `InvalidConfigurationErrorCode` | The native layer received an empty App ID, TikTok App ID or App Secret. Shared with iOS. |
| `1002` | `ConfigurationRejectedErrorCode` | The values do not match the SDK's patterns (only reachable by calling the native layer directly). Shared with iOS. |
| `1101` | `InvalidContextErrorCode` | Android only: no Android application context is available. |
| `1102` | `NativeUnavailableErrorCode` | Android only: the native classes are missing, for example because `DisableKapuschTikTokBusinessAndroidNativeAssets=true`. |
| `1103` | `NativeExceptionErrorCode` | Android only: the native layer threw while configuring the SDK. |
| `1104` | `InitIncompleteErrorCode` | Android only: an earlier native initialization in this process did not complete. Every retry in the same process returns this code. |
| `1105` | `InitTimeoutErrorCode` | Android only: the SDK did not report its initialization within 30 seconds. |

## Configuration reference

| Property | Native call | Notes |
|---|---|---|
| `DebugMode` | `openDebugMode()` | Shows events in Events Manager **Test events**. |
| `LogLevel` | `setLogLevel` | `null` keeps the SDK default (`NONE`). The SDK only has `NONE`, `INFO`, `WARN`, `DEBUG`, in that order, and logs errors at `INFO`: `Error` maps to `INFO`, `Warn` and `Info` to `WARN`, `Debug` and `Verbose` to `DEBUG`. |
| `DisableAutoStart` | `disableAutoStart()` | Holds delivery until `StartTrack()`; `InitializeAsync` reports `InitializedTrackingDisabled`. |
| `DisableAutomaticTracking` | `disableAutoEvents()` | Disables `InstallApp`, `LaunchAPP` and `2Dretention`. On Android it does **not** disable automatic purchase tracking. |
| `DisableInstallTracking` / `DisableLaunchTracking` / `DisableRetentionTracking` | `disableInstallLogging()` / `disableLaunchLogging()` / `disableRetentionLogging()` | One automatic event each. |
| `DisablePaymentTracking` | `disableAutoIapTrack()` | See the double counting warning below. |
| `DisableAdvertiserIdCollection` | `disableAdvertiserIDCollection()` | Stops GAID collection. Also remove the `AD_ID` permission, see below. |
| `DisableEnhancedDataPostback` | `disableAutoEnhancedDataPostbackEvent()` | Stops automatic UI interaction capture. |
| `DisableMonitor` | `disableMonitor()` | Stops the SDK's own health metrics. |
| `LimitedDataUse` | `enableLimitedDataUse()` | Limited Data Use mode (US state privacy laws). |
| `LowPerformanceDevice` | `setIsLowPerformanceDevice(true)` | Lets the SDK reduce optional work. |
| `DisableAppTrackingDialog`, `DisableSKAdNetworkSupport`, `AttAuthorizationDelay`, `CustomUserAgent` | iOS only | Ignored on Android. They exist so shared code can configure both platforms. |

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
- The typed helpers reject a negative value, a value without a currency and a
  currency that is not a three-letter ISO 4217 code, like the iOS package.
  `TikTokBusinessCurrencies.Supported` lists the codes the Android SDK enumerates.
- The typed helpers send `value` as a number, like the SDK's
  `TTContentsEvent.Builder.setValue(double)`. A `TikTokBusinessContent` sends its
  price as a string and omits an unset price, like `TTContentParams`.
- Property values may be strings, booleans, numbers, dates, `TikTokBusinessContent`,
  dictionaries or lists, nested up to 8 levels. `null` values are sent as JSON `null`.
- Every tracking call returns `false` when the SDK is not initialized or tracking
  is disabled, and `true` once the SDK accepted the call.
- There is no `CompletePayment` app event. That name belongs to TikTok's web pixel.
  Use `Purchase`.
- `InstallApp`, `LaunchAPP` and `2Dretention` are automatic. Send them manually
  only if you disabled the automatic ones.
- The Android SDK spells the in-app ad impression event `InAppAdImpr`; the iOS SDK
  spells it `InAppADImpr`. Each package uses its own SDK's spelling.

`Identify(externalId, externalUserName, phoneNumber, email)` enables advanced
matching; it returns `false` when every value is empty. The SDK hashes the values
with SHA-256 on the device and keeps the first identity until `Logout()`.
`Flush()` asks the SDK to send queued events now; it is not a delivery receipt.
`UpdateAccessToken` replaces the App Secret after a rotation. In debug mode,
`TestEventCode` returns the code to enter in Events Manager **Test events**.

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

- The interop AAR's consumer rules keep the facade, `com.tiktok.**` and the
  reflectively loaded `AdvertisingIdClient` members only. AndroidX lifecycle and
  Install Referrer classes are referenced directly by the SDK, so R8 keeps what it
  uses and still shrinks the rest. Validate your own Release build with R8 enabled.
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

## Differences from the iOS package

`Kapusch.TikTok.Business.iOS` exposes the same types, members, configuration
fields, result codes and status semantics, so shared code compiles and behaves the
same on both platforms. The remaining differences come from the two SDKs:

| Topic | Android | iOS |
|---|---|---|
| `AppId` | Application package name. | Numeric App Store ID. |
| SDK version | 1.7.1 | 1.7.2 |
| `SetTrackingEnabled(false)` | Stops forwarding manual calls (dropped); automatic and held events are still sent. | Pauses sending; events stay queued locally. |
| Event tracked right before `Identify` / `Logout` | May carry the newer identity, see "Threading and call order". | Same call-order contract for events tracked after these calls. |
| Initialization wait | Bounded to 30 seconds (`InitTimeoutErrorCode`). | Completes from the SDK callback. |
| Wrapper error codes | `1001`, `1002` and the Android-only `11xx` codes. | `1001`, `1002`. |
| SDK error codes | `-2` only. | Forwarded unchanged, for example `-2`; the `-1` the SDK reports with `DisableAutoStart` becomes `InitializedTrackingDisabled` with code `0`. |
| `value` in the typed helpers | Number. | Decimal string. |
| `TikTokBusinessContent.Price` unset | Omitted. | `0`. |
| `LogLevel` | `Error` → `INFO`, `Warn`/`Info` → `WARN`, `Debug`/`Verbose` → `DEBUG`. | One SDK level each. |
| In-app ad impression name | `InAppAdImpr` | `InAppADImpr` |
| `PurchaseRestored` | Never sent by the SDK. | Sent by the purchase observer. |
| `DisableAutomaticTracking` | Lifecycle events only, not purchases. | Also stops the purchase observer. |
| Android only | `DisableAdvertiserIdCollection`, `DisableMonitor`, `BundledSdkVersion`, `TikTokBusinessCurrencies`, `TikTokBusinessPropertyNames.OrderId`. | Accepted and ignored where it is a config field. |
| iOS only | Accepted and ignored: `DisableAppTrackingDialog`, `DisableSKAdNetworkSupport`, `AttAuthorizationDelay`, `CustomUserAgent`. | |

## Coexistence with the OpenSDK Share package

The Business SDK and the OpenSDK use different Java packages (`com.tiktok.*`
versus `com.tiktok.open.sdk.*`) and share no artifact. The Share package bundles
its own runtime dependency graph by default, while the Business package gets
AndroidX and Kotlin from NuGet. An app that references both packages must set
`KapuschTikTokAndroidIncludeBundledRuntimeDependencies=false`, as described in
[`../Integration.md`](../Integration.md): with the bundled graph the Java build
fails on duplicate AndroidX classes. With that setting, a Release build with R8
that references both packages succeeds.

## MSBuild properties

| Property | Default | Effect |
|---|---|---|
| `DisableKapuschTikTokBusinessAndroidNativeAssets` | `false` | Skips injecting both AARs. `InitializeAsync` then returns `NativeUnavailableErrorCode` (`1102`). |
| `UseKapuschTikTokBusinessAndroidInteropFromSource` | `false` | Uses a sibling checkout's native build. See [`SourceMode.md`](SourceMode.md). |
