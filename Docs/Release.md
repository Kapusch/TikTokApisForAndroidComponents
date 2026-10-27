# Release

Each package is released independently by `.github/workflows/publish.yml`. Using
a single workflow keeps one NuGet Trusted Publishing policy for the repository.

| Package | Tag | Version taken from the tag |
|---|---|---|
| `Kapusch.TikTok.Android` | `vX.Y.Z` or `vX.Y.Z-rc.N` | `v1.0.1` → `1.0.1` |
| `Kapusch.TikTok.Business.Android` | `business-vX.Y.Z` or `business-vX.Y.Z-rc.N` | `business-v1.0.0-rc.1` → `1.0.0-rc.1` |

GitHub tag filters match the whole tag name, so `v*` never matches `business-v*`.
Business versions must be `X.Y.Z` or `X.Y.Z-rc.N`.

## Rules

- Release only from commits reachable from `origin/master`. The workflow refuses
  anything else.
- Versions are immutable. The workflow refuses to publish a version that already
  exists on NuGet.org; publish a new version instead. Never move or delete a
  published tag.
- Start a new package line with release candidates (`-rc.N`) and promote a
  qualified candidate by tagging the same commit without the suffix.

## Channels

- **Tag push:** packs, validates and publishes the matching package to NuGet.org
  through Trusted Publishing (`NuGet/login`, OIDC).
- **Manual run with `manual_version`:** publishes that version of the selected
  `package` (`share` or `business`) to NuGet.org, with the same master-reachability check.
- **Manual run without a version:** publishes `X.Y.Z-preview.<run>.<sha>` of the
  selected package to GitHub Packages.

Every run uploads the packed nupkg as a workflow artifact. Packs set
`RepositoryCommit` to the released commit.

## Release a Business version

```sh
git checkout master && git pull --ff-only
git tag -a business-v1.0.0-rc.1 -m "Kapusch.TikTok.Business.Android 1.0.0-rc.1"
git push origin business-v1.0.0-rc.1
```

Before promoting a candidate, qualify it on a device: Events Manager **Test
events** must show `InstallApp`, `LaunchAPP` and the manual events, and no
automatic `Purchase` when `DisablePaymentTracking` is set.
