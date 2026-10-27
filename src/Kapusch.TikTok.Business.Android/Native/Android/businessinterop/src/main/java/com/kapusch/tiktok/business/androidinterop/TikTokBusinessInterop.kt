package com.kapusch.tiktok.business.androidinterop

import android.app.Application
import android.content.Context
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.webkit.ValueCallback
import com.tiktok.TikTokBusinessSdk
import com.tiktok.appevents.base.TTBaseEvent
import org.json.JSONObject
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Thin JNI surface over the official TikTok Business SDK.
 *
 * The upstream SDK owns event persistence, batching and retries. This facade only
 * translates a [Bundle] configuration into `TTConfig`, runs the initialization on the
 * main thread, and reports its outcome at most once. Nothing here touches the SDK
 * before [initialize] is called.
 *
 * SDK 1.7.1 constraints this facade works around:
 * - `initializeSdk` assigns its static instance before building the event logger,
 *   then returns silently, without calling back, on every later call;
 * - its asynchronous init step logs and swallows any Throwable without calling
 *   `success()` or `fail()`.
 * A second native attempt therefore never completes, and a first attempt can stay
 * silent. [nativeInitAttempted] makes later attempts fail at once with
 * [ERROR_INIT_INCOMPLETE]; the managed caller bounds the wait for a silent first
 * attempt.
 */
object TikTokBusinessInterop {
    private const val LOG_TAG = "KapuschTikTokBusiness"

    const val KEY_APP_ID = "app_id"
    const val KEY_TIKTOK_APP_ID = "tiktok_app_id"
    const val KEY_ACCESS_TOKEN = "access_token"
    const val KEY_DEBUG_MODE = "debug_mode"
    const val KEY_LOG_LEVEL = "log_level"
    const val KEY_DISABLE_AUTO_START = "disable_auto_start"
    const val KEY_DISABLE_AUTOMATIC_TRACKING = "disable_automatic_tracking"
    const val KEY_DISABLE_INSTALL_TRACKING = "disable_install_tracking"
    const val KEY_DISABLE_LAUNCH_TRACKING = "disable_launch_tracking"
    const val KEY_DISABLE_RETENTION_TRACKING = "disable_retention_tracking"
    const val KEY_DISABLE_PAYMENT_TRACKING = "disable_payment_tracking"
    const val KEY_DISABLE_ADVERTISER_ID_COLLECTION = "disable_advertiser_id_collection"
    const val KEY_DISABLE_ENHANCED_DATA_POSTBACK = "disable_enhanced_data_postback"
    const val KEY_DISABLE_MONITOR = "disable_monitor"
    const val KEY_LIMITED_DATA_USE = "limited_data_use"
    const val KEY_LOW_PERFORMANCE_DEVICE = "low_performance_device"

    const val RESULT_SUCCESS = "success"
    const val RESULT_TRACKING_ENABLED = "tracking_enabled"
    const val RESULT_ERROR_CODE = "error_code"
    const val RESULT_ERROR_MESSAGE = "error_message"

    // Wrapper error codes, the same values as TikTokBusinessInitResult in the managed layer.
    // Native SDK codes (1.7.1 only reports -2) are forwarded unchanged.
    const val ERROR_INVALID_CONFIGURATION = 1001
    const val ERROR_CONFIGURATION_REJECTED = 1002
    const val ERROR_INVALID_CONTEXT = 1101
    const val ERROR_NATIVE_EXCEPTION = 1103
    const val ERROR_INIT_INCOMPLETE = 1104

    // Same patterns as com.tiktok.util.RegexUtil. The SDK blanks a non-matching id and
    // initializes anyway, so reject those values before it sees them.
    private val APP_ID_PATTERN = Regex("^[a-zA-Z][a-zA-Z0-9_]*(\\.[a-zA-Z][a-zA-Z0-9_]*)+$")
    private val TIKTOK_APP_ID_PATTERN = Regex("^(\\d+,)*\\d+$")

    private val mainHandler by lazy { Handler(Looper.getMainLooper()) }

    // Set on the main thread right before the only call to TikTokBusinessSdk.initializeSdk.
    @Volatile
    private var nativeInitAttempted = false

