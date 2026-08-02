using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Flaming meatballs, the kitchen's answer to survivor.io's lightning: on a cooldown a volley falls
    /// out of the sky onto customers picked at random, wherever they are — no aiming, no line of sight,
    /// nothing the player steers. Levelling adds meatballs to the volley as well as damage, so it grows
    /// from a nuisance into a rain.
    ///
    /// Random rather than nearest on purpose: the cook's other tools already handle whatever is in his
    /// face, so this one is worth having precisely because it reaches the back of the crowd.
    /// </summary>
    public class MeatballWeapon : Weapon
    {
        [SerializeField] private float range = 8f;
        [SerializeField] private float baseDamage = 16f;
        /// <summary>It lands as a splash, not a dart: everyone standing around the customer it was
        /// aimed at takes the hit too.</summary>
        [SerializeField] private float blastRadius = 2.1f;
        [SerializeField] private float drawnSize = 1.15f;

        private float Damage => baseDamage + (Level - 1) * 6f;
        /// <summary>One more meatball every other level: 1, 1, 2, 2, 3.</summary>
        private int Volley => 1 + (Level - 1) / 2;

        private void Awake() => baseInterval = 2.2f;

        protected override void Fire()
        {
            Vector2 me = transform.position;
            float r2 = range * range;
            int volley = Volley;

            // Reservoir sampling over the live registry: picks `volley` customers uniformly in one pass,
            // without building a list every shot.
            var picked = new Transform[volley];
            int seen = 0;
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null) continue;
                if (((Vector2)e.transform.position - me).sqrMagnitude > r2) continue;

                seen++;
                if (seen <= volley) picked[seen - 1] = e.transform;
                else
                {
                    int slot = Random.Range(0, seen);
                    if (slot < volley) picked[slot] = e.transform;
                }
            }

            for (int i = 0; i < volley; i++)
            {
                if (picked[i] == null) continue;
                MeatballShot.Strike(picked[i].position, Stats.RollDamage(Damage), blastRadius, drawnSize);
            }
        }
    }
}
