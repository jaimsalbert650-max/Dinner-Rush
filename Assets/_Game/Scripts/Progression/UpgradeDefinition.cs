using System;
using UnityEngine;

namespace DinnerRush
{
    public enum UpgradeKind { Stat, Weapon, Passive }

    /// <summary>
    /// A level-up choice. Either a <b>Stat</b> boost (adds to a PlayerStats value) or a <b>Weapon</b>
    /// (grants a new weapon or levels an owned one). Authored as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(menuName = "DinnerRush/Upgrade", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public UpgradeKind kind = UpgradeKind.Stat;
        public string displayName = "Upgrade";
        [TextArea] public string description = "";
        public Color accent = new Color(0.55f, 0.85f, 0.30f);
        public Sprite icon;   // shown on the level-up card (optional)

        [Header("Stat / Passive upgrade")]
        public StatType stat = StatType.Damage;
        public float amount = 1f;
        public int maxLevel = 5;   // Passive kind only: cap on how many times it can be taken

        [Header("Weapon upgrade (class name, e.g. MolotovWeapon)")]
        public string weaponTypeName = "";

        public Type WeaponType =>
            string.IsNullOrEmpty(weaponTypeName) ? null : Type.GetType("DinnerRush." + weaponTypeName + ", Assembly-CSharp");

        /// <summary>Apply a stat upgrade to the player's stats (no-op for weapon upgrades).</summary>
        public void ApplyStat(PlayerStats stats)
        {
            if (kind == UpgradeKind.Stat) stats?.AddModifier(stat, amount);
        }
    }
}
