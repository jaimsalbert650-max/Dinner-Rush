namespace DinnerRush
{
    public enum StatType
    {
        // core stats
        MoveSpeed, MaxHP, Damage, AttackRate, PickupRadius,
        // passive items ("Supplies")
        Armor, Regen, Wisdom, Greed, Crit
    }

    /// <summary>
    /// Runtime, mutable stat block for the player. Systems read from it;
    /// upgrades write to it via AddModifier. Plain C# so it is unit-testable.
    /// </summary>
    public class PlayerStats
    {
        public float MoveSpeed = 5f;
        public float MaxHP = 100f;
        public float Damage = 10f;
        public float AttackRate = 1f;      // attacks per second
        public float PickupRadius = 1.5f;

        // passive-item stats
        public float DamageReduction = 0f;   // Apron: fraction of contact damage ignored (0..0.8)
        public float HpRegen = 0f;           // Coffee: HP per second
        public float XpMult = 1f;            // Notepad: XP gain multiplier
        public float CoinMult = 1f;          // Tip Jar: coin gain multiplier
        public float CritChance = 0f;        // Sharp Knife: chance a hit deals CritMult× damage
        public float CritMult = 2f;

        public void AddModifier(StatType type, float amount)
        {
            switch (type)
            {
                case StatType.MoveSpeed: MoveSpeed += amount; break;
                case StatType.MaxHP: MaxHP += amount; break;
                case StatType.Damage: Damage += amount; break;
                case StatType.AttackRate: AttackRate += amount; break;
                case StatType.PickupRadius: PickupRadius += amount; break;
                case StatType.Armor: DamageReduction += amount; break;
                case StatType.Regen: HpRegen += amount; break;
                case StatType.Wisdom: XpMult += amount; break;
                case StatType.Greed: CoinMult += amount; break;
                case StatType.Crit: CritChance += amount; break;
            }
        }

        /// <summary>Roll crit for a single damage instance: CritMult× on a crit, otherwise unchanged.</summary>
        public float RollDamage(float dmg) => UnityEngine.Random.value < CritChance ? dmg * CritMult : dmg;
    }
}
