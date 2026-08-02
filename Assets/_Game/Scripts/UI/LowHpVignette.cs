using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// A red screen-edge vignette that fades in (and pulses) as the player's HP drops below a
    /// threshold. Full-screen radial-alpha Image driven by <see cref="PlayerHealth"/>.
    /// </summary>
    public class LowHpVignette : MonoBehaviour
    {
        [SerializeField] private float threshold = 0.35f;
        [SerializeField] private Color color = new Color(0.85f, 0.06f, 0.06f);

        private PlayerHealth _health;
        private Image _img;
        private float _target;
        private float _phase;

        private void Start()
        {
            _health = FindAnyObjectByType<PlayerHealth>();
            Build();
            if (_health != null)
            {
                _health.OnChanged += OnHp;
                OnHp(_health.Current, _health.Max);
            }
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnChanged -= OnHp;
        }

        private void OnHp(float cur, float max)
        {
            float f = max > 0f ? cur / max : 1f;
            _target = f < threshold ? Mathf.InverseLerp(threshold, 0.08f, f) : 0f;
        }

        private void Update()
        {
            if (_img == null) return;
            _phase += Time.unscaledDeltaTime * 4f;
            float pulse = _target * (0.78f + 0.22f * Mathf.Sin(_phase));
            var c = color;
            c.a = Mathf.Clamp01(pulse) * 0.72f;
            _img.color = c;
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            var go = new GameObject("LowHpVignette", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetSiblingIndex(1);   // over the game, under most HUD/panels
            _img = go.AddComponent<Image>();
            _img.sprite = Vignette(128);
            _img.raycastTarget = false;
            _img.color = new Color(color.r, color.g, color.b, 0f);
            var rt = _img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite Vignette(int d)
        {
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            var cols = new Color32[d * d];
            float half = d * 0.5f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((dist - 0.6f) / 0.55f);
                    a = a * a;
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
        }
    }
}
