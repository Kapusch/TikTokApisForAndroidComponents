# Kapusch.TikTok.Business.Android

.NET Android interop for the TikTok Business SDK (App Events) 1.7.1.

- Ships the checksum-locked upstream AAR unchanged plus a thin Kotlin facade.
  Consumer builds never download native artifacts.
- Nothing is loaded or sent until `TikTokBusinessSdk.InitializeAsync` is called:
  gate that call on the user's consent.
- Shared AndroidX and Kotlin classes come from managed NuGet dependencies.
  Google Play Billing and Install Referrer stay owned by the application.
- Requires Android API 23 or later (AndroidX lifecycle 2.10).
- Same API and result semantics as `Kapusch.TikTok.Business.iOS`, so shared code
  works on both platforms. `InitializeAsync` always completes within about 30 seconds.

```csharp
var result = await TikTokBusinessSdk.InitializeAsync(new TikTokBusinessConfig
{
	AppId = "com.example.app",
	TikTokAppIds = ["7000000000000000000"],
	AccessToken = secret,
	DisablePaymentTracking = true,
});

TikTokBusinessSdk.TrackStartTrial(value: 0m, currency: "EUR", eventId: transactionId);
```

Automatic in-app purchase tracking reports every store transaction as
`Purchase` at catalog price, including free-trial starts. Subscription apps
should set `DisablePaymentTracking` and send `StartTrial` / `Subscribe`
manually. See `Docs/Business/Integration.md` in the repository.
