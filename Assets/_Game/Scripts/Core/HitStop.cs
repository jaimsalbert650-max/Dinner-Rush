using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Brief "hit-stop": dips Time.timeScale for a fraction of a second on impact for punch, then
    /// restores it. Guards against the level-up pause (timeScale 0) so it never un-pauses that screen,
    /// and ignores overlapping requests so a dense swarm can't stack into a long freeze.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }

        [SerializeField] private float freezeScale = 0.05f;

        private float _restoreAt = -1f;

        private void Awake() { Instance = this; }
        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; if (Time.timeScale == freezeScale) Time.timeScale = 1f; }
        }

        public static void Do(float seconds)
        {
            if (Instance != null) Instance.Begin(seconds);
        }

        private void Begin(float seconds)
        {
            if (_restoreAt >= 0f) return;          // already freezing — don't stack
            if (Time.timeScale == 0f) return;      // paused (level-up) — leave it alone
            Time.timeScale = freezeScale;
            _restoreAt = Time.unscaledTime + seconds;
        }

        private void Update()
        {
            if (_restoreAt < 0f) return;
            if (Time.unscaledTime >= _restoreAt)
            {
                if (Mathf.Approximately(Time.timeScale, freezeScale)) Time.timeScale = 1f;
                _restoreAt = -1f;
            }
        }
    }
}
