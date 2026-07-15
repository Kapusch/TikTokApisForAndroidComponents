plugins {
	id("com.android.library")
	id("org.jetbrains.kotlin.android")
	id("org.jetbrains.kotlin.plugin.parcelize")
}
android {
	namespace = "com.tiktok.open.sdk.core"
	compileSdk = 34
	defaultConfig { minSdk = 21 }
	buildTypes { release { isMinifyEnabled = false } }
	compileOptions { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
	kotlinOptions { jvmTarget = "17" }
}
