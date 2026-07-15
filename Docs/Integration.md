# Integration

The package contributes the callback activity and the pinned TikTok Core/Share
artifacts. Set `UseKapuschTikTokAndroidInteropFromSource=true` to use a sibling
checkout. The source build must exist before the consuming app resolves Android
libraries.

`KapuschTikTokAndroidIncludeBundledRuntimeDependencies` defaults to `true`.
Set it to `false` only when the application already supplies a compatible
runtime graph and validates the resulting AAB for missing or duplicate classes.

The callback activity is exported because TikTok launches it with the result.
It rejects direct sharing when TikTok is no longer installed and never calls the
SDK's install-landing flow from that state.
