using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Decaying camera shake. Accumulates "trauma" from <see cref="Shake"/> and exposes an additive
    /// <see cref="Offset"/> each frame; <see cref="CameraFollow"/> applies it after positioning so the
    /// two layer cleanly. Uses unscaled time so the shake keeps moving during hit-stop.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float maxOffset = 0.45f;
        [SerializeField] private float recover = 1.7f;   // trauma lost per second

        private float _trauma;
        private int _seed;

        public Vector3 Offset { get; private set; }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Add trauma (0..1). Kills call this. Trauma is clamped so it never runs away.</summary>
        public static void Shake(float amount)
        {
            if (Instance != null) Instance._trauma = Mathf.Clamp01(Instance._trauma + amount);
        }

        private void Update()
        {
            if (_trauma <= 0f) { Offset = Vector3.zero; return; }
            Vector2 o = Random.insideUnitCircle * _trauma * maxOffset;
            Offset = new Vector3(o.x, o.y, 0f);
            _trauma = Mathf.Max(0f, _trauma - recover * Time.unscaledDeltaTime);
        }
    }
}
