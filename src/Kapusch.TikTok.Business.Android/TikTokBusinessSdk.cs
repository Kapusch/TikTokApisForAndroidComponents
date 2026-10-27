using System.Text.RegularExpressions;
using Android.OS;
using Kapusch.TikTok.Business.Android.Internal;

namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// .NET facade over the TikTok Business SDK (App Events SDK) for Android. Same members and
/// semantics as <c>Kapusch.TikTok.Business.iOS</c>; Android differences are documented per member
/// and in <c>Docs/Business/Integration.md</c>.
/// </summary>
/// <remarks>
/// Nothing native is loaded and nothing is sent until <see cref="InitializeAsync"/> is called: every
/// other member returns <see langword="false"/> (or <see langword="null"/>) before a successful
/// initialization. Gate <see cref="InitializeAsync"/> on the user's consent.
/// <para>
/// Only <see cref="InitializeAsync"/> switches to the main thread. Every other member calls the SDK
/// synchronously on the calling thread, so calls made in sequence take effect in that order: an event
/// tracked right after <see cref="Identify"/> carries the identity, and one tracked after
/// <see cref="Logout"/> does not. Android only: the SDK copies the identity when its event thread
/// builds a queued event, so an event tracked right before <see cref="Identify"/> or
/// <see cref="Logout"/> may carry the newer identity.
/// </para>
/// </remarks>
public static partial class TikTokBusinessSdk
{
	/// <summary>Android only: the TikTok Business SDK for Android version bundled in this package.</summary>
	public const string BundledSdkVersion = "1.7.1";

	// Upper bound for the SDK to report its initialization. Its init step swallows its own
	// exceptions without calling back, so the wait must be bounded here.
	private static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(30);

	private static readonly object Gate = new();
	private static Task<TikTokBusinessInitResult>? _initialization;
	private static volatile bool _nativeEntered;
	private static volatile bool _ready;
	private static volatile bool _forwardingEnabled = true;

	/// <summary>
	/// Initializes the TikTok Business SDK on the main thread and completes with the SDK result.
	/// The SDK is a process-wide singleton: later calls return the current state without reconfiguring it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Concurrent calls share one native initialization, which always completes within about
	/// 30 seconds (<see cref="TikTokBusinessInitResult.InitTimeoutErrorCode"/>). The SDK can be
	/// entered only once per process. A failure before it was entered
	/// (<see cref="TikTokBusinessInitResult.NativeUnavailableErrorCode"/>,
	/// <see cref="TikTokBusinessInitResult.InvalidContextErrorCode"/>) lets a retry run the checks again.
	/// Once it was entered, a retry returns the current state when a timed-out initialization finished
	/// late, and <see cref="TikTokBusinessInitResult.InitIncompleteErrorCode"/> otherwise.
	/// </para>
	/// <para>
	/// With <see cref="TikTokBusinessConfig.DisableAutoStart"/> the result is
	/// <see cref="TikTokBusinessInitStatus.InitializedTrackingDisabled"/> with <c>ErrorCode</c> <c>0</c>.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentException">A required value is missing or malformed.</exception>
	public static Task<TikTokBusinessInitResult> InitializeAsync(TikTokBusinessConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);
		Validate(config);

		Task<TikTokBusinessInitResult> initialization;
		TaskCompletionSource<TikTokBusinessInitResult>? owner = null;
		lock (Gate)
		{
			if (Ready())
			{
				return Task.FromResult(new TikTokBusinessInitResult(CurrentStatus()));
			}
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
		return initialization;
	}

	/// <summary>
	/// Starts sending events after an initialization with <see cref="TikTokBusinessConfig.DisableAutoStart"/>
	/// (<c>startTrack()</c> on Android, which also flushes). Same as <c>SetTrackingEnabled(true)</c>.
	/// </summary>
	/// <returns><see langword="false"/> when the SDK is not initialized.</returns>
	public static bool StartTrack()
	{
		if (!Ready())
		{
			return false;
		}
		_forwardingEnabled = true;
		return Native(NativeTikTokBusinessInterop.StartTrack);
	}

