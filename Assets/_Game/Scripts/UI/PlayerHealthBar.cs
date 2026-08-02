using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A small world-space health bar that floats under the player, built from two sprites
    /// (dark background + green fill) and driven by <see cref="PlayerHealth"/>. The fill shrinks
    /// from the right as HP drops.
    /// </summary>
    public class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, -0.7f, 0f);
        [SerializeField] private float width = 1.1f;
        [SerializeField] private float height = 0.16f;
        [SerializeField] private Color backColor = new Color(0.12f, 0.13f, 0.16f, 0.9f);
        [SerializeField] private Color fillColor = new Color(0.45f, 0.82f, 0.35f);

        private PlayerHealth _health;
        private Transform _fill;

        private void Start()
        {
            _health = GetComponentInParent<PlayerHealth>();
            if (_health == null) _health = FindAnyObjectByType<PlayerHealth>();
            Build();
            if (_health != null)
            {
                _health.OnChanged += OnChanged;
                OnChanged(_health.Current, _health.Max);
            }
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnChanged -= OnChanged;
        }

        private void OnChanged(float cur, float max)
        {
            float f = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            if (_fill == null) return;
            _fill.localScale = new Vector3(width * f, height, 1f);
            _fill.localPosition = new Vector3(-width * 0.5f + width * f * 0.5f, offset.y, 0f);
        }

        private void Build()
        {
            MakePiece("HealthBarBg", new Vector3(0f, offset.y, 0f), new Vector3(width, height, 1f), backColor, 90);
            _fill = MakePiece("HealthBarFill", new Vector3(0f, offset.y, 0f), new Vector3(width, height, 1f), fillColor, 91).transform;
        }

        private GameObject MakePiece(string name, Vector3 lp, Vector3 scale, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lp;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square();
            sr.color = c;
            sr.sortingOrder = order;
            return go;
        }

        private static Sprite Square()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
    }
}
