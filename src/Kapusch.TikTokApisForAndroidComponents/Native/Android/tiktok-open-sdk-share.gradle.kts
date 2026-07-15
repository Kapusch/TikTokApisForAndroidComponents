plugins {
	id("com.android.library")
	id("org.jetbrains.kotlin.android")
	id("org.jetbrains.kotlin.plugin.parcelize")
}
android {
	namespace = "com.tiktok.open.sdk.share"
	compileSdk = 34
	defaultConfig {
		minSdk = 21
		buildConfigField("String", "SHARE_SDK_NAME", "\"tiktok-open-sdk-share\"")
		buildConfigField("String", "SHARE_SDK_VERSION", "\"2.3.1\"")
	}
	buildFeatures { buildConfig = true }
	buildTypes { release { isMinifyEnabled = false } }
	compileOptions { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
	kotlinOptions { jvmTarget = "17" }
}
dependencies {
	api(project(":tiktok-open-sdk-core"))
	implementation("com.google.code.gson:gson:2.9.1")
	implementation("androidx.browser:browser:1.3.0")
	implementation("com.google.android.gms:play-services-base:18.2.0")
	implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.6.1")
	implementation("com.google.android.gms:play-services-ads-identifier:18.0.1")
}
