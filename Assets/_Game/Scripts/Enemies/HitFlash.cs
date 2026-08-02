using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Briefly tints every sprite of the enemy toward a flash color when it takes damage, then eases
    /// back. Works with multi-part enemies (body + head + limbs). Pool-safe: restores colors on disable.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.08f;

        private EnemyHealth _health;
        private SpriteRenderer[] _renderers;
        private Color[] _base;
        private float _t = -1f;

        private void Awake() => _health = GetComponent<EnemyHealth>();

        // Captured in Start so any runtime-built parts (EnemyVisual, built in Awake) are included.
        private void Start() => Capture();

        private void Capture()
        {
            if (_renderers != null) return;
            var all = GetComponentsInChildren<SpriteRenderer>(true);
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var r in all)
                if (r.gameObject.name != "VariantRing") list.Add(r);   // ring keeps its own colour
            _renderers = list.ToArray();
            _base = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _base[i] = _renderers[i].color;
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDamaged += Flash;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= Flash;
            Restore();
            _t = -1f;
        }

        private void Flash() => _t = 0f;

        private void Restore()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].color = _base[i];
        }

        /// <summary>Driven by <see cref="EnemyMovement.TickAll"/>, not by the engine — see the note there.</summary>
        public void Tick(float dt)
        {
            if (_t < 0f) return;
            Capture();
            _t += dt;
            float u = _t / flashDuration;
            if (u >= 1f) { u = 1f; _t = -1f; }
            Color flash = flashColor;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].color = Color.Lerp(flash, _base[i], u);
        }
    }
}
