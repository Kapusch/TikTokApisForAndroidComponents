namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// Configuration passed to <see cref="TikTokBusinessSdk.InitializeAsync(TikTokBusinessConfig, CancellationToken)"/>.
/// </summary>
/// <remarks>
/// Every opt-out defaults to <see langword="false"/>, which matches the native SDK defaults.
/// Properties marked "iOS only" exist so shared code can configure both platforms with one
/// object; they are ignored on Android.
/// </remarks>
public sealed class TikTokBusinessConfig
{
	/// <summary>Creates a configuration with the three identifiers the SDK requires.</summary>
	/// <param name="appId">The store app id. On Android this is the application package name.</param>
	/// <param name="tikTokAppId">One TikTok App ID, or several separated by commas.</param>
	/// <param name="accessToken">The app secret generated in TikTok Events Manager.</param>
	public TikTokBusinessConfig(string appId, string tikTokAppId, string accessToken)
	{
		AppId = appId;
		TikTokAppId = tikTokAppId;
		AccessToken = accessToken;
	}

	/// <summary>Creates a configuration that reports to several TikTok App IDs.</summary>
	public TikTokBusinessConfig(string appId, IEnumerable<string> tikTokAppIds, string accessToken)
		: this(appId, string.Join(',', tikTokAppIds ?? throw new ArgumentNullException(nameof(tikTokAppIds))), accessToken)
	{
	}

	/// <summary>The store app id. On Android this is the application package name.</summary>
	public string AppId { get; }

	/// <summary>
	/// One TikTok App ID, or several separated by commas without a trailing comma.
	/// The SDK does not initialize when the ids do not match <see cref="AppId"/> in Events Manager.
	/// </summary>
	public string TikTokAppId { get; }

	/// <summary>
	/// The app secret generated in TikTok Events Manager. The native SDKs name this value
	/// <c>accessToken</c>. Never commit it to source control.
	/// </summary>
	public string AccessToken { get; }

	/// <summary>Enables the SDK debug mode used by Events Manager "Test events".</summary>
	public bool DebugMode { get; init; }

	/// <summary>Native SDK log verbosity. Defaults to <see cref="TikTokBusinessLogLevel.None"/>.</summary>
	public TikTokBusinessLogLevel LogLevel { get; init; } = TikTokBusinessLogLevel.None;

	/// <summary>
	/// Initializes the SDK but holds network delivery until <see cref="TikTokBusinessSdk.StartTrack"/>.
	/// Note that the SDK still requests its remote configuration during initialization.
	/// </summary>
	public bool DisableAutoStart { get; init; }

	/// <summary>
	/// Disables the automatic lifecycle events (InstallApp, LaunchAPP, 2Dretention).
	/// On Android this does not disable automatic in-app purchase tracking: use
	/// <see cref="DisablePaymentTracking"/> as well.
	/// </summary>
	public bool DisableAutomaticTracking { get; init; }

	/// <summary>Disables the automatic <c>InstallApp</c> event.</summary>
	public bool DisableInstallTracking { get; init; }

	/// <summary>Disables the automatic <c>LaunchAPP</c> event.</summary>
	public bool DisableLaunchTracking { get; init; }

	/// <summary>Disables the automatic <c>2Dretention</c> event.</summary>
	public bool DisableRetentionTracking { get; init; }

	/// <summary>
	/// Asks TikTok to disable automatic in-app purchase tracking. The native SDK sends this
	/// preference to TikTok's configuration endpoint, which makes the final decision, so verify
	/// in Events Manager that no automatic <c>Purchase</c> arrives.
	/// </summary>
	public bool DisablePaymentTracking { get; init; }

	/// <summary>iOS only (deprecated upstream since 1.4.0). Ignored on Android.</summary>
	public bool DisableAppTrackingDialog { get; init; }

	/// <summary>iOS only: stops the SDK from updating SKAdNetwork conversion values. Ignored on Android.</summary>
	public bool DisableSKAdNetworkSupport { get; init; }

	/// <summary>Android: stops the SDK from reading the Google advertising id (GAID).</summary>
	public bool DisableAdvertiserIdCollection { get; init; }

	/// <summary>Disables Enhanced Data Postback (automatic UI interaction capture).</summary>
	public bool DisableEnhancedDataPostback { get; init; }

	/// <summary>Android: disables the SDK's own health and performance metrics.</summary>
	public bool DisableMonitor { get; init; }

	/// <summary>Enables Limited Data Use mode (for example for US state privacy laws).</summary>
	public bool LimitedDataUse { get; init; }
}

/// <summary>Native TikTok Business SDK log verbosity.</summary>
public enum TikTokBusinessLogLevel
{
	/// <summary>No native logs.</summary>
	None = 0,

	/// <summary>Informational logs.</summary>
	Info = 1,

	/// <summary>Warnings.</summary>
	Warn = 2,

	/// <summary>Debug output.</summary>
	Debug = 3,
}
