using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DinnerRush.EditorTools
{
    /// <summary>
    /// One-button Android build, so a playable APK never depends on remembering the right toggles:
    /// only the game scene ships, ARM64 + IL2CPP (what current phones and the Play Store require),
    /// portrait, ASTC textures, and an `.apk` rather than an `.aab` so it can be sideloaded directly.
    ///
    /// Tools ▸ Chef Survivor ▸ Build APK. The result lands in `Builds/DinnerRush.apk`.
    /// </summary>
    public static class BuildAndroid
    {
        private const string OutputDir = "Builds";
        private const string OutputName = "DinnerRush.apk";

        /// <summary>Playtest APK: fully optimised, just permitted to still contain placeholder art
        /// (see <see cref="PlaceholderGuard"/>). NOT a Development Build — that would cost framerate
        /// and make a playtest misreport how the game actually runs.</summary>
        [MenuItem("Tools/Chef Survivor/Build APK (playtest)")]
        public static void BuildPlaytest() => Build(true);

        /// <summary>Release APK — fails while any placeholder art is still referenced.</summary>
        [MenuItem("Tools/Chef Survivor/Build APK (release)")]
        public static void BuildRelease() => Build(false);

        public static void Build() => Build(true);

        private static void Build(bool playtest)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildAndroid] No enabled scenes in Build Settings.");
                return;
            }

            Directory.CreateDirectory(OutputDir);
            var path = Path.Combine(OutputDir, OutputName);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.danil.dinnerrush");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.development = false;   // both builds are optimised
            PlaceholderGuard.AllowPlaceholdersOnce = playtest;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log("[BuildAndroid] OK → " + path + "  (" + (summary.totalSize / 1048576) + " MB, "
                          + (int)summary.totalTime.TotalSeconds + "s, " + (playtest ? "playtest" : "release") + ")");
            else
                Debug.LogError("[BuildAndroid] " + summary.result + " — " + summary.totalErrors + " error(s). "
                               + "See the Editor log for the failing step.");
        }
    }
}
