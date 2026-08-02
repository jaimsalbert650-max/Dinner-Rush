using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Base for auto-firing weapons. Ticks a cooldown (scaled by the player's AttackRate) and calls
    /// <see cref="Fire"/>. <see cref="Level"/> (1..MaxLevel) scales power; concrete weapons decide how.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        [SerializeField] protected float baseInterval = 1f;
        [SerializeField] protected int maxLevel = 5;

        public int Level { get; protected set; } = 1;
        public int MaxLevel => maxLevel;
        public PlayerStats Stats { get; set; } = new PlayerStats();

        private float _cooldown;

        public virtual void LevelUp() { if (Level < maxLevel) Level++; }

        /// <summary>Effective seconds between fires — base interval sped up by AttackRate.</summary>
        protected virtual float Interval => baseInterval / Mathf.Max(0.05f, Stats != null ? Stats.AttackRate : 1f);

        protected virtual void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;
            _cooldown = Interval;
            Fire();
        }

        protected abstract void Fire();

        /// <summary>Nearest active enemy within range, or null. Reads the live-enemy registry rather
        /// than querying physics — see <see cref="EnemyMovement.At"/> for why.</summary>
        protected Transform NearestEnemy(float range)
        {
            Vector2 from = transform.position;
            float best = range * range;
            Transform nearest = null;
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                EnemyMovement e = EnemyMovement.At(i);
                if (e == null) continue;
                float d = ((Vector2)e.transform.position - from).sqrMagnitude;
                if (d < best) { best = d; nearest = e.transform; }
            }
            return nearest;
        }

        protected static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
