pluginManagement {
  repositories { google(); mavenCentral(); gradlePluginPortal() }
}
dependencyResolutionManagement {
  repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
  repositories { google(); mavenCentral() }
}
rootProject.name = "KapuschTikTokInteropAndroid"
include(":tiktok-open-sdk-core", ":tiktok-open-sdk-share", ":tiktokinterop")
project(":tiktok-open-sdk-core").buildFileName = "build.gradle.kts"
project(":tiktok-open-sdk-share").buildFileName = "build.gradle.kts"
