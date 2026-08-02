using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A shot fired by a ranged enemy (Gunner). Flies in a straight line and damages the player once
    /// on proximity, then despawns; also despawns after a short lifetime. Built procedurally (small
    /// coloured disc) so it needs no prefab. Uses distance-based hit detection like EnemyContactDamage.
    ///
    /// Pooled and ticked centrally. It used to `new GameObject` per shot and `Destroy` it on hit or
    /// timeout — with a crowd of Gunners that is a steady stream of object creation and destruction,
    /// which on Android shows up as collection pauses (stutter) rather than a lower average frame
    /// rate. Instances are now reused, and one static tick moves them all instead of each carrying
    /// its own Update.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        private Vector2 _vel;
        private float _life;
        private float _damage;
        private const float HitRadius = 0.35f;

        private static PlayerHealth _player;
        private static Sprite _disc;

        private static readonly List<EnemyProjectile> Live = new List<EnemyProjectile>(64);
        private static readonly Stack<EnemyProjectile> Pool = new Stack<EnemyProjectile>(64);

        public static void Spawn(Vector2 pos, Vector2 dir, float speed, float damage)
        {
            // The pool is static, so after a scene reload it still holds instances that were
            // destroyed with the old scene — skip past those rather than handing one out.
            EnemyProjectile p = null;
            while (p == null && Pool.Count > 0) p = Pool.Pop();
            if (p == null) p = Create();

            p.transform.position = pos;
            p._vel = dir.sqrMagnitude > 0.0001f ? dir.normalized * speed : Vector2.up * speed;
            p._damage = damage;
            p._life = 4f;
            p.gameObject.SetActive(true);
            Live.Add(p);
        }

        private static EnemyProjectile Create()
        {
            var go = new GameObject("EnemyShot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Disc();
            sr.color = new Color(1f, 0.55f, 0.15f, 1f);
            sr.sortingOrder = 4;
            go.transform.localScale = Vector3.one * 0.4f;
            return go.AddComponent<EnemyProjectile>();
        }

        /// <summary>Moves every live shot. Called once a frame by <see cref="EnemyTicker"/>.</summary>
        public static void TickAll(float dt)
        {
            if (Live.Count == 0) return;
            if (_player == null) _player = FindAnyObjectByType<PlayerHealth>();
            bool canHit = _player != null && !_player.IsDead;
            Vector2 target = canHit ? (Vector2)_player.transform.position : Vector2.zero;

            for (int i = Live.Count - 1; i >= 0; i--)
            {
                EnemyProjectile p = Live[i];
                if (p == null) { Live.RemoveAt(i); continue; }

                p.transform.position += (Vector3)(p._vel * dt);
                p._life -= dt;

                if (p._life <= 0f) { p.Recycle(i); continue; }

                if (canHit && ((Vector2)p.transform.position - target).sqrMagnitude <= HitRadius * HitRadius)
                {
                    _player.Damage(p._damage);
                    p.Recycle(i);
                }
            }
        }

        private void Recycle(int liveIndex)
        {
            Live.RemoveAt(liveIndex);
            gameObject.SetActive(false);
            Pool.Push(this);
        }

        private static Sprite Disc()
        {
            if (_disc != null) return _disc;
            const int d = 32;
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            var cols = new Color32[d * d];
            float r = d * 0.5f - 1f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - d * 0.5f + 0.5f, dy = y - d * 0.5f + 0.5f;
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(dx * dx + dy * dy <= r * r ? 255 : 0));
                }
            tex.SetPixels32(cols); tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
            return _disc;
        }
    }
}
