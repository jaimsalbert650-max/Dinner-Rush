using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Persistent meta-progression (PlayerPrefs): banked coins carried between runs, and the permanent
    /// upgrades bought with them. The table, costs and stacking follow spec `07-upgrades.md`:
    /// cost = base x (level + 1), max level 6, and effects stack **multiplicatively** with the chef's
    /// base stats. Resolved once at run start by <see cref="Apply"/>, never read per-frame.
    /// </summary>
    public static class MetaProgress
    {
        public enum Meta { MaxHealth, MoveSpeed, CoinMagnet, PanPower, LuckyTips }

        public const int MaxLevel = 6;

        private static readonly int[] BaseCost = { 250, 400, 150, 350, 300 };
        private static readonly string[] DisplayNames = { "Max Health", "Move Speed", "Coin Magnet", "Pan Power", "Lucky Tips" };
        private static readonly string[] Descriptions =
        {
            "+5% HP per level", "+3% speed per level", "+10% pickup range",
            "+4% damage per level", "+5% coin drops",
        };
        /// <summary>Per-level multiplier added to 1.0 for each upgrade, in enum order.</summary>
        private static readonly float[] PerLevel = { 0.05f, 0.03f, 0.10f, 0.04f, 0.05f };

        private const string CoinsKey = "meta_coins";
        private const string MigratedKey = "meta_migrated_v2";
        private static string LvlKey(Meta m) => "meta_lvl_" + m;

        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);
        public static int Level(Meta m) { Migrate(); return PlayerPrefs.GetInt(LvlKey(m), 0); }

        public static string Name(Meta m) => DisplayNames[(int)m];
        public static string Describe(Meta m) => Descriptions[(int)m];

        /// <summary>Bank coins collected this run (call on death).</summary>
        public static void AddCoins(int n)
        {
            if (n <= 0) return;
            PlayerPrefs.SetInt(CoinsKey, Coins + n);
            PlayerPrefs.Save();
        }

        public static bool SpendCoins(int n)
        {
            if (n <= 0 || Coins < n) return false;
            PlayerPrefs.SetInt(CoinsKey, Coins - n);
            PlayerPrefs.Save();
            return true;
        }

        public static int Cost(Meta m) => BaseCost[(int)m] * (Level(m) + 1);

        public static bool IsMax(Meta m) => Level(m) >= MaxLevel;

        public static bool CanBuy(Meta m) => !IsMax(m) && Coins >= Cost(m);

        public static bool Buy(Meta m)
        {
            if (!CanBuy(m)) return false;
            int cost = Cost(m);
            PlayerPrefs.SetInt(CoinsKey, Coins - cost);
            PlayerPrefs.SetInt(LvlKey(m), Level(m) + 1);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Multiplier this upgrade currently applies (1.0 at level 0).</summary>
        public static float Multiplier(Meta m) => 1f + PerLevel[(int)m] * Level(m);

        /// <summary>Fold every purchased upgrade into a fresh run's stats.</summary>
        public static void Apply(PlayerStats stats)
        {
            if (stats == null) return;
            stats.MaxHP *= Multiplier(Meta.MaxHealth);
            stats.MoveSpeed *= Multiplier(Meta.MoveSpeed);
            stats.PickupRadius *= Multiplier(Meta.CoinMagnet);
            stats.Damage *= Multiplier(Meta.PanPower);
            stats.CoinMult *= Multiplier(Meta.LuckyTips);
        }

        /// <summary>The three pre-spec upgrades used different ids; carry their levels over once so a
        /// player's purchases are not silently wiped by the rename.</summary>
        private static void Migrate()
        {
            if (PlayerPrefs.GetInt(MigratedKey, 0) == 1) return;
            PlayerPrefs.SetInt(MigratedKey, 1);
            Carry("meta_lvl_MaxHP", Meta.MaxHealth);
            Carry("meta_lvl_MoveSpeed", Meta.MoveSpeed);
            Carry("meta_lvl_Damage", Meta.PanPower);
            PlayerPrefs.Save();
        }

        private static void Carry(string oldKey, Meta to)
        {
            int lvl = PlayerPrefs.GetInt(oldKey, 0);
            if (lvl <= 0) return;
            PlayerPrefs.SetInt(LvlKey(to), Mathf.Min(lvl, MaxLevel));
            PlayerPrefs.DeleteKey(oldKey);
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(CoinsKey);
            foreach (Meta m in System.Enum.GetValues(typeof(Meta))) PlayerPrefs.DeleteKey(LvlKey(m));
            PlayerPrefs.Save();
        }
    }
}
