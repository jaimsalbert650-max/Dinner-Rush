using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A world-space HP bar above an enemy, hidden by default and shown only for elites (the spawner
    /// calls <see cref="Show"/>). Built at runtime from white sprites. Sits on the enemy root (which
    /// doesn't rotate/scale), so it stays upright and steady above the animated body. Pool-safe:
    /// hides itself on enable.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private float yOffset = 1.9f;
        [SerializeField] private float width = 1.6f;
        [SerializeField] private float height = 0.18f;

        private EnemyHealth _health;
        private GameObject _root;
        private Transform _fill;
        private bool _shown;

        private void Awake() => _health = GetComponent<EnemyHealth>();

        private void OnEnable() { if (_root != null) _root.SetActive(false); SetShown(false); }   // reset for pooled reuse
        private void OnDisable() => SetShown(false);

        // Only bars that are actually visible need refreshing, and only elites ever show one — so
        // instead of 150 LateUpdate dispatches a frame that all early-out, the shown bars keep
        // themselves in this list and EnemyTicker walks just that.
        private static readonly System.Collections.Generic.List<EnemyHealthBar> Shown =
            new System.Collections.Generic.List<EnemyHealthBar>(16);

        private void SetShown(bool v)
        {
            if (v == _shown) return;
            _shown = v;
            if (v) Shown.Add(this); else Shown.Remove(this);
        }

        /// <summary>Refreshes every visible bar. Called once a frame by <see cref="EnemyTicker"/>.</summary>
        public static void TickShown()
        {
            for (int i = Shown.Count - 1; i >= 0; i--)
            {
                if (i >= Shown.Count) continue;
                EnemyHealthBar b = Shown[i];
                if (b != null) b.UpdateBar();
            }
        }

        /// <summary>
        /// Builds the bar the first time it is actually needed. It used to be built in Awake for every
        /// enemy and hidden immediately — 4 GameObjects, 3 SpriteRenderers and a fresh Sprite each, so
        /// at the 150-enemy cap that was 600 objects and 450 renderers the culling system tracked every
        /// frame for something no ordinary enemy ever shows.
        /// </summary>
        public void Show(bool show)
        {
            if (show && _root == null) Build();
            SetShown(show);
            if (_root != null) _root.SetActive(show);
            if (show) UpdateBar();
        }

        private void Build()
        {
            var sq = Square();
            _root = new GameObject("EliteHealthBar");
            _root.transform.SetParent(transform, false);
            _root.transform.localPosition = new Vector3(0f, yOffset, 0f);

            MakePiece("Border", _root.transform, sq, new Color(0f, 0f, 0f, 0.75f), width + 0.06f, height + 0.06f, 20, 0f);
            MakePiece("Bg", _root.transform, sq, new Color(0.25f, 0.05f, 0.05f, 0.95f), width, height, 21, 0f);
            _fill = MakePiece("Fill", _root.transform, sq, new Color(0.95f, 0.3f, 0.25f, 1f), width, height, 22, 0f).transform;
        }

        private void UpdateBar()
        {
            if (_health == null || _fill == null) return;
            float f = _health.Max > 0f ? Mathf.Clamp01(_health.Current / _health.Max) : 0f;
            _fill.localScale = new Vector3(width * f, height, 1f);
            _fill.localPosition = new Vector3(-width * (1f - f) * 0.5f, 0f, 0f);   // shrink from the right
        }

        private static Transform MakePiece(string name, Transform parent, Sprite s, Color c, float w, float h, int order, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            return go.transform;
        }

        private static Sprite _square;

        /// <summary>Shared across every bar — this used to allocate a fresh Sprite per enemy.</summary>
        private static Sprite Square()
        {
            if (_square != null) return _square;
            var t = Texture2D.whiteTexture;
            return _square = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width);
        }
    }
}
