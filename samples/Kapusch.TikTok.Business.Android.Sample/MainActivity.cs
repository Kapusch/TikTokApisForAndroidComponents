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

		// InitializeAsync validates the values first and is bounded: it always completes within
		// about 30 seconds.
		TikTokBusinessInitResult result;
		try
		{
			result = await TikTokBusinessSdk.InitializeAsync(config);
		}
		catch (ArgumentException error)
		{
			Write($"Invalid configuration in Assets/{LocalConfigAsset}: {error.Message}");
			return;
		}
		Write(
			$"Init status={result.Status} code={result.ErrorCode} message={result.ErrorMessage} "
				+ $"initialized={TikTokBusinessSdk.IsInitialized} tracking={TikTokBusinessSdk.IsTrackingEnabled} "
				+ $"sdk={TikTokBusinessSdk.SdkVersion} testEventCode={TikTokBusinessSdk.TestEventCode}"
		);
		if (!result.IsSuccess)
		{
			return;
		}

		// Manual events with a store transaction id as event_id, as recommended when automatic
		// in-app purchase tracking is disabled.
		var tracked =
			TikTokBusinessSdk.TrackStartTrial(value: 0m, currency: "EUR", eventId: "sample-trial-0001", contentId: "sample.monthly", contentType: "subscription")
			& TikTokBusinessSdk.TrackSubscribe(value: 4.99m, currency: "EUR", eventId: "sample-sub-0001", contentId: "sample.monthly", contentType: "subscription")
			& TikTokBusinessSdk.TrackEvent(
				TikTokBusinessEventNames.ViewContent,
				new Dictionary<string, object>
				{
					[TikTokBusinessPropertyNames.ContentId] = "sample.paywall",
					[TikTokBusinessPropertyNames.Contents] = new[] { new TikTokBusinessContent(ContentId: "sample.monthly", Price: 4.99m, Quantity: 1) },
				}
			)
			& TikTokBusinessSdk.TrackRegistration();
		var flushed = TikTokBusinessSdk.Flush();
		Write($"Tracked StartTrial, Subscribe, ViewContent and Registration: accepted={tracked}; flush requested={flushed}.");
	}

	private TikTokBusinessConfig? LoadConfig()
	{
		try
		{
			using var stream = Assets!.Open(LocalConfigAsset);
			using var document = JsonDocument.Parse(stream);
			var root = document.RootElement;
			return new TikTokBusinessConfig
			{
				AppId = root.GetProperty("appId").GetString() ?? PackageName!,
				TikTokAppIds = (root.GetProperty("tikTokAppId").GetString() ?? string.Empty).Split(
					',',
					StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
				),
				AccessToken = root.GetProperty("accessToken").GetString() ?? string.Empty,
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