	/// <summary>Enables or pauses tracking.</summary>
	/// <remarks>
	/// Android limitation: the SDK has no public API to pause delivery once it has started.
	/// Disabling stops this wrapper from forwarding <c>Track*</c>, <see cref="Identify"/> and
	/// <see cref="Flush"/> calls, which then return <see langword="false"/> and are dropped, not queued
	/// as on iOS. Automatic events and events the SDK already holds are still sent until the process
	/// ends. On a consent withdrawal also call <see cref="Logout"/>, persist the choice and do not call
	/// <see cref="InitializeAsync"/> on the next launch. Enabling calls <see cref="StartTrack"/>.
	/// </remarks>
	/// <returns><see langword="false"/> when the SDK is not initialized.</returns>
	public static bool SetTrackingEnabled(bool enabled)
	{
		if (enabled)
		{
			return StartTrack();
		}
		if (!Ready())
		{
			return false;
		}
		_forwardingEnabled = false;
		return true;
	}

	/// <summary>Tracks an event.</summary>
	/// <param name="eventName">A <see cref="TikTokBusinessEventNames"/> value or a custom name.</param>
	/// <param name="properties">
	/// Event properties, preferably <see cref="TikTokBusinessPropertyNames"/> keys. Values may be strings,
	/// booleans, numbers, nested dictionaries, lists or <see cref="TikTokBusinessContent"/>.
	/// </param>
	/// <param name="eventId">
	/// Optional deduplication identifier (for example a store transaction identifier). The SDK generates one when omitted.
	/// </param>
	/// <returns><see langword="false"/> when the SDK is not initialized or tracking is disabled.</returns>
	public static bool TrackEvent(
		string eventName,
		IReadOnlyDictionary<string, object>? properties = null,
		string? eventId = null
	)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
		var json = TikTokBusinessJson.WriteProperties(properties);
		return CanForward() && Native(() => NativeTikTokBusinessInterop.TrackEvent(eventName, json, NullIfEmpty(eventId)));
	}

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.StartTrial"/>.</summary>
	public static bool TrackStartTrial(
		decimal? value = null,
		string? currency = null,
		string? eventId = null,
		string? contentId = null,
		string? contentType = null
	) => TrackEvent(TikTokBusinessEventNames.StartTrial, CommerceProperties(value, currency, contentId, contentType), eventId);

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Subscribe"/>.</summary>
	public static bool TrackSubscribe(
		decimal? value = null,
		string? currency = null,
		string? eventId = null,
		string? contentId = null,
		string? contentType = null
	) => TrackEvent(TikTokBusinessEventNames.Subscribe, CommerceProperties(value, currency, contentId, contentType), eventId);

	/// <summary>
	/// Tracks <see cref="TikTokBusinessEventNames.Purchase"/>, the App Events equivalent of a completed payment
	/// (the app SDK has no <c>CompletePayment</c> event).
	/// </summary>
	public static bool TrackPurchase(
		decimal? value = null,
		string? currency = null,
		string? eventId = null,
		string? contentId = null,
		string? contentType = null,
		IReadOnlyList<TikTokBusinessContent>? contents = null,
		string? description = null
	)
	{
		var properties = CommerceProperties(value, currency, contentId, contentType);
		if (contents is { Count: > 0 })
		{
			properties[TikTokBusinessPropertyNames.Contents] = contents;
		}
		if (!string.IsNullOrEmpty(description))
		{
			properties[TikTokBusinessPropertyNames.Description] = description;
		}
		return TrackEvent(TikTokBusinessEventNames.Purchase, properties, eventId);
	}

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Registration"/>.</summary>
	public static bool TrackRegistration(string? eventId = null) =>
		TrackEvent(TikTokBusinessEventNames.Registration, null, eventId);

	/// <summary>Tracks <see cref="TikTokBusinessEventNames.Login"/>.</summary>
	public static bool TrackLogin(string? eventId = null) =>
		TrackEvent(TikTokBusinessEventNames.Login, null, eventId);

	/// <summary>
	/// Advanced matching. The SDK hashes the values (SHA-256) on the device before sending them.
	/// The SDK keeps the first identity until <see cref="Logout"/> is called.
	/// </summary>
	/// <returns>
	/// <see langword="false"/> when the SDK is not initialized, tracking is disabled or every value is empty.
	/// </returns>
	public static bool Identify(
		string? externalId,
		string? externalUserName = null,
		string? phoneNumber = null,
		string? email = null
	) =>
		CanForward()
		&& Native(
			() =>
				NativeTikTokBusinessInterop.Identify(
					NullIfEmpty(externalId),
					NullIfEmpty(externalUserName),
					NullIfEmpty(phoneNumber),
					NullIfEmpty(email)
				)
		);

	/// <summary>
	/// Clears the identity set by <see cref="Identify"/> and resets the SDK's anonymous user data.
	/// Works while tracking is disabled.
	/// </summary>
	/// <returns><see langword="false"/> when the SDK is not initialized.</returns>
	public static bool Logout() => Ready() && Native(NativeTikTokBusinessInterop.Logout);

	/// <summary>Asks the SDK to send queued events now. This is not a delivery receipt.</summary>
	/// <returns><see langword="false"/> when the SDK is not initialized or tracking is disabled.</returns>
	public static bool Flush() => CanForward() && Native(NativeTikTokBusinessInterop.Flush);

	/// <summary>Replaces the App Secret used by the SDK (for example after a rotation in Events Manager).</summary>
	/// <returns><see langword="false"/> when the SDK is not initialized.</returns>
	public static bool UpdateAccessToken(string accessToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
		return Ready() && Native(() => NativeTikTokBusinessInterop.UpdateAccessToken(accessToken.Trim()));
	}

	/// <summary>
	/// <see langword="true"/> once <see cref="InitializeAsync"/> initialized the SDK in this process,
	/// including the deferred-start state.
	/// </summary>
	public static bool IsInitialized => Ready();

	/// <summary>
	/// <see langword="true"/> when the SDK is initialized and allowed to send events. On Android this is
	/// also <see langword="false"/> after <c>SetTrackingEnabled(false)</c>.
	/// </summary>
	public static bool IsTrackingEnabled =>
		Ready() && _forwardingEnabled && Native(NativeTikTokBusinessInterop.IsTrackingEnabled);

	/// <summary>
	/// Version of the bundled TikTok Business SDK, for example <c>1.7.1</c>. Read from the native
	/// library once it is loaded; <see cref="BundledSdkVersion"/> before that.
	/// </summary>
	public static string SdkVersion
	{
		get
		{
			if (!_nativeEntered)
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
	/// Test event code to enter in Events Manager, Test Events. Only available when the SDK runs with
	/// <see cref="TikTokBusinessConfig.DebugMode"/>; <see langword="null"/> otherwise.
	/// </summary>
	public static string? TestEventCode
	{
		get
		{
			if (!Ready())
			{
				return null;
			}
			try
			{
				return NativeTikTokBusinessInterop.TestEventCode();
			}
			catch (Java.Lang.Throwable)
			{
				return null;
			}
		}
	}

	private static TikTokBusinessInitStatus CurrentStatus() =>
		IsTrackingEnabled ? TikTokBusinessInitStatus.Started : TikTokBusinessInitStatus.InitializedTrackingDisabled;

	private static bool CanForward() => _forwardingEnabled && Ready();

	// Whether the SDK finished initializing in this process. A success that arrives after the
	// wait timed out is picked up here, so the state never depends on who observed it first.
	private static bool Ready()
	{
		if (_ready)
		{
			return true;
		}
		if (!_nativeEntered || !Native(NativeTikTokBusinessInterop.IsInitialized))
		{
			return false;
		}
		_ready = true;
		return true;
	}

	// Measurement calls are fire-and-forget: a JNI failure is reported as false, never thrown.
	private static bool Native(Func<bool> call)
	{
		try
		{
			return call();
		}
		catch (Java.Lang.Throwable)
		{
			return false;
		}
	}

	private static Dictionary<string, object> CommerceProperties(
		decimal? value,
		string? currency,
		string? contentId,
		string? contentType
	)
	{
		var properties = new Dictionary<string, object>(StringComparer.Ordinal);
		if (value is { } amount)
		{
			if (amount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "TikTok event values must not be negative.");
			}
			if (string.IsNullOrEmpty(currency))
			{
				throw new ArgumentException("A currency is required when a value is provided.", nameof(currency));
			}
			// TTContentsEvent.Builder.setValue(double) sends a number on Android.
			properties[TikTokBusinessPropertyNames.Value] = amount;
		}
		if (!string.IsNullOrEmpty(currency))
		{
			if (!CurrencyPattern().IsMatch(currency))
			{
				throw new ArgumentException("Currency must be an ISO 4217 code such as USD or EUR.", nameof(currency));
			}
			properties[TikTokBusinessPropertyNames.Currency] = currency;
		}
		if (!string.IsNullOrEmpty(contentId))
		{
			properties[TikTokBusinessPropertyNames.ContentId] = contentId;
		}
		if (!string.IsNullOrEmpty(contentType))
		{
			properties[TikTokBusinessPropertyNames.ContentType] = contentType;
		}
		return properties;
	}

	private static void StartInitialization(
		TikTokBusinessConfig config,
		TaskCompletionSource<TikTokBusinessInitResult> completion
	)
	{
		_nativeEntered = true;
		var timeout = new Timer(
			static state =>
			{
				var pending = (TaskCompletionSource<TikTokBusinessInitResult>)state!;
				Complete(
					pending,
					TikTokBusinessInitResult.Failure(
						TikTokBusinessInitResult.InitTimeoutErrorCode,
						"The TikTok Business SDK did not report its initialization in time."
					)
				);
			},
			completion,
			InitTimeout,
			Timeout.InfiniteTimeSpan
		);
		completion.Task.ContinueWith(
			static (_, state) => ((Timer)state!).Dispose(),
			timeout,
			CancellationToken.None,
			TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default
		);

		try
		{
			using var bundle = ToBundle(config);
			NativeTikTokBusinessInterop.Initialize(bundle, result => Complete(completion, result));
		}
		catch (Java.Lang.ClassNotFoundException error)
		{
			// Nothing native ran: keep every other member away from the missing classes.
			_nativeEntered = false;
			Complete(completion, TikTokBusinessInitResult.Failure(TikTokBusinessInitResult.NativeUnavailableErrorCode, error.Message));
		}
		catch (Java.Lang.NoClassDefFoundError error)
		{
			// Nothing native ran: keep every other member away from the missing classes.
			_nativeEntered = false;
			Complete(completion, TikTokBusinessInitResult.Failure(TikTokBusinessInitResult.NativeUnavailableErrorCode, error.Message));
		}
		catch (Java.Lang.Throwable error)
		{
			Complete(completion, TikTokBusinessInitResult.Failure(TikTokBusinessInitResult.NativeExceptionErrorCode, error.Message));
		}
	}

	// Called by the native callback, the timeout or a synchronous failure; the first result wins.
	// A success always latches the ready state, even after a timeout was reported. A failure
	// clears the shared task before completing it, so a caller that awaited it can retry at once.
	private static void Complete(
		TaskCompletionSource<TikTokBusinessInitResult> completion,
		TikTokBusinessInitResult result
	)
	{
		if (result.IsSuccess)
		{
			_ready = true;
		}
		else
		{
			lock (Gate)
			{
				if (!completion.Task.IsCompleted && ReferenceEquals(_initialization, completion.Task))
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
		bundle.PutString(NativeTikTokBusinessInterop.KeyTikTokAppId, string.Join(',', config.TikTokAppIds));
		bundle.PutString(NativeTikTokBusinessInterop.KeyAccessToken, config.AccessToken.Trim());
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyDebugMode, config.DebugMode);
		if (config.LogLevel is { } logLevel)
		{
			bundle.PutInt(NativeTikTokBusinessInterop.KeyLogLevel, (int)logLevel);
		}
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
		bundle.PutBoolean(NativeTikTokBusinessInterop.KeyLowPerformanceDevice, config.LowPerformanceDevice);
		return bundle;
	}

	private static void Validate(TikTokBusinessConfig config)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(config.AppId, nameof(config.AppId));
		ArgumentException.ThrowIfNullOrWhiteSpace(config.AccessToken, nameof(config.AccessToken));
		if (!AndroidPackageName().IsMatch(config.AppId.Trim()))
		{
			throw new ArgumentException("AppId must be the Android application package name.", nameof(config));
		}
		if (config.TikTokAppIds is null || config.TikTokAppIds.Count == 0)
		{
			throw new ArgumentException("At least one TikTok App ID is required.", nameof(config));
		}
		foreach (var tiktokAppId in config.TikTokAppIds)
		{
			if (string.IsNullOrEmpty(tiktokAppId) || !NumericPattern().IsMatch(tiktokAppId))
			{
				throw new ArgumentException("Each TikTok App ID must be numeric, without spaces or commas.", nameof(config));
			}
		}
		if (config.AttAuthorizationDelay is { } delay && delay < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(config), "AttAuthorizationDelay must not be negative.");
		}
	}

	private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

	// Same pattern as com.tiktok.util.RegexUtil#validateAppId in the Android SDK.
	[GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$", RegexOptions.CultureInvariant)]
	private static partial Regex AndroidPackageName();

	[GeneratedRegex("^[0-9]+$", RegexOptions.CultureInvariant)]
	private static partial Regex NumericPattern();

	[GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
	private static partial Regex CurrencyPattern();
}
