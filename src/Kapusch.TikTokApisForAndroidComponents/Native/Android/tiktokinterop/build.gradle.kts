plugins {
	id("com.android.library")
	id("org.jetbrains.kotlin.android")
}
val packagedRuntime by configurations.creating
android {
	namespace = "com.kapusch.tiktok.androidinterop"
	compileSdk = 34
	defaultConfig { minSdk = 21; consumerProguardFiles("consumer-rules.pro") }
	buildTypes { release { isMinifyEnabled = false } }
	compileOptions { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
	kotlinOptions { jvmTarget = "17" }
}
dependencies {
	implementation(project(":tiktok-open-sdk-share"))
	implementation("androidx.core:core-ktx:1.12.0")
	packagedRuntime("com.google.code.gson:gson:2.9.1")
	packagedRuntime("androidx.browser:browser:1.3.0")
	packagedRuntime("com.google.android.gms:play-services-base:18.2.0")
	packagedRuntime("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.6.1")
	packagedRuntime("com.google.android.gms:play-services-ads-identifier:18.0.1")
	packagedRuntime("androidx.core:core-ktx:1.12.0")
}
tasks.register<Copy>("copyReleaseRuntimeDependencies") {
	dependsOn("assembleRelease")
	from(packagedRuntime)
	into(layout.buildDirectory.dir("runtime-dependencies"))
}
