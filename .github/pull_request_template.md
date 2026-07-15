## Summary

- What does this change do?

## Checklist

- [ ] No client keys, signing material, or secrets committed
- [ ] Built native assets: `bash src/Kapusch.TikTokApisForAndroidComponents/Native/Android/build.sh`
- [ ] Packed NuGet: `dotnet pack src/Kapusch.TikTokApisForAndroidComponents/Kapusch.TikTokApisForAndroidComponents.csproj -c Release -o artifacts/nuget`
- [ ] Validated package: `scripts/validate-package.sh`
- [ ] Updated dependency locks, Gradle verification and notices if upstream inputs changed
- [ ] Updated docs if behavior or integration changed
