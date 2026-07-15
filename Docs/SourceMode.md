# Source mode

Source mode lets an application consume the managed project and already-built native assets from a sibling checkout. It does not download dependencies from the consuming app.

## Steps

1. Build native assets:

   ```sh
   bash src/Kapusch.TikTokApisForAndroidComponents/Native/Android/build.sh
   ```

2. Add a `ProjectReference` to `src/Kapusch.TikTokApisForAndroidComponents/Kapusch.TikTokApisForAndroidComponents.csproj`.
3. Import both `buildTransitive/Kapusch.TikTok.Android.props` and `buildTransitive/Kapusch.TikTok.Android.targets` from the checkout.
4. Set `UseKapuschTikTokAndroidInteropFromSource=true` in the consuming Android project.
5. Keep `KapuschTikTokAndroidIncludeBundledRuntimeDependencies=true` unless the app deliberately supplies and validates a compatible runtime graph.
