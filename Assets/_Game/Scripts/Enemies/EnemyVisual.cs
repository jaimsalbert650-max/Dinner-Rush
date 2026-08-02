using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Gives each enemy a random "customer" look, sliced from the customer art sheet
    /// (castomers_cut.png). Each customer is a single complete sprite, so no assembly is needed —
    /// the root SpriteRenderer just gets one of the regular customers at random. The two labelled
    /// bosses in the sheet are reserved (not used for normal spawns). HitFlash tints the sprite.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyVisual : MonoBehaviour
    {
        [SerializeField] private Texture2D sheet;              // visitirs_cut.png
        [SerializeField] private Color tint = Color.white;
        [SerializeField, Tooltip("Which customer in the sheet every enemy uses (single enemy type).")]
        private int spriteIndex = 0;

        private const float Ppu = 280f;

        // Visitor rects within the sheet (visitirs_cut, 5x2 grid).
        private static readonly Rect[] Customers =
        {
            new Rect(112, 650, 249, 293),   // pilot
            new Rect(507, 650, 277, 265),   // judge
            new Rect(905, 648, 265, 295),   // doctor
            new Rect(1300, 648, 265, 295),  // woman
            new Rect(1726, 643, 273, 290),  // clown
            new Rect(0, 126, 383, 397),     // worker
            new Rect(947, 131, 256, 299),   // chef
            new Rect(1369, 131, 253, 278),  // painter
            new Rect(1762, 135, 235, 280),  // red-haired woman
        };

        private static Texture2D _builtFrom;
        private static Sprite[] _sprites;
        private static Sprite _ringSprite;

        private SpriteRenderer _ring;
        private SpriteRenderer _body;

        /// <summary>Show/hide a coloured ground ring marking a spawn variant (fast/tough).</summary>
        public void SetVariant(Color c, bool show)
        {
            if (_ring == null) return;
            _ring.enabled = show;
            _ring.color = c;
        }

        /// <summary>
        /// Swap which customer sprite this enemy wears — used by the spawner to give each character
        /// type a distinct look on pooled reuse. Colour stays white so HitFlash's cached base tint
        /// (and the flash restore) keep working; use SetVariant for colour-coding instead.
        /// </summary>
        public void SetAppearance(int index)
        {
            if (_body == null || _sprites == null || _sprites.Length == 0) return;
            _body.sprite = _sprites[Mathf.Clamp(index, 0, _sprites.Length - 1)];
        }

        /// <summary>
        /// Half-width of the body as actually drawn, in world units, including the character's
        /// scale. The hurtbox is sized from this so it tracks whatever sprite the enemy is wearing
        /// — the customer sprites differ a lot in width (the widest is ~1.7x the narrowest).
        /// </summary>
        public float DrawnHalfWidth =>
            _body != null && _body.sprite != null
                ? _body.sprite.bounds.extents.x * Mathf.Abs(_body.transform.lossyScale.x)
                : 0.45f;

        private void Awake()
        {
            BuildShared();

            // Put the sprite on a child so EnemyAnimator can bounce/waddle it without moving the
            // physics body (root has the Rigidbody2D + collider). The root renderer is left disabled.
            if (TryGetComponent<SpriteRenderer>(out var rootSr)) rootSr.enabled = false;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(transform, false);
            var sr = bodyGo.AddComponent<SpriteRenderer>();
            if (_sprites != null && _sprites.Length > 0)
                sr.sprite = _sprites[Mathf.Clamp(spriteIndex, 0, _sprites.Length - 1)];
            sr.color = tint;
            sr.sortingOrder = 2;
            _body = sr;

            var ring = new GameObject("VariantRing");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            ring.transform.localScale = new Vector3(1.1f, 0.6f, 1f);
            _ring = ring.AddComponent<SpriteRenderer>();
            _ring.sprite = _ringSprite;
            _ring.color = Color.clear;
            _ring.sortingOrder = 0;
            _ring.enabled = false;
        }

        private void BuildShared()
        {
            if (_builtFrom == sheet && _sprites != null) return;
            _builtFrom = sheet;
            if (sheet == null)
            {
                _sprites = null;
            }
            else
            {
                _sprites = new Sprite[Customers.Length];
                for (int i = 0; i < Customers.Length; i++)
                    _sprites[i] = Sprite.Create(sheet, Customers[i], new Vector2(0.5f, 0.5f), Ppu);
            }
            if (_ringSprite == null) _ringSprite = Circle(64);
        }

        private static Sprite Circle(int d)
        {
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            var cols = new Color32[d * d];
            float r = d * 0.5f - 0.5f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - d * 0.5f + 0.5f, dy = y - d * 0.5f + 0.5f;
                    bool inside = dx * dx + dy * dy <= r * r;
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
                }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
        }
    }
}
