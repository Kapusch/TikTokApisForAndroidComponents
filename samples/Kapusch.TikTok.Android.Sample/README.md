# Minimal integration sample

Provide `tiktokClientKey` as a local manifest placeholder and configure a `FileProvider` whose authority is `${applicationId}.fileprovider`. Do not commit a real client key.

Start the packaged callback activity with two readable JPEG paths:

```csharp
var intent = new Intent();
intent.SetClassName(PackageName, AndroidTikTokInterop.ShareActivityClassName);
intent.PutExtra(
	AndroidTikTokInterop.ExtraImagePaths,
	new[] { firstImagePath, secondImagePath }
);
StartActivityForResult(intent, requestCode: 6401);
```

Read the stable result values in `OnActivityResult`:

```csharp
var status = data?.GetStringExtra(AndroidTikTokInterop.ExtraStatus);
var error = data?.GetStringExtra(AndroidTikTokInterop.ExtraErrorCode);
```

The runtime handoff requires an approved TikTok application and a physical device with TikTok installed.
