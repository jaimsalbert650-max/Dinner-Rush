using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A persistent green aura around the player that damages every enemy inside it on each tick.
    /// Radius and damage grow with level. Builds a soft filled circle + a bright ring edge that pulse.
    /// </summary>
    public class AuraWeapon : Weapon
    {
        [SerializeField] private float baseRadius = 1.7f;
        [SerializeField] private float baseDamage = 5f;
        [SerializeField] private Color fillColor = new Color(0.35f, 0.9f, 0.4f, 0.16f);
        [SerializeField] private Color edgeColor = new Color(0.45f, 1f, 0.5f, 0.7f);

        private Transform _fill, _edge;
        private float _phase;

        private float Radius => baseRadius + (Level - 1) * 0.35f;
        private float Damage => baseDamage + (Level - 1) * 3f;

        private void Awake()
        {
            baseInterval = 0.4f;   // ticks ~2.5x/sec (before AttackRate)
            BuildVisual();
        }

        protected override void Fire()
        {
            // Backwards: a kill removes the enemy from the registry mid-sweep.
            Vector2 c = transform.position;
            float rSq = Radius * Radius;
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                EnemyMovement e = EnemyMovement.At(i);
                if (e == null || e.Health == null) continue;
                if (((Vector2)e.transform.position - c).sqrMagnitude > rSq) continue;
                e.Health.TakeDamage(Damage);
            }
        }

        protected override void Update()
        {
            base.Update();
            _phase += Time.deltaTime * 3f;
            float pulse = 1f + Mathf.Sin(_phase) * 0.04f;
            float d = Radius * 2f * pulse;
            if (_fill != null) _fill.localScale = new Vector3(d, d, 1f);
            if (_edge != null) _edge.localScale = new Vector3(d, d, 1f);
        }

        private void BuildVisual()
        {
            _fill = MakePiece("AuraFill", Circle(96, 1f), fillColor, -10).transform;
            _edge = MakePiece("AuraEdge", Circle(96, 0.86f), edgeColor, -9).transform;
        }

        private GameObject MakePiece(string name, Sprite s, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            return go;
        }

        /// <summary>Filled disc when innerFrac=1; a ring/annulus when innerFrac&lt;1. Diameter = 1 unit.</summary>
        private static Sprite Circle(int d, float innerFrac)
        {
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            var cols = new Color32[d * d];
            float rOuter = d * 0.5f - 0.5f;
            float rInner = rOuter * innerFrac;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - d * 0.5f + 0.5f, dy = y - d * 0.5f + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    bool inside = dist <= rOuter && dist >= rInner;
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
                }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
        }
    }
}
