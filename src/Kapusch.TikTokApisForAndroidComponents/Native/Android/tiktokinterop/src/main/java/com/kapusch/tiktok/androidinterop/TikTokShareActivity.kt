package com.kapusch.tiktok.androidinterop

import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Bundle
import android.util.Log
import androidx.core.content.FileProvider
import com.tiktok.open.sdk.core.appcheck.TikTokAppCheckUtil
import com.tiktok.open.sdk.share.MediaType
import com.tiktok.open.sdk.share.ShareApi
import com.tiktok.open.sdk.share.ShareRequest
import com.tiktok.open.sdk.share.model.MediaContent
import java.io.File

class TikTokShareActivity : Activity() {
    companion object {
        const val EXTRA_IMAGE_PATHS = "ktk_image_paths"
        const val EXTRA_STATUS = "ktk_status"
        const val EXTRA_ERROR_CODE = "ktk_error_code"
        private const val CLIENT_KEY_META_DATA = "com.kapusch.tiktok.CLIENT_KEY"
        private const val LOG_TAG = "KapuschTikTokShare"
        private const val STATUS_SUCCESS = "success"
        private const val STATUS_DRAFT = "draft"
        private const val STATUS_CANCELLED = "cancelled"
        private const val STATUS_FAILED = "failed"
        private const val ERROR_USER_CANCELLED = -2
        private const val ERROR_DRAFT_SAVED = 20016
        private const val ERROR_SHARE_CANCELLED = 20013
    }

    private lateinit var shareApi: ShareApi
    private var waitingForCallback = false
    private var leftForTikTok = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        shareApi = ShareApi(this)

        if (handleTikTokCallback(intent)) return
        if (savedInstanceState != null) {
            waitingForCallback = savedInstanceState.getBoolean("waiting_for_callback", false)
            leftForTikTok = savedInstanceState.getBoolean("left_for_tiktok", false)
            return
        }

        startShare()
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleTikTokCallback(intent)
    }

    override fun onPause() {
        if (waitingForCallback) {
            leftForTikTok = true
        }
        super.onPause()
    }

    override fun onResume() {
        super.onResume()
        if (waitingForCallback && leftForTikTok && !handleTikTokCallback(intent)) {
            finishWith(STATUS_FAILED, "returned_without_callback")
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putBoolean("waiting_for_callback", waitingForCallback)
        outState.putBoolean("left_for_tiktok", leftForTikTok)
        super.onSaveInstanceState(outState)
    }

    private fun startShare() {
        if (!TikTokAppCheckUtil.isTikTokAppInstalled(this)) {
            finishWith(STATUS_FAILED, "tiktok_not_installed")
            return
        }

        val imagePaths = intent.getStringArrayExtra(EXTRA_IMAGE_PATHS)?.toList().orEmpty()
        if (imagePaths.size < 2 || imagePaths.any { !File(it).isFile }) {
            finishWith(STATUS_FAILED, "invalid_image_paths")
            return
        }

        val clientKey = readClientKey()
        if (clientKey.isBlank() || clientKey.startsWith("${'$'}{")) {
            finishWith(STATUS_FAILED, "missing_client_key")
            return
        }

        val targetPackage = TikTokAppCheckUtil.getInstalledTikTokApp(this)?.appPackageName
        if (targetPackage.isNullOrBlank()) {
            finishWith(STATUS_FAILED, "tiktok_not_installed")
            return
        }

        val uris = ArrayList<String>(imagePaths.size)
        try {
            imagePaths.forEach { path ->
                val uri: Uri = FileProvider.getUriForFile(this, "$packageName.fileprovider", File(path))
                grantUriPermission(targetPackage, uri, Intent.FLAG_GRANT_READ_URI_PERMISSION)
                uris.add(uri.toString())
            }
        } catch (_: Exception) {
            finishWith(STATUS_FAILED, "file_provider_uri_failed")
            return
        }

        val request = ShareRequest(
            clientKey = clientKey,
            mediaContent = MediaContent(MediaType.IMAGE, uris),
            packageName = packageName,
            resultActivityFullPath = javaClass.name,
        )
        Log.i(
            LOG_TAG,
            "Launching OpenSDK share target=$targetPackage mediaCount=${uris.size}",
        )
        val launchResult = shareApi.share(request)
        if (launchResult.result != 0) {
            Log.e(LOG_TAG, "OpenSDK rejected share launch result=${launchResult.result}")
            finishWith(STATUS_FAILED, "share_request_not_sent")
            return
        }

        waitingForCallback = true
    }

    private fun readClientKey(): String =
        try {
            @Suppress("DEPRECATION")
            val appInfo = packageManager.getApplicationInfo(packageName, PackageManager.GET_META_DATA)
            appInfo.metaData?.getString(CLIENT_KEY_META_DATA).orEmpty()
        } catch (_: PackageManager.NameNotFoundException) {
            ""
        }

    private fun handleTikTokCallback(callbackIntent: Intent?): Boolean {
        val response = shareApi.getShareResponseFromIntent(callbackIntent) ?: return false
        Log.i(
            LOG_TAG,
            "Share response"
                + " errorCode=${response.errorCode}"
                + " subErrorCode=${response.subErrorCode}"
                + " state=${response.state.orEmpty()}"
                + " errorMessage=${response.errorMsg.orEmpty()}",
        )
        when {
            response.errorCode == 0 -> finishWith(STATUS_SUCCESS, null)
            response.subErrorCode == ERROR_DRAFT_SAVED -> finishWith(STATUS_DRAFT, null)
            response.errorCode == ERROR_USER_CANCELLED ||
                response.subErrorCode == ERROR_SHARE_CANCELLED ->
                finishWith(STATUS_CANCELLED, null)
            response.errorMsg?.contains("cancel", ignoreCase = true) == true ->
                finishWith(STATUS_CANCELLED, null)
            else ->
                finishWith(
                    STATUS_FAILED,
                    buildErrorCode(response.errorCode, response.subErrorCode),
                )
        }
        return true
    }

    private fun buildErrorCode(errorCode: Int, subErrorCode: Int?): String =
        if (subErrorCode == null || subErrorCode == 0) {
            "tiktok_$errorCode"
        } else {
            "tiktok_${errorCode}_sub_$subErrorCode"
        }

    private fun finishWith(status: String, errorCode: String?) {
        val result = Intent().putExtra(EXTRA_STATUS, status)
        errorCode?.let { result.putExtra(EXTRA_ERROR_CODE, it) }
        setResult(RESULT_OK, result)
        finish()
    }
}
