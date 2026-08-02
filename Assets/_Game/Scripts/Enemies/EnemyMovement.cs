using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Steers the enemy toward the player each physics step, but STOPS at a standoff distance and
    /// pushes away from nearby enemies (boids-style separation). The result: enemies stream in from
    /// their spawn points and SURROUND the player — standing next to him in a loose ring instead of
    /// all stacking on his exact position — then deal contact damage from there.
    ///
    /// The enemy colliders are triggers on a kinematic body (no physical pushing), so the separation
    /// that spreads the crowd out is done manually here, against a shared registry of live enemies.
    ///
    /// A small ability hook adds per-character movement flavour (dash chargers, weaving zig-zaggers);
    /// stat-only variants (fast / tanky) just use SetSpeed + the size/appearance setters elsewhere.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        public enum Ability { None, Charger, Zigzag, Healer, Ranged, Summoner }

        /// <summary>Raised when a Summoner wants minions spawned at a world point. The spawner fulfils it.</summary>
        public static event System.Action<Vector2, int> SummonRequested;

        [SerializeField] private float speed = 2.2f;
        [SerializeField] private float standoff = 0.5f;           // stops closing in once this near the player
        [SerializeField] private float separationRadius = 1.15f;  // neighbours inside this push it away (~sprite width, so bodies don't overlap)
        [SerializeField] private float separationStrength = 2.4f; // strong enough to actually hold that spacing in a dense crowd

        // Shared registry of live enemies so each one can cheaply push off its neighbours.
        private static readonly List<EnemyMovement> Active = new List<EnemyMovement>(256);

        /// <summary>Live enemy count — the spawner uses it to cap Summoner/Splitter flooding.</summary>
        public static int ActiveCount => Active.Count;

        /// <summary>
        /// Live enemy by index, for code that needs to sweep the crowd. Weapons use this instead of
        /// a physics query: everything on the field is on one layer with no collision filtering, so
        /// `Physics2D.OverlapCircleAll` returned the player and every flying knife too, allocated a
        /// fresh array per shot, and then paid a `GetComponent<EnemyHealth>()` on each hit to throw
        /// the non-enemies away. This registry already holds exactly the enemies, costs nothing to
        /// read, and matches how the rest of the game finds things (contact damage and pickups are
        /// both plain distance checks). Returns null for an out-of-range index.
        /// </summary>
        public static EnemyMovement At(int i) => (uint)i < (uint)Active.Count ? Active[i] : null;

        /// <summary>This enemy's health, cached at Awake.</summary>
        public EnemyHealth Health => _selfHealth;

        // --- per-step position snapshot -------------------------------------------------------
        // Separation is O(n^2) by nature, and at the 150-enemy cap that is ~22k pair checks per
        // physics step. Reading `other._rb.position` and comparing `other == null` inside that loop
        // costs two *native* calls each time — over a million a second — which is what made a full
        // arena crawl on a phone. Snapshotting positions into a plain array once per step turns the
        // inner loop into pure managed float math; the pair count is unchanged but each pair is
        // roughly free. `_idx` lets an enemy skip itself without searching the list.
        private static Vector2[] _snapPos = new Vector2[256];
        private static int _snapCount;
        private static float _snapTime = -1f;
        private int _idx = -1;

        private static void EnsureSnapshot()
        {
            if (_snapTime == Time.fixedTime) return;
            _snapTime = Time.fixedTime;

            int n = Active.Count;
            if (_snapPos.Length < n) _snapPos = new Vector2[Mathf.NextPowerOfTwo(n)];
            for (int i = 0; i < n; i++)
            {
                var a = Active[i];
                // Far-away sentinel for a dead slot: it can never fall inside a separation radius.
                _snapPos[i] = a != null && a._rb != null ? a._rb.position : new Vector2(1e6f, 1e6f);
            }
            _snapCount = n;
        }

        private Rigidbody2D _rb;
        private Transform _target;
        private EnemyHealth _selfHealth;
        private Ability _ability = Ability.None;

        // charger state
        private float _chargeCd;
        private float _chargeT;
        private Vector2 _chargeDir;
        // zigzag state
        private float _weavePhase;
        // healer state
        private float _healCd;
        // ranged state
        private float _shootCd;
        // summoner state
        private float _summonCd;

        private const float HealRadius = 2.5f;
        private const float HealInterval = 1.5f;
        private const float ShootDamage = 8f;
        private const float SummonInterval = 4.5f;

        public void SetTarget(Transform t) => _target = t;
        public void SetSpeed(float s) => speed = s;
        public void SetStandoff(float s) => standoff = Mathf.Max(0.1f, s);

        public void SetAbility(Ability a)
        {
            _ability = a;
            _chargeCd = Random.Range(2.5f, 4.5f);
            _chargeT = 0f;
            _weavePhase = Random.value * 6.28318f;
            _healCd = Random.Range(0.5f, HealInterval);
            _shootCd = Random.Range(0.8f, 2f);
            _summonCd = Random.Range(2f, SummonInterval);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _selfHealth = GetComponent<EnemyHealth>();
            _anim = GetComponent<EnemyAnimator>();
            _flash = GetComponent<HitFlash>();
            _contact = GetComponent<EnemyContactDamage>();
        }

        // --- per-frame work, driven from one place ---------------------------------------------
        // These three used to be ordinary MonoBehaviours with their own `Update`, so at the
        // 150-enemy cap the engine crossed from native code into managed script 450 times a frame
        // just to dispatch them. The registry below already exists, so one `Update` in the scene
        // can walk it and call the same work as direct C# calls.
        private EnemyAnimator _anim;
        private HitFlash _flash;
        private EnemyContactDamage _contact;

        /// <summary>
        /// Runs the per-frame work of every live enemy. Iterates backwards because an enemy can
        /// remove itself mid-loop (an exploder detonating calls TakeDamage → OnDisable), and
        /// OnDisable swaps the last entry into the freed slot: going down means that entry was
        /// already handled, so nothing is skipped. Anything spawned during the loop is appended
        /// past `i` and simply starts next frame.
        /// </summary>
        public static void TickAll(float dt)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (i >= Active.Count) continue;          // list shrank under us
                EnemyMovement e = Active[i];
                if (e == null) continue;

                if (e._flash != null) e._flash.Tick(dt);
                if (e._anim != null) e._anim.Tick(dt);
                if (e._contact != null) e._contact.Tick(dt);   // last: this one can kill the enemy
            }
        }
        private void OnEnable()
        {
            _idx = Active.Count;
            Active.Add(this);
            _chargeT = 0f; _chargeCd = Random.Range(2.5f, 4.5f); _healCd = Random.Range(0.5f, HealInterval);
            _shootCd = Random.Range(0.8f, 2f); _summonCd = Random.Range(2f, SummonInterval);
        }

        /// <summary>Swap-with-last removal so the registry stays index-stable in O(1) — `Active.Remove`
        /// was a linear scan plus a shift, run for every enemy that dies.</summary>
        private void OnDisable()
        {
            int last = Active.Count - 1;
            if (_idx >= 0 && _idx <= last && Active[_idx] == this)
            {
                Active[_idx] = Active[last];
                if (Active[_idx] != null) Active[_idx]._idx = _idx;
                Active.RemoveAt(last);
            }
            else Active.Remove(this);   // registry out of sync (shouldn't happen) — fall back
            _idx = -1;
            _snapTime = -1f;            // the snapshot's indices just changed; force a rebuild
        }

        /// <summary>
        /// One physics step of movement for every live enemy, from a single FixedUpdate in the scene
        /// instead of 150 of them — the same consolidation as <see cref="TickAll"/>, and it matters
        /// more here because a physics step runs 50 times a second regardless of frame rate.
        /// Backwards for the same reason: an enemy may leave the registry mid-step.
        /// </summary>
        public static void FixedTickAll()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (i >= Active.Count) continue;
                EnemyMovement e = Active[i];
                if (e != null) e.Step(dt);
            }
        }

        private void Step(float dt)
        {
            if (_target == null) return;

            Vector2 pos = _rb.position;
            Vector2 toPlayer = (Vector2)_target.position - pos;
            float dist = toPlayer.magnitude;
            Vector2 dirToPlayer = dist > 0.0001f ? toPlayer / dist : Vector2.zero;

            // --- Charger: wind up a straight dash on a cooldown, ignoring the standoff while dashing. ---
            if (_ability == Ability.Charger)
            {
                _chargeCd -= dt;
                if (_chargeT > 0f)
                {
                    _chargeT -= dt;
                    _rb.MovePosition(pos + _chargeDir * (speed * 2.7f) * dt);
                    return;
                }
                if (_chargeCd <= 0f && dist < 6.5f)
                {
                    _chargeDir = dirToPlayer;
                    _chargeT = 0.45f;
                    _chargeCd = Random.Range(3f, 5f);
                }
            }

            // --- Seek: always press toward the player. The personal-space clamp (below) is the wall
            // they can't cross, and separation spaces them out — together those form a natural packed
            // crowd of several rings around the cook, instead of every enemy collapsing onto one thin
            // standoff ring (which just makes them overlap into a clump). ---
            Vector2 seek = dirToPlayer;

            // --- Zigzag: weave sideways across the approach vector for an erratic, dodgy feel. ---
            if (_ability == Ability.Zigzag && seek != Vector2.zero)
            {
                _weavePhase += dt * 7f;
                Vector2 perp = new Vector2(-dirToPlayer.y, dirToPlayer.x);
                seek = (seek + perp * (Mathf.Sin(_weavePhase) * 0.9f)).normalized;
            }

            // --- Healer: on a cooldown, mend nearby allies (a % of their max HP). Moves normally too. ---
            if (_ability == Ability.Healer)
            {
                _healCd -= dt;
                if (_healCd <= 0f)
                {
                    _healCd = HealInterval;
                    float hr2 = HealRadius * HealRadius;
                    for (int i = 0; i < Active.Count; i++)
                    {
                        var o = Active[i];
                        if (o == this || o == null || o._selfHealth == null) continue;
                        if (((Vector2)o._rb.position - pos).sqrMagnitude <= hr2)
                            o._selfHealth.Heal(o._selfHealth.Max * 0.05f);
                    }
                }
            }

            // --- Ranged: hangs back (big standoff) and lobs a shot at the player on a cooldown. ---
            if (_ability == Ability.Ranged)
            {
                _shootCd -= dt;
                if (_shootCd <= 0f && dist < 9f && dist > 1f)
                {
                    _shootCd = Random.Range(1.6f, 2.4f);
                    EnemyProjectile.Spawn(pos, dirToPlayer, 6f, ShootDamage);
                }
            }

            // --- Summoner: on a cooldown, asks the spawner to pop a couple of minions beside it. ---
            if (_ability == Ability.Summoner)
            {
                _summonCd -= dt;
                if (_summonCd <= 0f)
                {
                    _summonCd = SummonInterval;
                    SummonRequested?.Invoke(pos, 2);
                }
            }

            // --- Separation: sum pushes from nearby enemies so the crowd fans out AROUND the player. ---
            EnsureSnapshot();
            Vector2 sep = Vector2.zero;
            float r2 = separationRadius * separationRadius;
            for (int i = 0; i < _snapCount; i++)
            {
                if (i == _idx) continue;
                Vector2 d = pos - _snapPos[i];
                float sq = d.sqrMagnitude;
                if (sq > 0.0001f && sq < r2)
                {
                    float dd = Mathf.Sqrt(sq);
                    // Soft-collision falloff: pushes gently at the edge of the radius but blows up as two
                    // bodies get close, so a real minimum spacing is enforced and they stop overlapping.
                    sep += (d / dd) * Mathf.Min(4f, separationRadius / dd - 1f);
                }
            }

            Vector2 vel = seek * speed + sep * (separationStrength * speed);
            float max = speed * 1.4f;
            if (vel.sqrMagnitude > max * max) vel = vel.normalized * max;

            Vector2 newPos = pos + vel * dt;

            // Personal-space floor around the player: the crowd behind can shove a front-row enemy
            // sideways, but NEVER past the standoff ring onto the cook. Without this the mob's
            // separation pressure crushes the front rows straight onto the player and instantly kills
            // him; with it they hold a ring and deal contact damage from there (survival.io feel).
            Vector2 fromPlayer = newPos - (Vector2)_target.position;
            float fp = fromPlayer.magnitude;
            if (fp < standoff)
            {
                Vector2 outDir = fp > 0.0001f ? fromPlayer / fp
                               : (dirToPlayer.sqrMagnitude > 0.0001f ? -dirToPlayer : Vector2.up);
                newPos = (Vector2)_target.position + outDir * standoff;
            }

            _rb.MovePosition(newPos);
        }
    }
}
