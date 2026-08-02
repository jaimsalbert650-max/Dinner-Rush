using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A collectible gold coin (currency). Builds its own visual; homes to the player within pickup
    /// radius and adds to <see cref="GameStats.Coins"/> on contact, then returns to its pool.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private float magnetSpeed = 11f;
        [SerializeField] private float collectDistance = 0.35f;

        private Transform _player;
        private PlayerStats _stats;
        private Action<Coin> _despawn;
        private Vector3 _baseScale = Vector3.one;
        private float _popT = 999f;
        private const float PopTime = 0.18f;
        private bool _built;

        public void Init(Transform player, PlayerStats stats, Action<Coin> despawn, Sprite art)
        {
            if (!_built) { BuildVisual(art); _built = true; }
            _player = player;
            _stats = stats;
            _despawn = despawn;
            _popT = 0f;   // pop-in on spawn
        }

        private void Update()
        {
            if (_popT < PopTime)
            {
                _popT += Time.deltaTime;
                float u = Mathf.Clamp01(_popT / PopTime);
                float e = 1f + 2.7f * Mathf.Pow(u - 1f, 3f) + 1.7f * Mathf.Pow(u - 1f, 2f);
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
            if (GameStats.Instance != null) GameStats.Instance.AddCoin(value);
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
                float targetH = 0.5f;
                float sh = art.bounds.size.y;
                float k = sh > 0.0001f ? targetH / sh : 1f;
                transform.localScale = new Vector3(k, k, 1f);
                _baseScale = transform.localScale;
                return;
            }

            var outline = GetComponent<SpriteRenderer>();
            if (outline == null) outline = gameObject.AddComponent<SpriteRenderer>();
            outline.sprite = Circle(48);
            outline.color = new Color(0.5f, 0.36f, 0.06f);
            outline.sortingOrder = 4;
            transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            _baseScale = transform.localScale;

            AddPart("Body", Circle(48), Vector3.zero, new Vector3(0.82f, 0.82f, 1f), new Color(0.98f, 0.80f, 0.20f), 5);
            AddPart("Shine", Circle(48), new Vector3(-0.1f, 0.1f, 0f), new Vector3(0.3f, 0.3f, 1f), new Color(1f, 0.96f, 0.66f), 6);
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
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(dx * dx + dy * dy <= r * r ? 255 : 0));
                }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
        }
    }
}
