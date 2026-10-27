# Kapusch TikTok Business interop: called from .NET through JNI by name.
-keep class com.kapusch.tiktok.business.androidinterop.TikTokBusinessInterop { public static *; }

# The upstream AAR keeps com.tiktok.** and the Billing API itself. Its direct
# references (AndroidX lifecycle, Install Referrer) are traced by R8 from the kept
# com.tiktok.** classes, so they need no keep rule here: a broad androidx.lifecycle
# keep would stop R8 from shrinking that family in every consuming app. Only the
# Google advertising id classes are reached by reflection.
-keep class com.tiktok.** { *; }
-keep class com.google.android.gms.ads.identifier.AdvertisingIdClient { public *; }
-keep class com.google.android.gms.ads.identifier.AdvertisingIdClient$Info { public *; }

# Optional, consumer-owned dependencies. The SDK degrades gracefully when they
# are absent, so missing classes must not fail an R8 build.
-dontwarn com.android.installreferrer.**
-dontwarn com.android.billingclient.**
-dontwarn com.google.android.gms.ads.identifier.**