    /**
     * Initializes the SDK on the main thread and calls [callback] at most once with a
     * result [Bundle]. When the SDK init step stays silent there is no callback: the
     * managed caller bounds the wait.
     */
    @JvmStatic
    fun initialize(context: Context, config: Bundle, callback: ValueCallback<Bundle>) {
        val completed = AtomicBoolean(false)
        val complete: (Bundle) -> Unit = { result ->
            if (completed.compareAndSet(false, true)) {
                try {
                    callback.onReceiveValue(result)
                } catch (error: Throwable) {
                    Log.e(LOG_TAG, "Initialization callback failed", error)
                }
            }
        }
        val run = Runnable {
            try {
                initializeOnMainThread(context, config, complete)
            } catch (error: Throwable) {
                Log.e(LOG_TAG, "TikTok Business SDK initialization threw", error)
                complete(failureResult(ERROR_NATIVE_EXCEPTION, error.javaClass.simpleName))
            }
        }
        if (Looper.myLooper() == Looper.getMainLooper()) {
            run.run()
        } else {
            mainHandler.post(run)
        }
    }

    private fun initializeOnMainThread(context: Context, config: Bundle, complete: (Bundle) -> Unit) {
        if (TikTokBusinessSdk.isInitialized()) {
            // The SDK is a process-wide singleton: report its current state.
            complete(successResult())
            return
        }
        if (nativeInitAttempted) {
            // The SDK ignores a second initializeSdk call without calling back, so
            // never re-enter it: report the earlier attempt as incomplete instead.
            complete(
                failureResult(
                    ERROR_INIT_INCOMPLETE,
                    "The TikTok Business SDK was already initialized once in this process and did not complete.",
                ),
            )
            return
        }

        val appId = config.getString(KEY_APP_ID).orEmpty().trim()
        val tiktokAppId = config.getString(KEY_TIKTOK_APP_ID).orEmpty().replace(" ", "")
        val accessToken = config.getString(KEY_ACCESS_TOKEN).orEmpty().trim()
        if (appId.isEmpty() || tiktokAppId.isEmpty() || accessToken.isEmpty()) {
            complete(failureResult(ERROR_INVALID_CONFIGURATION, "invalid_configuration"))
            return
        }
        if (!APP_ID_PATTERN.matches(appId) || !TIKTOK_APP_ID_PATTERN.matches(tiktokAppId)) {
            complete(failureResult(ERROR_CONFIGURATION_REJECTED, "configuration_rejected"))
            return
        }

        val application = context.applicationContext as? Application
        if (application == null) {
            complete(failureResult(ERROR_INVALID_CONTEXT, "An Application context is required."))
            return
        }

        val ttConfig = TikTokBusinessSdk.TTConfig(application, accessToken)
            .setAppId(appId)
            .setTTAppId(tiktokAppId)
        if (config.containsKey(KEY_LOG_LEVEL)) ttConfig.setLogLevel(logLevel(config.getInt(KEY_LOG_LEVEL)))
        if (config.getBoolean(KEY_DEBUG_MODE, false)) ttConfig.openDebugMode()
        if (config.getBoolean(KEY_DISABLE_AUTO_START, false)) ttConfig.disableAutoStart()
        if (config.getBoolean(KEY_DISABLE_AUTOMATIC_TRACKING, false)) ttConfig.disableAutoEvents()
        if (config.getBoolean(KEY_DISABLE_INSTALL_TRACKING, false)) ttConfig.disableInstallLogging()
        if (config.getBoolean(KEY_DISABLE_LAUNCH_TRACKING, false)) ttConfig.disableLaunchLogging()
        if (config.getBoolean(KEY_DISABLE_RETENTION_TRACKING, false)) ttConfig.disableRetentionLogging()
        if (config.getBoolean(KEY_DISABLE_PAYMENT_TRACKING, false)) ttConfig.disableAutoIapTrack()
        if (config.getBoolean(KEY_DISABLE_ADVERTISER_ID_COLLECTION, false)) ttConfig.disableAdvertiserIDCollection()
        if (config.getBoolean(KEY_DISABLE_ENHANCED_DATA_POSTBACK, false)) ttConfig.disableAutoEnhancedDataPostbackEvent()
        if (config.getBoolean(KEY_DISABLE_MONITOR, false)) ttConfig.disableMonitor()
        if (config.getBoolean(KEY_LIMITED_DATA_USE, false)) ttConfig.enableLimitedDataUse()
        if (config.getBoolean(KEY_LOW_PERFORMANCE_DEVICE, false)) ttConfig.setIsLowPerformanceDevice(true)

        nativeInitAttempted = true
        TikTokBusinessSdk.initializeSdk(ttConfig, object : TikTokBusinessSdk.TTInitCallback {
            override fun success() {
                complete(successResult())
            }

            override fun fail(code: Int, msg: String?) {
                Log.w(LOG_TAG, "TikTok Business SDK initialization failed code=$code")
                complete(failureResult(code, msg ?: "initialization_failed"))
            }
        })
    }

