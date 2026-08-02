using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>A small homing missile: curves toward its target, damages it on contact with a small
    /// impact VFX, then despawns. Pooled by the weapon that fires it.</summary>
    public class Missile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        private const float Life = 2.5f, HitDist = 0.4f;

        private float _damage, _t;
        private bool _alive;
        private Transform _target;
        private Vector2 _dir;
        private Action<Missile> _despawn;

        private void Awake() => BuildVisual();

        public void Launch(Vector3 from, Transform target, float damage, Action<Missile> despawn)
        {
            transform.position = from;
            _target = target;
            _damage = damage;
            _despawn = despawn;
            _t = 0f;
            _alive = true;
            _dir = target != null ? ((Vector2)(target.position - from)).normalized : Vector2.up;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_alive) return;
            _t += Time.deltaTime;
            if (_t >= Life) { Despawn(); return; }

            if (_target != null && _target.gameObject.activeSelf)
                _dir = Vector2.Lerp(_dir, ((Vector2)(_target.position - transform.position)).normalized, 0.18f).normalized;

            transform.position += (Vector3)(_dir * speed * Time.deltaTime);
            transform.right = _dir;

            if (_target != null && _target.gameObject.activeSelf &&
                ((Vector2)(_target.position - transform.position)).sqrMagnitude <= HitDist * HitDist)
            {
                var eh = _target.GetComponent<EnemyHealth>();
                if (eh != null) eh.TakeDamage(_damage);
                VfxSpawner.Play(VfxKind.Impact, transform.position);
                Despawn();
            }
        }

        // Deactivate once; without this a timed-out missile stays active and re-despawns every frame,
        // bloating the pool with duplicate references.
        private void Despawn()
        {
            if (!_alive) return;
            _alive = false;
            gameObject.SetActive(false);
            _despawn?.Invoke(this);
        }

        private void BuildVisual()
        {
            MakeSR("Body", RoundedRect(22, 10, 4), new Color(0.85f, 0.28f, 0.22f), new Vector3(1f, 1f, 1f), 0);
            MakeSR("Tail", RoundedRect(10, 8, 3), new Color(1f, 0.72f, 0.25f), new Vector3(0.5f, 0.8f, 1f), 1, new Vector3(-0.11f, 0f, 0f));
            MakeSR("Nose", Circle(12), new Color(0.97f, 0.95f, 0.9f), new Vector3(0.09f, 0.09f, 1f), 1, new Vector3(0.1f, 0f, 0f));
        }

        private void MakeSR(string name, Sprite s, Color c, Vector3 scale, int order, Vector3 lp = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lp;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = 20 + order;
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
            tex.SetPixels32(cols); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite RoundedRect(int w, int h, int radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cols = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, radius, w - 1 - radius), cy = Mathf.Clamp(y, radius, h - 1 - radius);
                    float dx = x - cx, dy = y - cy;
                    cols[y * w + x] = new Color32(255, 255, 255, (byte)(dx * dx + dy * dy <= radius * radius ? 255 : 0));
                }
            tex.SetPixels32(cols); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
