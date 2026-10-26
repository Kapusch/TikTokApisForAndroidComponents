# Kapusch TikTok Business interop: called from .NET through JNI by name.
-keep class com.kapusch.tiktok.business.androidinterop.TikTokBusinessInterop { public static *; }

# The upstream AAR keeps com.tiktok.** and the Billing API itself. The SDK also
# reaches these classes reflectively or through optional compile-only
# dependencies, as listed by the TikTok Business SDK Android integration guide.
-keep class com.tiktok.** { *; }
-keep class com.android.installreferrer.api.** { *; }
-keep class androidx.lifecycle.** { *; }
-keep class com.google.android.gms.ads.identifier.AdvertisingIdClient { public *; }
-keep class com.google.android.gms.ads.identifier.AdvertisingIdClient$Info { public *; }

# Optional, consumer-owned dependencies. The SDK degrades gracefully when they
# are absent, so missing classes must not fail an R8 build.
-dontwarn com.android.installreferrer.**
-dontwarn com.android.billingclient.**
-dontwarn com.google.android.gms.ads.identifier.**