    @JvmStatic
    fun isInitialized(): Boolean = TikTokBusinessSdk.isInitialized()

    /** Whether the SDK sends events: false until [startTrack] after `disableAutoStart()`. */
    @JvmStatic
    fun isTrackingEnabled(): Boolean = TikTokBusinessSdk.isInitialized() && TikTokBusinessSdk.getNetworkSwitch()

    @JvmStatic
    fun sdkVersion(): String = com.tiktok.BuildConfig.VERSION_NAME

    /** Test event code shown in Events Manager; the SDK only creates one in debug mode. */
    @JvmStatic
    fun testEventCode(): String? =
        if (TikTokBusinessSdk.isInitialized()) TikTokBusinessSdk.getTestEventCode()?.takeIf { it.isNotBlank() } else null

    // Every call below runs synchronously on the calling thread and returns true when it was
    // handed to the SDK, and false when the SDK is not initialized, an argument is empty, or
    // the SDK threw. Measurement is fire-and-forget: a failure is logged and never propagates
    // into the host application.
    //
    // Call order: identify() and logout() change the SDK identity before they return, so an
    // event tracked after them carries the new identity. The SDK copies the identity when its
    // own event thread builds a queued event, so an event tracked right before them may carry
    // the new identity too. TikTokBusinessSdk.getAppEventLogger() returns null outside SDK
    // call stacks, so that queue cannot be used to order these calls.

    @JvmStatic
    fun startTrack(): Boolean = guarded("startTrack") { TikTokBusinessSdk.startTrack() }

    /**
     * Tracks one event. [propertiesJson] is a JSON object produced by the managed
     * layer; a null [eventId] lets the SDK generate one.
     */
    @JvmStatic
    fun trackEvent(name: String, propertiesJson: String?, eventId: String?): Boolean {
        if (name.isBlank()) return false
        return guarded("trackEvent") {
            val properties = if (propertiesJson.isNullOrBlank()) JSONObject() else JSONObject(propertiesJson)
            TikTokBusinessSdk.trackTTEvent(TTBaseEvent(name, properties, eventId?.takeIf { it.isNotBlank() }))
        }
    }

    @JvmStatic
    fun identify(externalId: String?, externalUserName: String?, phoneNumber: String?, email: String?): Boolean {
        if (listOf(externalId, externalUserName, phoneNumber, email).all { it.isNullOrBlank() }) return false
        return guarded("identify") { TikTokBusinessSdk.identify(externalId, externalUserName, phoneNumber, email) }
    }

    @JvmStatic
    fun logout(): Boolean = guarded("logout") { TikTokBusinessSdk.logout() }

    @JvmStatic
    fun flush(): Boolean = guarded("flush") { TikTokBusinessSdk.flush() }

    @JvmStatic
    fun updateAccessToken(accessToken: String): Boolean {
        if (accessToken.isBlank()) return false
        return guarded("updateAccessToken") { TikTokBusinessSdk.updateAccessToken(accessToken) }
    }

    private inline fun guarded(operation: String, block: () -> Unit): Boolean {
        if (!TikTokBusinessSdk.isInitialized()) return false
        return try {
            block()
            true
        } catch (error: Throwable) {
            Log.w(LOG_TAG, "TikTok Business SDK $operation failed: ${error.javaClass.simpleName}")
            false
        }
    }

    // Values of the managed TikTokBusinessLogLevel enum, shared with the iOS package. The
    // Android SDK orders its levels NONE < INFO < WARN < DEBUG, shows every message at or
    // below the configured level, and logs errors at INFO. Each managed level maps to the
    // quietest Android level that still shows the messages it asks for.
    private fun logLevel(value: Int): TikTokBusinessSdk.LogLevel = when (value) {
        1 -> TikTokBusinessSdk.LogLevel.INFO
        2, 3 -> TikTokBusinessSdk.LogLevel.WARN
        4, 5 -> TikTokBusinessSdk.LogLevel.DEBUG
        else -> TikTokBusinessSdk.LogLevel.NONE
    }

    private fun successResult(): Bundle = Bundle().apply {
        putBoolean(RESULT_SUCCESS, true)
        putBoolean(RESULT_TRACKING_ENABLED, TikTokBusinessSdk.getNetworkSwitch())
    }

    private fun failureResult(code: Int, message: String): Bundle = Bundle().apply {
        putBoolean(RESULT_SUCCESS, false)
        putInt(RESULT_ERROR_CODE, code)
        putString(RESULT_ERROR_MESSAGE, message)
    }
}
