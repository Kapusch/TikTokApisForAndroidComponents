# Business package source mode

Source mode lets an application consume the Business managed project and its
already-built native assets from a sibling checkout. The consuming app never
downloads native dependencies.

1. Build the native assets:

   ```sh
   bash src/Kapusch.TikTok.Business.Android/Native/Android/build.sh
   ```

2. Add a `ProjectReference` to
   `src/Kapusch.TikTok.Business.Android/Kapusch.TikTok.Business.Android.csproj`.
3. Import both `buildTransitive/Kapusch.TikTok.Business.Android.props` and
   `buildTransitive/Kapusch.TikTok.Business.Android.targets` from the checkout.
4. Set `UseKapuschTikTokBusinessAndroidInteropFromSource=true` in the consuming
   Android project.

The targets fail with an explicit error when the source-mode AARs are missing.
The managed NuGet dependencies flow through the `ProjectReference` as usual.
