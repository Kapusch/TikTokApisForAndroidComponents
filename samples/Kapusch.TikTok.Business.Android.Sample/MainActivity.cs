using System.Text.Json;
using Android.App;
using Android.OS;
using Android.Util;
using Android.Widget;
using Kapusch.TikTok.Business.Android;

namespace Kapusch.TikTok.Business.Android.Sample;

/// <summary>
/// Consent-gated integration example. Nothing reaches the TikTok SDK until the user taps
/// "Accept" (or the activity is started with the <c>auto_consent</c> boolean extra for
/// scripted smoke tests).
/// </summary>
[Activity(Label = "TikTok Business Sample", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
	private const string LogTag = "KtkBusinessSample";
	private const string LocalConfigAsset = "TikTokBusiness.local.json";
	private TextView? _log;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
		var accept = new Button(this) { Text = "Accept ad measurement" };
		var decline = new Button(this) { Text = "Decline" };
		_log = new TextView(this);
		layout.AddView(accept);
		layout.AddView(decline);
		layout.AddView(_log);
		SetContentView(layout);

		accept.Click += async (_, _) => await OnConsentGrantedAsync();
		decline.Click += (_, _) => Write("Consent declined: the SDK stays uninitialized.");

		Write($"Bundled TikTok Business SDK {TikTokBusinessSdk.BundledSdkVersion}; initialized={TikTokBusinessSdk.IsInitialized}");
		if (Intent?.GetBooleanExtra("auto_consent", false) == true)
		{
			_ = OnConsentGrantedAsync();
		}
	}

	private async Task OnConsentGrantedAsync()
	{
		var config = LoadConfig();
		if (config is null)
		{
			Write($"Missing Assets/{LocalConfigAsset}. Copy TikTokBusiness.template.json and fill in your ids.");
			return;
		}

		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		TikTokBusinessInitResult result;
		try
		{
			result = await TikTokBusinessSdk.InitializeAsync(config, timeout.Token);
		}
		catch (System.OperationCanceledException)
		{
			Write("Initialization timed out.");
			return;
		}
		Write(
			$"Init success={result.IsSuccess} code={result.ErrorCode} native={result.NativeErrorCode} "
				+ $"initialized={TikTokBusinessSdk.IsInitialized} sdk={TikTokBusinessSdk.SdkVersion}"
		);
		if (!result.IsSuccess)
		{
			return;
		}

		// Manual events with a store transaction id as event_id, as recommended when automatic
		// in-app purchase tracking is disabled.
		TikTokBusinessSdk.TrackStartTrial(value: 0m, currency: "EUR", eventId: "sample-trial-0001", contentId: "sample.monthly", contentType: "subscription");
		TikTokBusinessSdk.TrackSubscribe(value: 4.99m, currency: "EUR", eventId: "sample-sub-0001", contentId: "sample.monthly", contentType: "subscription");
		TikTokBusinessSdk.TrackEvent(
			TikTokBusinessEventNames.ViewContent,
			new Dictionary<string, object>
			{
				[TikTokBusinessPropertyNames.ContentId] = "sample.paywall",
				[TikTokBusinessPropertyNames.Contents] = new[] { new TikTokBusinessContent(ContentId: "sample.monthly", Price: 4.99m, Quantity: 1) },
			}
		);
		TikTokBusinessSdk.TrackRegistration();
		TikTokBusinessSdk.Flush();
		Write("Tracked StartTrial, Subscribe, ViewContent and Registration; flush requested.");
	}

	private TikTokBusinessConfig? LoadConfig()
	{
		try
		{
			using var stream = Assets!.Open(LocalConfigAsset);
			using var document = JsonDocument.Parse(stream);
			var root = document.RootElement;
			return new TikTokBusinessConfig(
				root.GetProperty("appId").GetString() ?? PackageName!,
				root.GetProperty("tikTokAppId").GetString() ?? string.Empty,
				root.GetProperty("accessToken").GetString() ?? string.Empty
			)
			{
				DebugMode = root.TryGetProperty("debugMode", out var debug) && debug.GetBoolean(),
				LogLevel = TikTokBusinessLogLevel.Debug,
				// Recommended for subscription apps: report trials and subscriptions manually.
				DisablePaymentTracking = true,
				DisableEnhancedDataPostback = true,
			};
		}
		catch (Java.IO.IOException)
		{
			return null;
		}
	}

	private void Write(string message)
	{
		Log.Info(LogTag, message);
		RunOnUiThread(() => _log?.Append(message + "\n"));
	}
}
