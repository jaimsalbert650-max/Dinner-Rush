using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DinnerRush.EditorTools
{
    /// <summary>
    /// The kit marks temporary art with two dashed sprites, and `00-README.md` §Placeholders requires
    /// that a build **fail** while either is still referenced — otherwise a dashed box ships as if it
    /// were art. The screens build their UI in code, so the reference to catch is the sprite name in a
    /// script, which is what this scans for.
    ///
    /// Run it any time from Tools ▸ Chef Survivor ▸ Check placeholder art; it also runs automatically
    /// before a build and throws, which aborts it.
    /// </summary>
    public class PlaceholderGuard : IPreprocessBuildWithReport
    {
        private static readonly string[] PlaceholderSprites =
        {
            "panel_placeholder_dashed",
            "panel_diorama_dashed",
        };

        private const string ScriptRoot = "Assets/_Game/Scripts";

        /// <summary>Set for a single build by the playtest menu item. Deliberately not tied to
        /// Development Build: a playtest APK should be as fast as the real thing, or the framerate it
        /// shows is a lie.</summary>
        public static bool AllowPlaceholdersOnce;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool allowed = AllowPlaceholdersOnce;
            AllowPlaceholdersOnce = false;      // one build only, never sticky

            var hits = Scan();
            if (hits.Count == 0) return;

            var message = "Placeholder art is still referenced (00-README.md §Placeholders):\n"
                          + string.Join("\n", hits);

            // A release build must never ship a dashed box as if it were art — that is the rule the
            // spec asks for. A playtest build says so explicitly and only gets a warning.
            if (allowed)
            {
                Debug.LogWarning("[PlaceholderGuard] " + message + "\nAllowed: playtest build.");
                return;
            }

            throw new BuildFailedException(message
                + "\nReplace it, or use Tools > Chef Survivor > Build APK (playtest).");
        }

        [MenuItem("Tools/Chef Survivor/Check placeholder art")]
        private static void CheckFromMenu()
        {
            var hits = Scan();
            if (hits.Count == 0)
            {
                Debug.Log("[PlaceholderGuard] No placeholder sprites referenced — clear to ship.");
                return;
            }
            Debug.LogWarning("[PlaceholderGuard] " + hits.Count + " placeholder reference(s) still in code:\n"
                             + string.Join("\n", hits));
        }

        /// <summary>Every `file:line` that still names a placeholder sprite.</summary>
        private static List<string> Scan()
        {
            var hits = new List<string>();
            if (!Directory.Exists(ScriptRoot)) return hits;

            foreach (var path in Directory.GetFiles(ScriptRoot, "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                    foreach (var sprite in PlaceholderSprites)
                        if (lines[i].Contains(sprite))
                            hits.Add("  " + path.Replace('\\', '/') + ":" + (i + 1) + "  → " + sprite);
            }
            return hits;
        }
    }
}
