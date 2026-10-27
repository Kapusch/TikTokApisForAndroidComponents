# Third-party notices (Kapusch.TikTok.Business.Android)

This package redistributes, unchanged, the TikTok Business SDK for Android 1.7.1
(`tiktok-business-android-sdk-1.7.1.aar`, Copyright (c) 2020 TikTok Inc.), licensed
under the MIT license. The license text is included as
`licenses/TIKTOK-BUSINESS-SDK-LICENSE`. The artifact is pinned by SHA-256 in
`DependencyLocks/Android/business-lockstate.txt`.

The package does not redistribute any other third-party Java library. The
following runtime dependencies are resolved by the consuming application:

- AndroidX Annotation, AndroidX Lifecycle Process and the Kotlin standard library,
  through the managed `Xamarin.AndroidX.*` and `Xamarin.Kotlin.StdLib` NuGet
  packages declared as dependencies;
- Google Play Billing Library and Google Play Install Referrer Library, optional
  and owned by the application.

## Data collection note

The SDK merges the `android.permission.INTERNET` and
`com.google.android.gms.permission.AD_ID` permissions. When initialized it reads
the Google advertising identifier (unless advertiser id collection is disabled),
the Play install referrer when that library is present, device and app
information, and in-app purchases when automatic purchase tracking is active. It
sends this data to TikTok. The consuming application must cover it in its privacy
policy, consent flow and Google Play Data Safety declaration.
