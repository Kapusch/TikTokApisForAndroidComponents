# Kapusch.TikTok.Android

Pinned TikTok OpenSDK Android 2.3.1 Core + Share wrapper for .NET Android.

Native runtime dependencies are bundled by default. Advanced consumers with a
compatible application-owned dependency graph may set
`KapuschTikTokAndroidIncludeBundledRuntimeDependencies=false`.

The interop Activity maps OpenSDK callbacks to `success`, `draft`, `cancelled`,
or `failed`, preserves native error codes, and reports
`returned_without_callback` when the user manually returns before TikTok sends
a result.
