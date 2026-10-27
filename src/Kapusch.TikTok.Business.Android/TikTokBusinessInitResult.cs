namespace Kapusch.TikTok.Business.Android;

/// <summary>Outcome of <see cref="TikTokBusinessSdk.InitializeAsync"/>.</summary>
public enum TikTokBusinessInitStatus
{
	/// <summary>The SDK is initialized and sends events.</summary>
	Started = 0,

	/// <summary>
	/// The SDK is initialized with <see cref="TikTokBusinessConfig.DisableAutoStart"/> and holds
	/// delivery until <see cref="TikTokBusinessSdk.StartTrack"/> is called.
	/// </summary>
	InitializedTrackingDisabled = 1,

	/// <summary>Initialization failed. See <see cref="TikTokBusinessInitResult.ErrorCode"/>.</summary>
	Failed = 2,
}

/// <summary>Result of <see cref="TikTokBusinessSdk.InitializeAsync"/>.</summary>
/// <param name="Status">Initialization outcome.</param>
/// <param name="ErrorCode">
/// <c>0</c> when there is no error. Otherwise the SDK error code (TikTok Business SDK for Android
/// 1.7.1 only reports <c>-2</c>, for an empty App ID or TikTok App ID) or one of the wrapper codes
/// declared on this type.
/// </param>
/// <param name="ErrorMessage">SDK or wrapper diagnostic message, for logs only.</param>
public sealed record TikTokBusinessInitResult(
	TikTokBusinessInitStatus Status,
	int ErrorCode = 0,
	string? ErrorMessage = null
)
{
	/// <summary>The native layer received an empty App ID, TikTok App ID or App Secret.</summary>
	public const int InvalidConfigurationErrorCode = 1001;

	/// <summary>
	/// The values do not match the SDK's own App ID / TikTok App ID patterns. The managed API
	/// validates them first and throws <see cref="ArgumentException"/>, so this indicates a direct
	/// native call.
	/// </summary>
	public const int ConfigurationRejectedErrorCode = 1002;

	/// <summary>Android only: no Android <c>Application</c> context is available.</summary>
	public const int InvalidContextErrorCode = 1101;

	/// <summary>
	/// Android only: the native interop classes are not in the app (native assets disabled with
	/// <c>DisableKapuschTikTokBusinessAndroidNativeAssets</c>, or stripped). Retrying does not help.
	/// </summary>
	public const int NativeUnavailableErrorCode = 1102;

	/// <summary>Android only: the native layer threw while configuring the SDK.</summary>
	public const int NativeExceptionErrorCode = 1103;

	/// <summary>
	/// Android only: an earlier native initialization in this process did not complete. The SDK
	/// cannot be initialized twice in a process, so every retry in the same process returns this code.
	/// </summary>
	public const int InitIncompleteErrorCode = 1104;

	/// <summary>
	/// Android only: the SDK did not report its initialization within 30 seconds (its init step
	/// swallows its own exceptions). If it finishes later, <see cref="TikTokBusinessSdk.IsInitialized"/>
	/// becomes <see langword="true"/> and a new <see cref="TikTokBusinessSdk.InitializeAsync"/> call
	/// reports the current state; otherwise that call returns <see cref="InitIncompleteErrorCode"/>.
	/// </summary>
	public const int InitTimeoutErrorCode = 1105;

	/// <summary><see langword="true"/> unless <see cref="Status"/> is <see cref="TikTokBusinessInitStatus.Failed"/>.</summary>
	public bool IsSuccess => Status != TikTokBusinessInitStatus.Failed;

	internal static TikTokBusinessInitResult Failure(int code, string? message) =>
		new(TikTokBusinessInitStatus.Failed, code, message);
}
