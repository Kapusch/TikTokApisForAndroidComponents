namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// Configuration passed to <see cref="TikTokBusinessSdk.InitializeAsync"/>.
/// Values come from TikTok Events Manager (Ads Manager, Tools, Events Manager, your app, Settings).
/// </summary>
/// <remarks>
/// Same shape as <c>Kapusch.TikTok.Business.iOS</c>, so shared code can configure both platforms
/// with one object. Every opt-out defaults to <see langword="false"/>, which matches the native
/// SDK defaults. Members marked "iOS only" are accepted and ignored on Android.
/// </remarks>
public sealed record TikTokBusinessConfig
{
	/// <summary>The store identifier of the app. On Android this is the application package name.</summary>
	public required string AppId { get; init; }

	/// <summary>
	/// One or several TikTok App IDs (one per ad account). Each ID is numeric. The SDK does not
	/// initialize when an ID does not belong to <see cref="AppId"/> in Events Manager.
	/// </summary>
	public required IReadOnlyList<string> TikTokAppIds { get; init; }

	/// <summary>
	/// The App Secret shown in Events Manager. The TikTok SDKs name this value <c>accessToken</c>.
	/// Never commit it to source control.
	/// </summary>
	public required string AccessToken { get; init; }

	/// <summary>Enables the SDK debug mode used by Events Manager "Test events". Never enable it in production.</summary>
	public bool DebugMode { get; init; }

	/// <summary>
	/// SDK log level. <see langword="null"/> keeps the SDK default (no logs). The Android SDK only has
	/// <c>NONE</c>, <c>INFO</c>, <c>WARN</c> and <c>DEBUG</c>, ordered in that sequence, and logs errors
	/// at <c>INFO</c>: <see cref="TikTokBusinessLogLevel.Error"/> maps to <c>INFO</c>,
	/// <see cref="TikTokBusinessLogLevel.Warn"/> and <see cref="TikTokBusinessLogLevel.Info"/> to
	/// <c>WARN</c>, <see cref="TikTokBusinessLogLevel.Debug"/> and <see cref="TikTokBusinessLogLevel.Verbose"/>
	/// to <c>DEBUG</c>.
	/// </summary>
	public TikTokBusinessLogLevel? LogLevel { get; init; }

	/// <summary>
	/// Initializes the SDK but holds network delivery until <see cref="TikTokBusinessSdk.StartTrack"/>.
	/// The SDK still requests its remote configuration during initialization, so this is not a
	/// consent gate: call <see cref="TikTokBusinessSdk.InitializeAsync"/> only after consent instead.
	/// Maps to <c>TTConfig.disableAutoStart()</c>.
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

	/// <summary>
	/// Accepted for API symmetry with iOS, where it is deprecated upstream and has no effect either.
	/// Ignored on Android.
	/// </summary>
	public bool DisableAppTrackingDialog { get; init; }

	/// <summary>iOS only: stops the SDK from updating SKAdNetwork conversion values. Ignored on Android.</summary>
	public bool DisableSKAdNetworkSupport { get; init; }

	/// <summary>
	/// Stops the SDK from reading the Google advertising id (GAID); maps to
	/// <c>disableAdvertiserIDCollection()</c>. No effect on iOS.
	/// </summary>
	public bool DisableAdvertiserIdCollection { get; init; }

	/// <summary>Disables Enhanced Data Postback (automatic UI interaction capture).</summary>
	public bool DisableEnhancedDataPostback { get; init; }

	/// <summary>Enables Limited Data Use mode (for example for US state privacy laws).</summary>
	public bool LimitedDataUse { get; init; }

	/// <summary>iOS only: delays the first flush for an App Tracking Transparency decision. Ignored on Android.</summary>
	public TimeSpan? AttAuthorizationDelay { get; init; }

	/// <summary>iOS only: custom user agent. Ignored on Android.</summary>
	public string? CustomUserAgent { get; init; }

	/// <summary>Marks the device as low-performance so the SDK reduces optional work (<c>setIsLowPerformanceDevice</c>).</summary>
	public bool LowPerformanceDevice { get; init; }

	/// <summary>Android only: disables the SDK's own health and performance metrics.</summary>
	public bool DisableMonitor { get; init; }
}

/// <summary>TikTok SDK log level, with the same values as the iOS package.</summary>
public enum TikTokBusinessLogLevel
{
	/// <summary>No SDK logs.</summary>
	None = 0,

	/// <summary>Errors only. Android maps this to <c>INFO</c>, the level its SDK logs errors at.</summary>
	Error = 1,

	/// <summary>Warnings and errors.</summary>
	Warn = 2,

	/// <summary>Informational messages. Android maps this to <c>WARN</c>, which also shows <c>INFO</c>.</summary>
	Info = 3,

	/// <summary>Debug messages.</summary>
	Debug = 4,

	/// <summary>Everything. Android maps this to <c>DEBUG</c>.</summary>
	Verbose = 5,
}
