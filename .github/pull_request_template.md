## Summary

- What does this change do?

## Checklist

- [ ] No client keys, signing material, or secrets committed
- [ ] Built native assets: `bash src/Kapusch.TikTokApisForAndroidComponents/Native/Android/build.sh`
- [ ] Packed NuGet: `dotnet pack src/Kapusch.TikTokApisForAndroidComponents/Kapusch.TikTokApisForAndroidComponents.csproj -c Release -o artifacts/nuget`
- [ ] Validated package: `scripts/validate-package.sh`
- [ ] Business package (if touched): built, packed and validated with `scripts/validate-business-package.sh`, sample built in Release
- [ ] Public tree scan: `python3 -I scripts/verify-public.py`
- [ ] Updated dependency locks, Gradle verification and notices if upstream inputs changed
- [ ] Updated docs if behavior or integration changed
