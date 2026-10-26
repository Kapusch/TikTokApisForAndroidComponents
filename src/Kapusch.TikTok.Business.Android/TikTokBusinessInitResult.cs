namespace Kapusch.TikTok.Business.Android;

/// <summary>Outcome of <see cref="TikTokBusinessSdk.InitializeAsync(TikTokBusinessConfig, CancellationToken)"/>.</summary>
/// <param name="IsSuccess">Whether the native SDK reported a successful initialization.</param>
/// <param name="ErrorCode">
/// A stable diagnostic when <paramref name="IsSuccess"/> is <see langword="false"/>. Wrapper codes are
/// listed in <see cref="TikTokBusinessInitErrorCodes"/>; native SDK failures use <c>tiktok_&lt;code&gt;</c>.
/// </param>
/// <param name="ErrorMessage">The native or wrapper message, for logs only.</param>
/// <param name="NativeErrorCode">The raw native SDK error code, when the SDK reported one.</param>
public sealed record TikTokBusinessInitResult(
	bool IsSuccess,
	string? ErrorCode = null,
	string? ErrorMessage = null,
	int? NativeErrorCode = null
)
{
	internal static TikTokBusinessInitResult Success { get; } = new(true);

	internal static TikTokBusinessInitResult Failure(string code, string? message = null, int? nativeCode = null) =>
		new(false, code, message, nativeCode);
}

/// <summary>Stable error codes produced by the wrapper itself.</summary>
public static class TikTokBusinessInitErrorCodes
{
	/// <summary><see cref="TikTokBusinessConfig.AppId"/> is not a valid Android package name.</summary>
	public const string InvalidAppId = "invalid_app_id";

	/// <summary><see cref="TikTokBusinessConfig.TikTokAppId"/> is not a comma-separated list of numeric ids.</summary>
	public const string InvalidTikTokAppId = "invalid_tiktok_app_id";

	/// <summary><see cref="TikTokBusinessConfig.AccessToken"/> is empty.</summary>
	public const string MissingAccessToken = "missing_access_token";

	/// <summary>No Android application context is available.</summary>
	public const string InvalidContext = "invalid_context";

	/// <summary>The native interop classes are not in the app (native assets disabled or stripped).</summary>
	public const string NativeUnavailable = "native_unavailable";

	/// <summary>The native layer threw while initializing.</summary>
	public const string NativeException = "native_exception";
}
