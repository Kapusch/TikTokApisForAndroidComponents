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

## Share result contract

Start `AndroidTikTokInterop.ShareActivityClassName` for a result and pass at
least two existing image paths through `AndroidTikTokInterop.ExtraImagePaths`.
The wrapper publishes these result extras:

- `ExtraStatus`: `success`, `draft`, `cancelled`, or `failed`.
- `ExtraErrorCode`: present only when the status is `failed`.

TikTok cancellation responses (`-2` or sub-error `20013`) map to `cancelled`.
Other native failures retain their raw error and sub-error as a stable
`tiktok_<error>_sub_<sub-error>` diagnostic. A manual return to the caller
without an OpenSDK callback returns `failed` with
`returned_without_callback`, so the consuming UI does not remain pending.

The wrapper launches the request through the official OpenSDK `ShareApi`.
Cold-start latency and process recreation remain controlled by Android and the
installed TikTok application; consumers must not treat process continuity as
proof of share completion.
