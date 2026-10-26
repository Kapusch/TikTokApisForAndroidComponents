using Android.OS;
using Android.Runtime;
using Android.Webkit;

namespace Kapusch.TikTok.Business.Android.Internal;

/// <summary>
/// JNI calls into <c>com.kapusch.tiktok.business.androidinterop.TikTokBusinessInterop</c>.
/// The Java class is resolved lazily, so nothing native is loaded before the first call.
/// </summary>
internal static class NativeTikTokBusinessInterop
{
	private const string ClassName = "com/kapusch/tiktok/business/androidinterop/TikTokBusinessInterop";

	// Bundle keys and result keys shared with TikTokBusinessInterop.kt.
	internal const string KeyAppId = "app_id";
	internal const string KeyTikTokAppId = "tiktok_app_id";
	internal const string KeyAccessToken = "access_token";
	internal const string KeyDebugMode = "debug_mode";
	internal const string KeyLogLevel = "log_level";
	internal const string KeyDisableAutoStart = "disable_auto_start";
	internal const string KeyDisableAutomaticTracking = "disable_automatic_tracking";
	internal const string KeyDisableInstallTracking = "disable_install_tracking";
	internal const string KeyDisableLaunchTracking = "disable_launch_tracking";
	internal const string KeyDisableRetentionTracking = "disable_retention_tracking";
	internal const string KeyDisablePaymentTracking = "disable_payment_tracking";
	internal const string KeyDisableAdvertiserIdCollection = "disable_advertiser_id_collection";
	internal const string KeyDisableEnhancedDataPostback = "disable_enhanced_data_postback";
	internal const string KeyDisableMonitor = "disable_monitor";
	internal const string KeyLimitedDataUse = "limited_data_use";

	private const string ResultSuccess = "success";
	private const string ResultErrorCode = "error_code";
	private const string ResultErrorMessage = "error_message";
	private const string ResultNativeErrorCode = "native_error_code";

	private static readonly Lazy<IntPtr> Class = new(
		() => JNIEnv.FindClass(ClassName),
		LazyThreadSafetyMode.ExecutionAndPublication
	);

	internal static void Initialize(Bundle config, Action<TikTokBusinessInitResult> onResult)
	{
		var context = global::Android.App.Application.Context;
		var callback = new InitCallback(onResult);
		JNIEnv.CallStaticVoidMethod(
			Class.Value,
			Method(
				"initialize",
				"(Landroid/content/Context;Landroid/os/Bundle;Landroid/webkit/ValueCallback;)V"
			),
			new JValue(context),
			new JValue(config),
			new JValue(callback)
		);
	}

	internal static bool IsInitialized() =>
		JNIEnv.CallStaticBooleanMethod(Class.Value, Method("isInitialized", "()Z"));

	internal static string SdkVersion() =>
		JNIEnv.GetString(
			JNIEnv.CallStaticObjectMethod(Class.Value, Method("sdkVersion", "()Ljava/lang/String;")),
			JniHandleOwnership.TransferLocalRef
		) ?? string.Empty;

	internal static void TrackEvent(string name, string? propertiesJson, string? eventId)
	{
		var jName = JNIEnv.NewString(name);
		var jProperties = NewStringOrNull(propertiesJson);
		var jEventId = NewStringOrNull(eventId);
		try
		{
			JNIEnv.CallStaticVoidMethod(
				Class.Value,
				Method("trackEvent", "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)V"),
				new JValue(jName),
				new JValue(jProperties),
				new JValue(jEventId)
			);
		}
		finally
		{
			DeleteLocalRef(jName);
			DeleteLocalRef(jProperties);
			DeleteLocalRef(jEventId);
		}
	}

	internal static void Identify(string externalId, string? externalUserName, string? phoneNumber, string? email)
	{
		var jId = JNIEnv.NewString(externalId);
		var jName = NewStringOrNull(externalUserName);
		var jPhone = NewStringOrNull(phoneNumber);
		var jEmail = NewStringOrNull(email);
		try
		{
			JNIEnv.CallStaticVoidMethod(
				Class.Value,
				Method(
					"identify",
					"(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)V"
				),
				new JValue(jId),
				new JValue(jName),
				new JValue(jPhone),
				new JValue(jEmail)
			);
		}
		finally
		{
			DeleteLocalRef(jId);
			DeleteLocalRef(jName);
			DeleteLocalRef(jPhone);
			DeleteLocalRef(jEmail);
		}
	}

	internal static void StartTrack() => CallVoid("startTrack");

	internal static void Logout() => CallVoid("logout");

	internal static void Flush() => CallVoid("flush");

	private static void CallVoid(string name) =>
		JNIEnv.CallStaticVoidMethod(Class.Value, Method(name, "()V"));

	private static IntPtr Method(string name, string signature) =>
		JNIEnv.GetStaticMethodID(Class.Value, name, signature);

	private static IntPtr NewStringOrNull(string? value) =>
		value is null ? IntPtr.Zero : JNIEnv.NewString(value);

	private static void DeleteLocalRef(IntPtr handle)
	{
		if (handle != IntPtr.Zero)
		{
			JNIEnv.DeleteLocalRef(handle);
		}
	}

	internal static TikTokBusinessInitResult ToResult(Bundle? bundle)
	{
		if (bundle is null)
		{
			return TikTokBusinessInitResult.Failure(
				TikTokBusinessInitErrorCodes.NativeException,
				"The native layer returned no initialization result."
			);
		}
		if (bundle.GetBoolean(ResultSuccess, false))
		{
			return TikTokBusinessInitResult.Success;
		}
		int? nativeCode = bundle.ContainsKey(ResultNativeErrorCode) ? bundle.GetInt(ResultNativeErrorCode) : null;
		return TikTokBusinessInitResult.Failure(
			bundle.GetString(ResultErrorCode) ?? TikTokBusinessInitErrorCodes.NativeException,
			bundle.GetString(ResultErrorMessage),
			nativeCode
		);
	}

	/// <summary>Receives the one-shot initialization result from the Kotlin facade.</summary>
	private sealed class InitCallback : Java.Lang.Object, IValueCallback
	{
		private Action<TikTokBusinessInitResult>? _onResult;

		internal InitCallback(Action<TikTokBusinessInitResult> onResult) => _onResult = onResult;

		public void OnReceiveValue(Java.Lang.Object? value)
		{
			var onResult = Interlocked.Exchange(ref _onResult, null);
			onResult?.Invoke(ToResult(value as Bundle ?? value?.JavaCast<Bundle>()));
		}
	}
}
