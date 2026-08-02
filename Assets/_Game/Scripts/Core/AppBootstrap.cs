using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Runs once before the first scene loads, so it applies in a build as well as in the editor.
    ///
    /// Unity's default is `targetFrameRate = -1` with vSync off, which on Android hands the decision
    /// to the platform — and the platform very often settles at 30 fps no matter how much headroom
    /// the game has. For an action game where the player is dodging a crowd, asking for 60 is the
    /// difference between "sluggish" and "responsive", and it costs nothing when the device cannot
    /// deliver it: the frame rate simply falls where it falls.
    /// </summary>
    public static class AppBootstrap
    {
        private const int TargetFps = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            // vSync must be off for targetFrameRate to be honoured at all.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
        }
    }
}
