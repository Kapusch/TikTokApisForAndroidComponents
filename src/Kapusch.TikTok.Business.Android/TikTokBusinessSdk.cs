using System.Text.RegularExpressions;
using Android.OS;
using Kapusch.TikTok.Business.Android.Internal;

namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// .NET Android entry point for the TikTok Business SDK (App Events).
/// </summary>
/// <remarks>
/// <para>
/// Nothing native is loaded and nothing is sent until <see cref="InitializeAsync"/> is called.
/// Gate that call on the user's consent. Calls made before initialization are ignored.
/// </para>
/// <para>
/// The SDK supports one initialization per process: the first successful configuration wins.
/// Call <see cref="InitializeAsync"/> from the main thread when possible; the wrapper marshals
/// to the main thread otherwise, as the SDK requires.
/// </para>
/// </remarks>
public static partial class TikTokBusinessSdk
{
	/// <summary>The TikTok Business SDK for Android version bundled in this package.</summary>
	public const string BundledSdkVersion = "1.7.1";

	private static readonly object Gate = new();
	private static Task<TikTokBusinessInitResult>? _initialization;
	private static volatile bool _forwardingEnabled = true;

	/// <summary>Whether the native SDK reports that it is initialized.</summary>
	public static bool IsInitialized
	{
		get
		{
			if (Volatile.Read(ref _initialization) is null)
			{
				return false;
			}
			try
			{
				return NativeTikTokBusinessInterop.IsInitialized();
			}
			catch (Java.Lang.Throwable)
			{
				return false;
			}
		}
	}

	/// <summary>
	/// The native SDK version, read from the native library once it is available, otherwise
	/// <see cref="BundledSdkVersion"/>.
	/// </summary>
	public static string SdkVersion
	{
		get
		{
			if (Volatile.Read(ref _initialization) is null)
			{
				return BundledSdkVersion;
			}
			try
			{
				var version = NativeTikTokBusinessInterop.SdkVersion();
				return string.IsNullOrEmpty(version) ? BundledSdkVersion : version;
			}
			catch (Java.Lang.Throwable)
			{
				return BundledSdkVersion;
			}
		}
	}

	/// <summary>
	/// Whether manual calls are forwarded to the SDK. See <see cref="SetTrackingEnabled(bool)"/>.
	/// </summary>
	public static bool IsTrackingEnabled => _forwardingEnabled;

	/// <summary>
	/// Initializes the TikTok Business SDK and completes with the SDK's own completion callback.
	/// </summary>
	/// <remarks>
	/// Concurrent or repeated calls share the same native initialization. A failed initialization
	/// can be retried. Cancelling <paramref name="cancellationToken"/> only stops waiting: the
	/// native initialization keeps running.
	/// </remarks>
	public static Task<TikTokBusinessInitResult> InitializeAsync(
		TikTokBusinessConfig config,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(config);
		var invalid = Validate(config);
		if (invalid is not null)
		{
			return Task.FromResult(invalid);
		}

		Task<TikTokBusinessInitResult> initialization;
		TaskCompletionSource<TikTokBusinessInitResult>? owner = null;
		lock (Gate)
		{
			if (_initialization is null)
			{
				owner = new TaskCompletionSource<TikTokBusinessInitResult>(
					TaskCreationOptions.RunContinuationsAsynchronously
				);
				_initialization = owner.Task;
			}
			initialization = _initialization;
		}

		// Only the caller that created the shared task starts the native initialization, and it
		// does so outside the lock so a synchronous native result cannot re-enter it.
		if (owner is not null)
		{
			StartInitialization(config, owner);
		}
		return cancellationToken.CanBeCanceled ? initialization.WaitAsync(cancellationToken) : initialization;
	}

