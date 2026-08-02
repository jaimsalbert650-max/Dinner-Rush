using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Spaghetti tornado: on a cooldown the cook throws up a vortex that circles him, hoovers customers
    /// into its throat, grinds them while it spins and then hurls them back out.
    ///
    /// Crowd control rather than damage: its job is to take a knot of the crowd off the cook for a few
    /// seconds and drop them somewhere else, so levelling widens it and keeps it up longer rather than
    /// only hitting harder.
    /// </summary>
    public class TornadoWeapon : Weapon
    {
        [SerializeField] private float baseRadius = 2.2f;
        [SerializeField] private float baseDps = 10f;
        [SerializeField] private float baseThrowDamage = 14f;
        [SerializeField] private float baseLiveSeconds = 3.5f;

        private float Radius => baseRadius + (Level - 1) * 0.22f;
        private float Dps => baseDps + (Level - 1) * 4f;
        private float ThrowDamage => baseThrowDamage + (Level - 1) * 5f;
        private float LiveSeconds => baseLiveSeconds + (Level - 1) * 0.4f;

        private void Awake() => baseInterval = 8f;

        protected override void Fire() =>
            Tornado.Summon(transform, Radius, Dps, ThrowDamage, LiveSeconds);
    }
}
