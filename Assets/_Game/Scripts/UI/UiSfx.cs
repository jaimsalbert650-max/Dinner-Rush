using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// UI audio. Every screen spec asks for a sound on each interactive element — `ButtonTap`,
    /// `ButtonDenied`, `Purchase`, `CardPick`, `DailyClaim`, `UnlockChef`, `Revive`, `VictorySting`,
    /// `LevelUpFanfare` — so this owns the names and the plumbing.
    ///
    /// **There are no clips in the project yet.** Drop `.wav`/`.ogg` files named after the constants
    /// below into `Assets/_Game/Resources/sfx/` and they start playing with no further wiring; until
    /// then every call is a silent no-op (reported once per name, not per tap).
    ///
    /// Taps bind themselves: <see cref="Bind"/> walks a screen's buttons when it opens, so a new screen
    /// gets its tap sound for free and no button site has to remember to ask for one.
    /// </summary>
    public static class UiSfx
    {
        public const string Tap = "ButtonTap";
        public const string Denied = "ButtonDenied";
        public const string Purchase = "Purchase";
        public const string CardPick = "CardPick";
        public const string DailyClaim = "DailyClaim";
        public const string UnlockChef = "UnlockChef";
        public const string Revive = "Revive";
        public const string VictorySting = "VictorySting";
        public const string LevelUpFanfare = "LevelUpFanfare";
        public const string CoinPickup = "CoinPickup";
        public const string PlayerHit = "PlayerHit";

        private const string ResourceFolder = "sfx/";
        private const float MinGap = 0.04f;      // collapses double-fires from one tap

        private static readonly Dictionary<string, AudioClip> Cache = new();
        private static readonly HashSet<string> Missing = new();
        private static AudioSource _source;
        private static float _lastPlay = -1f;

        /// <summary>Play a named UI sound. Unknown / absent clips are silently skipped.</summary>
        public static void Play(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var clip = Load(name);
            if (clip == null) return;

            if (Time.unscaledTime - _lastPlay < MinGap) return;
            _lastPlay = Time.unscaledTime;

            var src = Source();
            if (src == null) return;
            src.PlayOneShot(clip, Mathf.Clamp01(SettingsPanel.SoundVolume));
        }

        public static void PlayTap() => Play(Tap);
        public static void PlayDenied() => Play(Denied);

        /// <summary>Give every button under `root` a tap sound. Idempotent — a screen can be bound on
        /// every open without stacking listeners.</summary>
        public static void Bind(GameObject root)
        {
            if (root == null) return;
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<UiSfxBound>() != null) continue;
                b.gameObject.AddComponent<UiSfxBound>();
                b.onClick.AddListener(PlayTap);
            }
        }

        private static AudioClip Load(string name)
        {
            if (Cache.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>(ResourceFolder + name);
            Cache[name] = clip;
            if (clip == null && Missing.Add(name))
                Debug.Log("[UiSfx] no clip for '" + name + "' — drop one at Resources/" + ResourceFolder + name);
            return clip;
        }

        private static AudioSource Source()
        {
            if (_source != null) return _source;
            var go = new GameObject("UiSfx");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;          // UI is not in the world
            _source.ignoreListenerPause = true;  // menus run on timeScale 0
            return _source;
        }
    }

    /// <summary>Marker so a button is only ever wired for sound once.</summary>
    public class UiSfxBound : MonoBehaviour { }
}