	/// <summary>
	/// Starts network delivery when the SDK was initialized with
	/// <see cref="TikTokBusinessConfig.DisableAutoStart"/>. Ignored before initialization.
	/// </summary>
	public static void StartTrack()
	{
		var initialization = Volatile.Read(ref _initialization);
		if (initialization is null)
		{
			return;
		}
		if (initialization.IsCompletedSuccessfully)
		{
			if (initialization.Result.IsSuccess)
			{
				NativeTikTokBusinessInterop.StartTrack();
			}
			return;
		}
		initialization.ContinueWith(
			static task =>
			{
				if (task.IsCompletedSuccessfully && task.Result.IsSuccess)
				{
					NativeTikTokBusinessInterop.StartTrack();
				}
			},
			CancellationToken.None,
			TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default
		);
	}

	/// <summary>
	/// Enables or disables forwarding of manual calls (events, identify, logout, flush).
	/// </summary>
	/// <remarks>
	/// <para>Enabling also calls <see cref="StartTrack"/>.</para>
	/// <para>
	/// Android limitation: the native SDK has no public API to pause delivery once started.
	/// Disabling stops the wrapper from forwarding new calls, but automatic events and events
	/// already queued by the SDK are still delivered. To honour a consent withdrawal fully,
	/// persist the choice and do not call <see cref="InitializeAsync"/> on the next launch.
	/// </para>
	/// </remarks>
	public static void SetTrackingEnabled(bool enabled)
	{
		_forwardingEnabled = enabled;
		if (enabled)
		{
			StartTrack();
		}
	}

	/// <summary>Tracks an event with optional standard properties and a deduplication id.</summary>
	/// <param name="eventName">A name from <see cref="TikTokBusinessEventNames"/>, or a custom name.</param>
	/// <param name="properties">
	/// Property values may be strings, booleans, numbers, <see cref="TikTokBusinessContent"/>,
	/// dictionaries or lists. Prefer <see cref="TikTokBusinessPropertyNames"/>: TikTok discards
	/// custom property keys. <see langword="null"/> values are omitted.
	/// </param>
	/// <param name="eventId">
	/// Deduplication id, for example the store transaction id. The SDK generates one when omitted.
	/// </param>
	public static void TrackEvent(
		string eventName,
		IReadOnlyDictionary<string, object>? properties = null,
		string? eventId = null
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
		var json = TikTokBusinessJson.Serialize(properties);
		if (!CanForward())
		{
			return;
		}
		NativeTikTokBusinessInterop.TrackEvent(eventName, json, string.IsNullOrWhiteSpace(eventId) ? null : eventId);
	}

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.StartTrial"/>.</summary>
	/// <param name="value">Amount actually charged for the trial start, usually 0.</param>
	/// <param name="currency">ISO 4217 code from <see cref="TikTokBusinessCurrencies"/>; required with <paramref name="value"/>.</param>
	/// <param name="eventId">Deduplication id, for example the store transaction id.</param>
	/// <param name="contentId">Product or plan identifier.</param>
	/// <param name="contentType">Content type, for example <c>subscription</c>.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="contents">Optional content items.</param>
	public static void TrackStartTrial(
		decimal? value = null,
		string? currency = null,
		string? eventId = null,
		string? contentId = null,
		string? contentType = null,
		string? description = null,
		IReadOnlyList<TikTokBusinessContent>? contents = null
	) =>
		TrackCommerce(
			TikTokBusinessEventNames.StartTrial,
			value,
			currency,
			eventId,
			contentId,
			contentType,
			description,
			contents
		);

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Subscribe"/>.</summary>
	/// <param name="value">Amount actually charged.</param>
	/// <param name="currency">ISO 4217 code from <see cref="TikTokBusinessCurrencies"/>; required with <paramref name="value"/>.</param>
	/// <param name="eventId">Deduplication id, for example the store transaction id.</param>
	/// <param name="contentId">Product or plan identifier.</param>
	/// <param name="contentType">Content type, for example <c>subscription</c>.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="contents">Optional content items.</param>
	public static void TrackSubscribe(
		decimal? value = null,
		string? currency = null,
		string? eventId = null,
		string? contentId = null,
		string? contentType = null,
		string? description = null,
		IReadOnlyList<TikTokBusinessContent>? contents = null
	) =>
		TrackCommerce(
			TikTokBusinessEventNames.Subscribe,
			value,
			currency,
			eventId,
			contentId,
			contentType,
			description,
			contents
		);

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Purchase"/>.</summary>
	/// <param name="value">Amount actually charged.</param>
	/// <param name="currency">ISO 4217 code from <see cref="TikTokBusinessCurrencies"/>; required with <paramref name="value"/>.</param>
	/// <param name="eventId">Deduplication id, for example the store order id.</param>
	/// <param name="contentId">Product identifier.</param>
	/// <param name="contentType">Content type, for example <c>product</c>.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="contents">Optional content items.</param>
	public static void TrackPurchase(
		decimal? value = null,
		string? currency = null,
		string? eventId = null,
		string? contentId = null,
		string? contentType = null,
		string? description = null,
		IReadOnlyList<TikTokBusinessContent>? contents = null
	) =>
		TrackCommerce(
			TikTokBusinessEventNames.Purchase,
			value,
			currency,
			eventId,
			contentId,
			contentType,
			description,
			contents
		);

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Registration"/>.</summary>
	public static void TrackRegistration(string? eventId = null) =>
		TrackEvent(TikTokBusinessEventNames.Registration, null, eventId);

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Login"/>.</summary>
	public static void TrackLogin(string? eventId = null) =>
		TrackEvent(TikTokBusinessEventNames.Login, null, eventId);

