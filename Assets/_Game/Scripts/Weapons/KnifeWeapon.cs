using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Throws knives at the nearest enemy on a cadence set by AttackRate. Each level adds another
    /// knife thrown in a fan (the reference's "Kunai +1 kunai"). Pools projectiles.
    /// </summary>
    public class KnifeWeapon : Weapon
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float targetRange = 8f;
        [SerializeField] private float spreadDegrees = 14f;

        /// <summary>Raised each time knives are thrown (drives the attack animation).</summary>
        public event System.Action OnFired;

        private ObjectPool<Projectile> _pool;

        private void Awake()
        {
            baseInterval = 0.8f;   // snappier base attack
            _pool = new ObjectPool<Projectile>(
                factory: () => Instantiate(projectilePrefab),
                onGet: p => p.gameObject.SetActive(true),
                onReturn: p => p.gameObject.SetActive(false));
        }

        protected override void Fire()
        {
            Transform target = NearestEnemy(targetRange);
            if (target == null) return;

            Vector2 baseDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            int count = Level;
            float total = spreadDegrees * (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = count <= 1 ? 0f : -total * 0.5f + total * i / (count - 1);
                Vector2 dir = Rotate(baseDir, angle);
                Projectile p = _pool.Get();
                p.transform.position = transform.position;
                p.Launch(dir, Stats.RollDamage(Stats.Damage), proj => _pool.Return(proj));   // Sharp Knife rolls crit per spatula
            }
            OnFired?.Invoke();
        }
    }
}
