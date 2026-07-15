package com.kapusch.tiktok.androidinterop

import android.app.Activity
import android.content.Intent
import android.net.Uri
import android.os.Bundle
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
        private const val STATUS_SUCCESS = "success"
        private const val STATUS_DRAFT = "draft"
        private const val STATUS_CANCELLED = "cancelled"
        private const val STATUS_FAILED = "failed"
        private const val ERROR_DRAFT_SAVED = 20016
    }

    private lateinit var shareApi: ShareApi
    private var waitingForCallback = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        shareApi = ShareApi(this)

        if (handleTikTokCallback(intent)) return
        if (savedInstanceState != null) {
            waitingForCallback = savedInstanceState.getBoolean("waiting_for_callback", false)
            return
        }

        startShare()
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleTikTokCallback(intent)
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putBoolean("waiting_for_callback", waitingForCallback)
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

        val clientKey = applicationInfo.metaData?.getString(CLIENT_KEY_META_DATA).orEmpty()
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
        val launchResult = shareApi.share(request)
        if (launchResult.result != 0) {
            finishWith(STATUS_FAILED, "launch_${launchResult.result}")
            return
        }

        waitingForCallback = true
    }

    private fun handleTikTokCallback(callbackIntent: Intent?): Boolean {
        val response = shareApi.getShareResponseFromIntent(callbackIntent) ?: return false
        when {
            response.errorCode == 0 -> finishWith(STATUS_SUCCESS, null)
            response.errorCode == ERROR_DRAFT_SAVED -> finishWith(STATUS_DRAFT, null)
            response.errorMsg?.contains("cancel", ignoreCase = true) == true ->
                finishWith(STATUS_CANCELLED, null)
            else -> finishWith(STATUS_FAILED, "tiktok_${response.errorCode}")
        }
        return true
    }

    private fun finishWith(status: String, errorCode: String?) {
        val result = Intent().putExtra(EXTRA_STATUS, status)
        errorCode?.let { result.putExtra(EXTRA_ERROR_CODE, it) }
        setResult(RESULT_OK, result)
        finish()
    }
}
