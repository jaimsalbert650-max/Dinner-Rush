using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Tracks the player's owned passive items ("Supplies") and their levels (1..max). Picking a
    /// passive at level-up calls <see cref="AddOrLevel"/>, which bumps the level (capped) and applies
    /// its per-level amount to the shared <see cref="PlayerStats"/>. The passive counterpart to
    /// <see cref="WeaponManager"/>; the pause menu reads <see cref="Owned"/> for the Supplies panel.
    /// </summary>
    public class PassiveInventory : MonoBehaviour
    {
        public PlayerStats Stats { get; set; } = new PlayerStats();

        private readonly Dictionary<StatType, int> _levels = new();
        private readonly List<StatType> _order = new();   // acquisition order, for stable UI slots

        public int LevelOf(StatType stat) => _levels.TryGetValue(stat, out var l) ? l : 0;

        /// <summary>Owned passives, in the order they were first acquired.</summary>
        public IReadOnlyList<StatType> Owned => _order;

        /// <summary>
        /// Grant or level up a passive: bumps its level (capped at maxLevel), applies amountPerLevel to
        /// Stats, and returns the new level — or 0 if it was already maxed (a no-op).
        /// </summary>
        public int AddOrLevel(StatType stat, float amountPerLevel, int maxLevel)
        {
            int lvl = LevelOf(stat);
            if (lvl >= maxLevel) return 0;
            if (lvl == 0) _order.Add(stat);
            lvl++;
            _levels[stat] = lvl;
            Stats?.AddModifier(stat, amountPerLevel);
            return lvl;
        }
    }
}