	/// <summary>
	/// Advanced matching: associates the following events with a user. The SDK hashes the
	/// values with SHA-256 on the device before sending them.
	/// </summary>
	public static void Identify(
		string externalId,
		string? externalUserName = null,
		string? phoneNumber = null,
		string? email = null
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
		if (!CanForward())
		{
			return;
		}
		NativeTikTokBusinessInterop.Identify(externalId, externalUserName, phoneNumber, email);
	}

	/// <summary>Clears the identity set by <see cref="Identify"/>.</summary>
	public static void Logout()
	{
		if (CanForward())
		{
			NativeTikTokBusinessInterop.Logout();
		}
	}

	/// <summary>Asks the SDK to send queued events now. This is not a delivery receipt.</summary>
	public static void Flush()
	{
		if (CanForward())
		{
			NativeTikTokBusinessInterop.Flush();
		}
	}

	private static void TrackCommerce(
		string eventName,
		decimal? value,
		string? currency,
		string? eventId,
		string? contentId,
		string? contentType,
		string? description,
		IReadOnlyList<TikTokBusinessContent>? contents
	)
	{
		if (currency is not null && !TikTokBusinessCurrencies.IsSupported(currency))
		{
			throw new ArgumentException(
				$"Currency '{currency}' is not supported by the TikTok Business SDK.",
				nameof(currency)
			);
		}
		if (value is not null && currency is null)
		{
			throw new ArgumentException("A currency is required when a value is set.", nameof(currency));
		}

		var properties = new Dictionary<string, object>(StringComparer.Ordinal);
		if (value is { } amount)
		{
			properties[TikTokBusinessPropertyNames.Value] = amount;
		}
		AddIfPresent(properties, TikTokBusinessPropertyNames.Currency, currency);
		AddIfPresent(properties, TikTokBusinessPropertyNames.ContentId, contentId);
		AddIfPresent(properties, TikTokBusinessPropertyNames.ContentType, contentType);
		AddIfPresent(properties, TikTokBusinessPropertyNames.Description, description);
		if (contents is { Count: > 0 })
		{
			properties[TikTokBusinessPropertyNames.Contents] = contents;
		}
		TrackEvent(eventName, properties, eventId);
	}

