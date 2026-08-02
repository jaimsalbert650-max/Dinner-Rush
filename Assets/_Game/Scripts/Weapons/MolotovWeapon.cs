using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// The hot sauce molotov: the cook lobs a bottle into the thickest part of the crowd and leaves a
    /// burning puddle behind. Everything you see of it — the tumbling bottle, the burst, the puddle
    /// catching and burning — is the drawn sheet, played by <see cref="SauceBottle"/>.
    ///
    /// It aims at a *crowd*, not at whoever is nearest: a puddle is worth throwing where several
    /// customers will have to stand in it.
    /// </summary>
    public class MolotovWeapon : Weapon
    {
        [SerializeField] private float throwRange = 7f;
        [SerializeField] private float baseRadius = 1.5f;
        [SerializeField] private float baseDps = 14f;
        [SerializeField] private float burnSeconds = 3.5f;

        private float Radius => baseRadius + (Level - 1) * 0.18f;
        private float Dps => baseDps + (Level - 1) * 5f;

        private void Awake() => baseInterval = 2.6f;

        protected override void Fire()
        {
            Vector2 me = transform.position;
            if (!BestSpot(me, out var spot)) return;      // nothing in range: keep the bottle
            SauceBottle.Throw(me, spot, Radius, Stats.RollDamage(Dps), burnSeconds);
        }

        /// <summary>The spot with the most customers inside one puddle. Falls back to false when the
        /// room is empty, so the cook never throws sauce at nothing.</summary>
        private bool BestSpot(Vector2 from, out Vector2 spot)
        {
            spot = from;
            int best = 0;
            float range2 = throwRange * throwRange, r2 = Radius * Radius;

            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null) continue;
                Vector2 p = e.transform.position;
                if ((p - from).sqrMagnitude > range2) continue;

                int score = 0;
                for (int j = EnemyMovement.ActiveCount - 1; j >= 0; j--)
                {
                    var o = EnemyMovement.At(j);
                    if (o == null) continue;
                    if (((Vector2)o.transform.position - p).sqrMagnitude <= r2) score++;
                }
                if (score > best) { best = score; spot = p; }
            }
            return best > 0;
        }
    }
}
