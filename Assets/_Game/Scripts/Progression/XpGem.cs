using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A collectible XP gem. Builds its own green-diamond visual. Sits until the player comes within
    /// pickup radius, then homes in; grants XP on contact and returns itself to the pool.
    /// </summary>
    public class XpGem : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private float magnetSpeed = 11f;
        [SerializeField] private float collectDistance = 0.35f;

        private Transform _player;
        private PlayerExperience _xp;
        private PlayerStats _stats;
        private Action<XpGem> _despawn;
        private Vector3 _baseScale = Vector3.one;
        private float _popT = 999f;
        private const float PopTime = 0.18f;
        private bool _forced;   // once true, homes to the player regardless of pickup radius
        private bool _built;

        public void Init(Transform player, PlayerExperience xp, PlayerStats stats, Action<XpGem> despawn, Sprite art)
        {
            if (!_built) { BuildVisual(art); _built = true; }
            _player = player;
            _xp = xp;
            _stats = stats;
            _despawn = despawn;
            _popT = 0f;     // pop-in on spawn
            _forced = false;
            // On level-up, every gem on the field rushes to the player (classic Survivors magnet).
            if (_xp != null) { _xp.OnLevelUp -= HandleLevelUp; _xp.OnLevelUp += HandleLevelUp; }
        }

        private void HandleLevelUp(int level) => _forced = true;

        private void OnDisable()
        {
            if (_xp != null) _xp.OnLevelUp -= HandleLevelUp;   // safety on pool return
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
            if (_forced || d <= radius)
                transform.position = Vector3.MoveTowards(transform.position, _player.position, magnetSpeed * Time.deltaTime);
        }

        private void Collect()
        {
            // Unsubscribe before granting XP: collecting this gem may itself trigger a level-up, and
            // this gem is already leaving the field. Other live gems stay subscribed and get pulled.
            if (_xp != null) { _xp.OnLevelUp -= HandleLevelUp; _xp.AddXp(value); }
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
                float targetH = 0.55f;
                float sh = art.bounds.size.y;
                float k = sh > 0.0001f ? targetH / sh : 1f;
                transform.localScale = new Vector3(k, k, 1f);
                _baseScale = transform.localScale;
                return;
            }

            var outline = GetComponent<SpriteRenderer>();
            if (outline == null) outline = gameObject.AddComponent<SpriteRenderer>();
            outline.sprite = Diamond(48, 120f);
            outline.color = new Color(0.12f, 0.30f, 0.14f);
            outline.sortingOrder = 4;
            transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            _baseScale = transform.localScale;

            AddPart("Body", Diamond(48, 120f), Vector3.zero, new Vector3(0.87f, 0.87f, 1f), new Color(0.38f, 0.85f, 0.42f), 5);
            AddPart("Shine", Diamond(48, 120f), new Vector3(0f, 0.06f, 0f), new Vector3(0.42f, 0.42f, 1f), new Color(0.75f, 0.98f, 0.72f), 6);
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

        private static Sprite Diamond(int d, float ppu)
        {
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            var cols = new Color32[d * d];
            float c = d * 0.5f - 0.5f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
                    bool inside = dx + dy <= c;
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
                }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
