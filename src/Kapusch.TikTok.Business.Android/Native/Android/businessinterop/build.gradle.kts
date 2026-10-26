plugins {
	id("com.android.library")
	id("org.jetbrains.kotlin.android")
}
android {
	namespace = "com.kapusch.tiktok.business.androidinterop"
	compileSdk = 34
	defaultConfig { minSdk = 21; consumerProguardFiles("consumer-rules.pro") }
	buildTypes { release { isMinifyEnabled = false } }
	compileOptions { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
	kotlinOptions { jvmTarget = "17" }
}
dependencies {
	// The checksum-locked upstream AAR is shipped as its own package entry, so the
	// interop only compiles against its classes and never repackages them.
	compileOnly(files("../upstream/compile/classes.jar"))
	// Runtime AndroidX and Kotlin classes come from managed NuGet dependencies of
	// the .NET package (see the csproj), not from this AAR.
}
