using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Deals damage to the player on contact. Distance-based (the enemy's own collider is tiny and
    /// layers aren't set up), so it needs no physics wiring.
    ///
    /// Two modes, chosen per-character by the spawner:
    ///  - continuous (default): drains the player while touching, at damagePerSecond.
    ///  - burst / exploder (burstDamage &gt; 0): on first contact it detonates — one big hit to the
    ///    player, then it kills itself. Set via SetBurst; pool-safe (the fired flag resets on enable).
    /// </summary>
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 9f;
        [SerializeField] private float range = 0.7f;
        [SerializeField] private float burstDamage = 0f;   // >0 turns this enemy into a one-shot exploder

        // The player is the same object for every enemy, so the lookup is shared. It used to be one
        // cached reference per instance, which meant each newly spawned enemy ran its own
        // FindObjectOfType on its first frame — a batch of 150 spawns cost ~83 ms in one tick,
        // and the cost grows with the scene. The frame guard stops a missing player (between runs,
        // or after death) turning into one search per enemy per frame.
        private static Transform _player;
        private static PlayerHealth _health;
        private static int _searchedFrame = -1;

        private EnemyHealth _self;
        private bool _exploded;

        /// <summary>Set how hard this enemy hits — used by the spawner for per-character variants.</summary>
        public void SetDamage(float dps) => damagePerSecond = Mathf.Max(0f, dps);

        /// <summary>Turn this enemy into a kamikaze that detonates on contact (0 = normal contact damage).</summary>
        public void SetBurst(float b) => burstDamage = Mathf.Max(0f, b);

        /// <summary>How close counts as touching. The spawner derives it from where this character's
        /// body actually comes to rest (its standoff), so a wide customer still reaches the cook
        /// instead of standing in his face doing nothing.</summary>
        public void SetRange(float r) => range = Mathf.Max(0.2f, r);

        private void Awake() => _self = GetComponent<EnemyHealth>();
        private void OnEnable() => _exploded = false;   // pool-safe: a reused exploder can detonate again

        private static void EnsureRefs()
        {
            if (_health != null) return;
            if (_searchedFrame == Time.frameCount) return;
            _searchedFrame = Time.frameCount;
            _health = FindAnyObjectByType<PlayerHealth>();
            _player = _health != null ? _health.transform : null;
        }

        /// <summary>Driven by <see cref="EnemyMovement.TickAll"/>, not by the engine — see the note there.</summary>
        public void Tick(float dt)
        {
            EnsureRefs();
            if (_player == null || _health.IsDead) return;

            bool inRange = Vector2.Distance(transform.position, _player.position) <= range;

            if (burstDamage > 0f)
            {
                if (!_exploded && inRange)
                {
                    _exploded = true;
                    _health.Damage(burstDamage);                 // one big hit…
                    if (_self != null) _self.TakeDamage(_self.Current);   // …then blow up (drops + death fx + pool return)
                }
                return;
            }

            if (inRange)
                _health.Damage(damagePerSecond * dt);
        }
    }
}
