# Samples

- `Kapusch.TikTok.Android.Sample` documents the smallest OpenSDK Share integration
  surface without committing a client key. Provide the manifest placeholder locally
  and two readable JPEG paths before runtime testing. CI validates the package itself
  because editor handoff requires an approved TikTok application and a physical device.
- `Kapusch.TikTok.Business.Android.Sample` is a buildable clean-consumer app for the
  TikTok Business SDK package. CI builds it in Release with R8, with and without the
  optional Billing and Install Referrer dependencies. Credentials come from an
  untracked local asset.
