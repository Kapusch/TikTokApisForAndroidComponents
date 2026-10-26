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
 * translates a [Bundle] configuration into `TTConfig`, guarantees main-thread
 * initialization, and reports the initialization outcome exactly once.
 * Nothing here touches the SDK before [initialize] is called.
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

    const val RESULT_SUCCESS = "success"
    const val RESULT_ERROR_CODE = "error_code"
    const val RESULT_ERROR_MESSAGE = "error_message"
    const val RESULT_NATIVE_ERROR_CODE = "native_error_code"

    private val APP_ID_PATTERN = Regex("^[a-zA-Z][a-zA-Z0-9_]*(\\.[a-zA-Z][a-zA-Z0-9_]*)+$")
    private val TIKTOK_APP_ID_PATTERN = Regex("^(\\d+,)*\\d+$")

    private val mainHandler by lazy { Handler(Looper.getMainLooper()) }

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
                complete(failureResult("native_exception", error.javaClass.simpleName, null))
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
            complete(successResult())
            return
        }

        val appId = config.getString(KEY_APP_ID).orEmpty().trim()
        val tiktokAppId = config.getString(KEY_TIKTOK_APP_ID).orEmpty().replace(" ", "")
        val accessToken = config.getString(KEY_ACCESS_TOKEN).orEmpty().trim()
        if (!APP_ID_PATTERN.matches(appId)) {
            complete(failureResult("invalid_app_id", "AppId must be the Android package name.", null))
            return
        }
        if (!TIKTOK_APP_ID_PATTERN.matches(tiktokAppId)) {
            complete(failureResult("invalid_tiktok_app_id", "TikTokAppId must be one or more comma-separated numeric ids.", null))
            return
        }
        if (accessToken.isEmpty()) {
            complete(failureResult("missing_access_token", "AccessToken (the Events Manager app secret) is required.", null))
            return
        }

        val application = context.applicationContext as? Application
        if (application == null) {
            complete(failureResult("invalid_context", "An Application context is required.", null))
            return
        }

        val ttConfig = TikTokBusinessSdk.TTConfig(application, accessToken)
            .setAppId(appId)
            .setTTAppId(tiktokAppId)
            .setLogLevel(logLevel(config.getInt(KEY_LOG_LEVEL, 0)))
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

        TikTokBusinessSdk.initializeSdk(ttConfig, object : TikTokBusinessSdk.TTInitCallback {
            override fun success() {
                complete(successResult())
            }

            override fun fail(code: Int, msg: String?) {
                Log.w(LOG_TAG, "TikTok Business SDK initialization failed code=$code")
                complete(failureResult("tiktok_$code", msg, code))
            }
        })
    }

    @JvmStatic
    fun isInitialized(): Boolean = TikTokBusinessSdk.isInitialized()

    @JvmStatic
    fun sdkVersion(): String = com.tiktok.BuildConfig.VERSION_NAME

    @JvmStatic
    fun startTrack() {
        TikTokBusinessSdk.startTrack()
    }

    /**
     * Tracks one event. [propertiesJson] is a JSON object produced by the managed
     * layer; an empty [eventId] lets the SDK generate one.
     */
    @JvmStatic
    fun trackEvent(name: String, propertiesJson: String?, eventId: String?) {
        if (name.isBlank()) return
        val properties = if (propertiesJson.isNullOrBlank()) JSONObject() else JSONObject(propertiesJson)
        val id = eventId?.takeIf { it.isNotBlank() }
        TikTokBusinessSdk.trackTTEvent(TTBaseEvent(name, properties, id))
    }

    @JvmStatic
    fun identify(externalId: String?, externalUserName: String?, phoneNumber: String?, email: String?) {
        TikTokBusinessSdk.identify(externalId, externalUserName, phoneNumber, email)
    }

    @JvmStatic
    fun logout() {
        TikTokBusinessSdk.logout()
    }

    @JvmStatic
    fun flush() {
        TikTokBusinessSdk.flush()
    }

    private fun logLevel(value: Int): TikTokBusinessSdk.LogLevel = when (value) {
        1 -> TikTokBusinessSdk.LogLevel.INFO
        2 -> TikTokBusinessSdk.LogLevel.WARN
        3 -> TikTokBusinessSdk.LogLevel.DEBUG
        else -> TikTokBusinessSdk.LogLevel.NONE
    }

    private fun successResult(): Bundle = Bundle().apply { putBoolean(RESULT_SUCCESS, true) }

    private fun failureResult(code: String, message: String?, nativeCode: Int?): Bundle = Bundle().apply {
        putBoolean(RESULT_SUCCESS, false)
        putString(RESULT_ERROR_CODE, code)
        message?.let { putString(RESULT_ERROR_MESSAGE, it) }
        nativeCode?.let { putInt(RESULT_NATIVE_ERROR_CODE, it) }
    }
}
