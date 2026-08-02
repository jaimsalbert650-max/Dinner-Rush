using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A collectible food plate that heals the player. Builds its own procedural plate visual.
    /// Sits until the player comes within pickup radius, then homes in; heals on contact and
    /// returns itself to the pool. Mirrors XpGem's magnet/pool lifecycle.
    /// </summary>
    public class FoodPickup : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float healFraction = 0.15f;   // fraction of Max HP
        [SerializeField] private float magnetSpeed = 11f;
        [SerializeField] private float collectDistance = 0.4f;

        private Transform _player;
        private PlayerHealth _health;
        private PlayerStats _stats;
        private Action<FoodPickup> _despawn;
        private Vector3 _baseScale = Vector3.one;
        private float _popT = 999f;
        private const float PopTime = 0.18f;
        private bool _built;

        public void Init(Transform player, PlayerHealth health, PlayerStats stats, Action<FoodPickup> despawn, Sprite art)
        {
            if (!_built) { BuildVisual(art); _built = true; }
            _player = player;
            _health = health;
            _stats = stats;
            _despawn = despawn;
            _popT = 0f;
        }

        private void Update()
        {
            if (_popT < PopTime)
            {
                _popT += Time.deltaTime;
                float u = Mathf.Clamp01(_popT / PopTime);
                float e = 1f + 2.7f * Mathf.Pow(u - 1f, 3f) + 1.7f * Mathf.Pow(u - 1f, 2f);   // ease-out-back
                transform.localScale = _baseScale * e;
            }
            if (_player == null) return;
            float d = Vector2.Distance(transform.position, _player.position);
            if (d <= collectDistance) { Collect(); return; }

            float radius = _stats != null ? _stats.PickupRadius : 1.5f;
            if (d <= radius)
                transform.position = Vector3.MoveTowards(transform.position, _player.position, magnetSpeed * Time.deltaTime);
        }

        private void Collect()
        {
            if (_health != null) _health.Heal(healFraction * _health.Max);
            _despawn?.Invoke(this);
        }

        private void BuildVisual(Sprite art)
        {
            if (art != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
                sr.enabled = true;
                sr.sprite = art;
                sr.color = Color.white;
                sr.sortingOrder = 5;
                float targetH = 0.7f;
                float sh = art.bounds.size.y;
                float k = sh > 0.0001f ? targetH / sh : 1f;
                transform.localScale = new Vector3(k, k, 1f);
                _baseScale = transform.localScale;
                return;
            }

            var plate = GetComponent<SpriteRenderer>();
            if (plate == null) plate = gameObject.AddComponent<SpriteRenderer>();
            plate.sprite = Circle(48);
            plate.color = new Color(0.96f, 0.96f, 0.98f);   // white plate
            plate.sortingOrder = 4;
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            _baseScale = transform.localScale;

            AddPart("Rim", Circle(48), Vector3.zero, new Vector3(1.12f, 1.12f, 1f), new Color(0.80f, 0.82f, 0.88f), 3);
            AddPart("Food", Circle(48), new Vector3(0f, 0.02f, 0f), new Vector3(0.62f, 0.62f, 1f), new Color(0.93f, 0.45f, 0.28f), 5);
            AddPart("Shine", Circle(48), new Vector3(-0.12f, 0.14f, 0f), new Vector3(0.22f, 0.22f, 1f), new Color(1f, 0.85f, 0.7f), 6);
        }

        private void AddPart(string name, Sprite s, Vector3 lp, Vector3 scale, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lp;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
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
