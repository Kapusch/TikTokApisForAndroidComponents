# Third-party notices

This package contains TikTok OpenSDK Android 2.3.1 (Core and Share), distributed
under the license included in the pinned upstream source archive. The package
also redistributes the exact Gradle runtime dependencies resolved by the locked
component build. Their notices and licenses remain those of their respective
authors.

The Share SDK contains a TikTok-not-installed landing path that references the
Google advertising identifier. Even when a consuming app checks installation
before invoking the SDK, the dependency remains part of the binary and must be
covered by that app's Google Play Data Safety audit.