	private static void AddIfPresent(Dictionary<string, object> properties, string key, string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			properties[key] = value;
		}
	}

	private static bool CanForward() => _forwardingEnabled && Volatile.Read(ref _initialization) is not null;

	private static void StartInitialization(
		TikTokBusinessConfig config,
		TaskCompletionSource<TikTokBusinessInitResult> completion
	)
	{
		try
		{
			using var bundle = ToBundle(config);
			NativeTikTokBusinessInterop.Initialize(bundle, result => Complete(completion, result));
		}
		catch (Java.Lang.ClassNotFoundException error)
		{
			Complete(
				completion,
				TikTokBusinessInitResult.Failure(TikTokBusinessInitErrorCodes.NativeUnavailable, error.Message)
			);
		}
		catch (Java.Lang.NoClassDefFoundError error)
		{
			Complete(
				completion,
				TikTokBusinessInitResult.Failure(TikTokBusinessInitErrorCodes.NativeUnavailable, error.Message)
			);
		}
		catch (Java.Lang.Throwable error)
		{
			Complete(
				completion,
				TikTokBusinessInitResult.Failure(TikTokBusinessInitErrorCodes.NativeException, error.Message)
			);
		}
	}

	// A failure clears the shared task before completing it, so a caller that awaited the
	// failure can retry immediately and gets a fresh native initialization.
	private static void Complete(
		TaskCompletionSource<TikTokBusinessInitResult> completion,
		TikTokBusinessInitResult result
	)
	{
		if (!result.IsSuccess)
		{
			lock (Gate)
			{
				if (ReferenceEquals(_initialization, completion.Task))
				{
					_initialization = null;
				}
			}
		}
		completion.TrySetResult(result);
	}

	private static Bundle ToBundle(TikTokBusinessConfig config)
	{
		var bundle = new Bundle();
		bundle.PutString(NativeTikTokBusinessInterop.KeyAppId, config.AppId.Trim());
		bundle.PutString(NativeTikTokBusinessInterop.KeyTikTokAppId, NormalizeTikTokAppId(config.TikTokAppId));
		bundle.PutString(NativeTikTokBusinessInterop.KeyAccessToken, config.AccessToken.Trim());
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDebugMode, config.DebugMode);
		bundle.PutInt(NativeTikTokBusinessInterop.KeyLogLevel, (int)config.LogLevel);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisableAutoStart, config.DisableAutoStart);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisableAutomaticTracking, config.DisableAutomaticTracking);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisableInstallTracking, config.DisableInstallTracking);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisableLaunchTracking, config.DisableLaunchTracking);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisableRetentionTracking, config.DisableRetentionTracking);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisablePaymentTracking, config.DisablePaymentTracking);
		bundle.PutBoolean(
			NativeTikTokBusinessInterop.KeyDisableAdvertiserIdCollection,
			config.DisableAdvertiserIdCollection
		);
		bundle.PutBoolean(
			NativeTikTokBusinessInterop.KeyDisableEnhancedDataPostback,
			config.DisableEnhancedDataPostback
		);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDisableMonitor, config.DisableMonitor);
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyLimitedDataUse, config.LimitedDataUse);
		return bundle;
	}

	private static TikTokBusinessInitResult? Validate(TikTokBusinessConfig config)
	{
		if (string.IsNullOrWhiteSpace(config.AppId) || !AndroidPackageName().IsMatch(config.AppId.Trim()))
		{
			return TikTokBusinessInitResult.Failure(
				TikTokBusinessInitErrorCodes.InvalidAppId,
				"AppId must be the Android package name."
			);
		}
		if (
			string.IsNullOrWhiteSpace(config.TikTokAppId)
			|| !TikTokAppIdList().IsMatch(NormalizeTikTokAppId(config.TikTokAppId))
		)
		{
			return TikTokBusinessInitResult.Failure(
				TikTokBusinessInitErrorCodes.InvalidTikTokAppId,
				"TikTokAppId must be one or more comma-separated numeric ids."
			);
		}
		if (string.IsNullOrWhiteSpace(config.AccessToken))
		{
			return TikTokBusinessInitResult.Failure(
				TikTokBusinessInitErrorCodes.MissingAccessToken,
				"AccessToken (the Events Manager app secret) is required."
			);
		}
		return null;
	}

	private static string NormalizeTikTokAppId(string value) => value.Replace(" ", string.Empty, StringComparison.Ordinal);

	// Same patterns as com.tiktok.util.RegexUtil in the Android SDK.
	[GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$")]
	private static partial Regex AndroidPackageName();

	[GeneratedRegex(@"^(\d+,)*\d+$")]
	private static partial Regex TikTokAppIdList();
}
