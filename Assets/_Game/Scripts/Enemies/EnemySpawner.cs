using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Spawns roster enemies on a ring around the player. All wave/difficulty state lives in
    /// WaveManager; this reads the current spawn interval + enemy HP/speed, pools enemies, and — when
    /// a "splitter" character dies — pops out a couple of small spawnlings at the death spot.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private WaveManager waves;
        // Must stay outside the camera's view or enemies pop in on screen. The view is a portrait
        // orthographic box: half-height = Camera.orthographicSize (15), half-width = that x aspect
        // (~0.46), so the far corner sits ~16.5 units out — 17 keeps every spawn just off-screen.
        // Move this whenever the camera moves, or the crowd appears out of thin air.
        [SerializeField] private float spawnRadius = 17f;
        [SerializeField] private int maxEnemies = 150;   // cap ability-driven spawns (Summoner/Splitter) to protect perf

        private const float BaseHurtRadius = 0.1f;   // the prefab's collider, at character scale 1

        // Hurtbox radius as a fraction of the drawn body's half-width. Kept at half so the hitbox
        // stays comfortably inside the silhouette — a shot can never connect with empty air — while
        // no longer letting knives sail through the visible body. This is the one number to turn if
        // enemies now die too easily.
        private const float HurtWidthFraction = 0.5f;

        // Crowd spacing around the player. The cook's drawn body is ~0.8 units across, so half of it is
        // the floor every standoff starts from; BodyClearance is how much of the enemy's own half-width
        // is added on top — under 1 their sprites still overlap his edges a little, which reads as a
        // crowd pressing in rather than a tidy circle, while his head and shoulders stay clear.
        // ContactSlack keeps the bite range just past the ring so the front row still does damage.
        private const float PlayerHalfWidth = 0.40f;
        private const float BodyClearance = 0.85f;
        private const float ContactSlack = 0.15f;

        // Holding the crowd further out also lengthens the front row: the ring at ~0.88 seats ~5 bodies
        // where the old 0.64 seated ~3.5, so leaving the roster's dps alone would have raised incoming
        // damage ~40% as a side effect of a visibility fix. Scaled back by the ring-length ratio so the
        // pressure lands where it was before. Turn this to 1 to feel the un-compensated version.
        private const float CrowdDpsScale = 0.72f;

        private ObjectPool<EnemyHealth> _pool;
        private float _timer;

        // How many spawnlings each live enemy still owes on death (pooled instances are reused, so this
        // is overwritten every time an instance is (re)configured).
        private readonly Dictionary<EnemyHealth, int> _splitRemaining = new Dictionary<EnemyHealth, int>();

        private void Awake()
        {
            _pool = new ObjectPool<EnemyHealth>(
                factory: CreateEnemy,
                onGet: e => e.gameObject.SetActive(true),
                onReturn: e => e.gameObject.SetActive(false));
            if (waves == null) waves = FindAnyObjectByType<WaveManager>();
        }

        private void OnEnable() => EnemyMovement.SummonRequested += Summon;
        private void OnDisable() => EnemyMovement.SummonRequested -= Summon;

        /// <summary>Fulfil a Summoner's request: pop a couple of small spawnlings beside it.</summary>
        private void Summon(Vector2 pos, int count)
        {
            if (player == null || waves == null) return;
            if (EnemyMovement.ActiveCount >= maxEnemies) return;   // anti-flood
            EnemyType child = Spawnling();
            for (int i = 0; i < count; i++)
                SpawnConfigured(child, pos + Random.insideUnitCircle * 0.6f);
        }

        private EnemyHealth CreateEnemy()
        {
            EnemyHealth e = Instantiate(enemyPrefab);
            e.OnDied += pos => OnEnemyDied(e, pos);
            return e;
        }

        private void OnEnemyDied(EnemyHealth e, Vector3 pos)
        {
            // Spawn the split children FIRST, while the dying enemy is not yet back in the pool — so
            // Get() never hands its own instance back out as a child. (EnemyHealth.TakeDamage still
            // deactivates this instance right after OnDied returns, which would kill such a child.)
            if (_splitRemaining.TryGetValue(e, out int n) && n > 0)
            {
                _splitRemaining[e] = 0;   // this instance is spent; spawnlings never split again
                if (EnemyMovement.ActiveCount < maxEnemies)   // anti-flood
                {
                    EnemyType child = Spawnling();
                    for (int i = 0; i < n; i++)
                        SpawnConfigured(child, (Vector2)pos + Random.insideUnitCircle * 0.4f);
                }
            }

            _pool.Return(e);
        }

        private void Update()
        {
            if (waves == null || !waves.Running) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            Spawn();
            _timer = waves.CurrentSpawnInterval;
        }

        private void Spawn()
        {
            if (player == null || enemyPrefab == null) return;

            // The cap was only enforced on the ability-driven spawns (Summoner / Splitter); the timed
            // wave spawn ignored it, so a long run grew the crowd without bound and the O(n) separation
            // each enemy runs turned quadratic against a number nothing limited.
            if (EnemyMovement.ActiveCount >= maxEnemies) return;

            // Pick this spawn's character from the roster for the current wave, so the crowd is a mix
            // of ordinary clerks, fast runners, tanks, chargers, bosses… that shifts as waves climb.
            EnemyType type = EnemyRoster.PickForWave(waves.CurrentWave);

            // Spread spawns around a full ring so enemies arrive from every direction and surround.
            Vector2 pos = (Vector2)player.position + Random.insideUnitCircle.normalized * spawnRadius;
            SpawnConfigured(type, pos);
        }

        /// <summary>Get a pooled enemy and apply a character's stats/look/abilities at a world position.</summary>
        private void SpawnConfigured(EnemyType type, Vector2 pos)
        {
            EnemyHealth e = _pool.Get();
            e.transform.position = pos;

            e.Configure(waves.CurrentEnemyHp * type.hpMult);
            _splitRemaining[e] = type.splitCount;

            bool hasVisual = e.TryGetComponent<EnemyVisual>(out var vis);
            if (hasVisual)
            {
                vis.SetAppearance(type.spriteIndex);
                vis.SetVariant(type.ringColor, type.HasRing);
            }
            if (e.TryGetComponent<EnemyAnimator>(out var anim))
                anim.SetBaseScaleMult(type.scale);   // must precede the hurtbox: it sets the drawn size

            // Where this character comes to rest, measured from the body it is actually wearing: the
            // standoff is centre-to-centre, so a flat 0.55 let a ~1.1-unit-wide customer park its
            // sprite right on top of the cook and bury him. Now the drawn half-widths are what decide.
            float standoff = type.standoffOverride > 0f
                ? type.standoffOverride
                : PlayerHalfWidth + (hasVisual ? vis.DrawnHalfWidth : 0.45f * type.scale) * BodyClearance;

            if (e.TryGetComponent<EnemyMovement>(out var mv))
            {
                mv.SetTarget(player);
                mv.SetSpeed(waves.EnemySpeed * type.speedMult);
                // Exploders override to a tiny standoff so they get right up close before detonating.
                mv.SetStandoff(standoff);
                mv.SetAbility(type.ability);
            }

            // Size the hurtbox from the body as drawn. Every enemy used to keep the prefab's fixed
            // 0.1 circle no matter which customer sprite it wore or how big it was drawn, and knives
            // land through that trigger — so a knife crossing the outer half of a visible enemy
            // passed straight through it. Measured before this: a knife reached only 42% of the way
            // across the commonest enemy, and large characters were worse still, because the hurtbox
            // scaled with them while the knife did not.
            if (e.TryGetComponent<CircleCollider2D>(out var col))
                col.radius = hasVisual
                    ? Mathf.Max(BaseHurtRadius, vis.DrawnHalfWidth * HurtWidthFraction)
                    : BaseHurtRadius * type.scale;
            if (e.TryGetComponent<EnemyContactDamage>(out var dmg))
            {
                dmg.SetDamage(type.contactDps * CrowdDpsScale);
                dmg.SetBurst(type.burstDamage);
                // Contact has to reach from wherever the body stops, or a wide character would hold a
                // ring it cannot bite from. Exploders keep a snug range so they still detonate on touch.
                dmg.SetRange(standoff + ContactSlack);
            }
        }

        /// <summary>The small, fast, fragile enemy a splitter bursts into. Never splits again.</summary>
        private static EnemyType Spawnling() => new EnemyType
        {
            name = "Spawnling", spriteIndex = 5, scale = 0.55f, hpMult = 0.25f, speedMult = 1.4f,
            contactDps = 5f, ability = EnemyMovement.Ability.None,
            ringColor = new Color(0.7f, 0.5f, 1f, 0.7f), splitCount = 0
        };
    }
}
