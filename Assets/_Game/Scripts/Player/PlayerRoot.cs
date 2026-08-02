using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Owns the single PlayerStats instance and injects it into every player system,
    /// so movement and weapon all read the same live, upgradeable numbers.
    /// Runs before other player components (negative execution order) so meta/loadout bonuses are
    /// applied to Stats.MaxHP before PlayerHealth seeds its starting HP from it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerRoot : MonoBehaviour
    {
        public PlayerStats Stats { get; } = new PlayerStats();

        private void Awake()
        {
            MetaProgress.Apply(Stats);   // permanent between-run upgrades
            PlayerLoadout.Apply(Stats);  // chosen starting perk (#8)
            if (TryGetComponent<PlayerMovement>(out var move)) move.Stats = Stats;
            if (TryGetComponent<KnifeWeapon>(out var weapon)) weapon.Stats = Stats;
            // Passive-item inventory ("Supplies"); auto-added so it needs no prefab wiring.
            var passives = GetComponent<PassiveInventory>();
            if (passives == null) passives = gameObject.AddComponent<PassiveInventory>();
            passives.Stats = Stats;
        }
    }
}
