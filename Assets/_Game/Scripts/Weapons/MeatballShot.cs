using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A flaming meatball falling on a customer. It streaks in from off-screen, lands where it was
    /// aimed and bursts, hurting whatever is standing there.
    ///
    /// The art is drawn with its trail up and to the right, so the ball travels down-left and never has
    /// to be rotated — every meteor comes in on the same wind, which is what makes a volley read as one
    /// event instead of five unrelated sprites.
    /// </summary>
    public class MeatballShot : MonoBehaviour
    {
        /// <summary>Where it comes from, relative to where it lands.</summary>
        private static readonly Vector2 Approach = new Vector2(6.5f, 6.5f);
        /// <summary>Slow enough to watch it come down — the dread on the way in is the point of a
        /// meteor, and at a third of a second it was over before it registered.</summary>
        private const float FallSeconds = 0.8f, BurstSeconds = 0.22f;

        // Static, so it outlives a scene load while its GameObjects do not — pops skip the corpses.
        private static readonly Stack<MeatballShot> Pool = new Stack<MeatballShot>();
        private static Sprite[] _frames;

        private SpriteRenderer _sr;
        private Vector2 _to;
        private float _t, _damage, _radius, _size;
        private bool _burst;

        public static void Strike(Vector2 target, float damage, float radius, float size)
        {
            MeatballShot m = null;
            while (m == null && Pool.Count > 0) m = Pool.Pop();
            if (m == null)
            {
                var go = new GameObject("MeatballShot");
                m = go.AddComponent<MeatballShot>();
                m._sr = go.AddComponent<SpriteRenderer>();
                m._sr.sortingOrder = 24;         // falling from above everything
            }
            EnsureArt();

            m._to = target;
            m._damage = damage;
            m._radius = radius;
            m._size = size;
            m._t = 0f;
            m._burst = false;
            m._sr.color = Color.white;
            m.transform.position = target + Approach;
            m.Draw(0);
            m.gameObject.SetActive(true);
        }

        private void Update()
        {
            _t += Time.deltaTime;

            if (!_burst)
            {
                float u = Mathf.Clamp01(_t / FallSeconds);
                // Accelerating, so it lands like a rock rather than drifting in.
                transform.position = Vector2.Lerp(_to + Approach, _to, u * u);
                Draw(Mathf.FloorToInt(_t * 18f));
                if (u >= 1f)
                {
                    _burst = true;
                    _t = 0f;
                    Damage();
                }
                return;
            }

            // Burst: a quick flare-and-vanish where it hit, no second sheet needed.
            float k = Mathf.Clamp01(_t / BurstSeconds);
            float s = _size * Mathf.Lerp(1f, 1.7f, k);
            transform.localScale = new Vector3(s, s, 1f);
            _sr.color = new Color(1f, 1f, 1f, 1f - k);
            if (k >= 1f)
            {
                gameObject.SetActive(false);
                Pool.Push(this);
            }
        }

        /// <summary>Everything standing where it landed. Backwards, because a kill takes the enemy out
        /// of the registry mid-loop.</summary>
        private void Damage()
        {
            float r2 = _radius * _radius;
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null || e.Health == null) continue;
                if (((Vector2)e.transform.position - _to).sqrMagnitude > r2) continue;
                e.Health.TakeDamage(_damage);
            }
        }

        private void Draw(int frame)
        {
            if (_frames == null || _frames.Length == 0) return;
            var s = _frames[((frame % _frames.Length) + _frames.Length) % _frames.Length];
            if (s == null) return;
            _sr.sprite = s;
            transform.localScale = new Vector3(_size, _size, 1f);
        }

        private static void EnsureArt()
        {
            if (_frames != null) return;
            _frames = new Sprite[5];
            for (int i = 0; i < 5; i++) _frames[i] = Resources.Load<Sprite>("fx/meatball_" + (i + 1));
        }
    }
}
