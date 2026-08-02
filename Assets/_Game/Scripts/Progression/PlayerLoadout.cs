using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// The run's pre-game choices: which chef, and which starting tool (specs `03-chef-select.md` and
    /// `04-loadout.md`). Both persist on change, not on START, so an app kill mid-flow keeps the pick.
    ///
    /// A chef is a stat identity, not a costume: the HP / SPD pips Chef Select shows are the same
    /// numbers <see cref="Apply"/> turns into multipliers, so the card never lies. Chef 0 is free; the
    /// rest are bought once with coins.
    /// </summary>
    public static class PlayerLoadout
    {
        public struct Chef
        {
            public string name;
            public string tagline;
            public int hp;          // 1..5, drawn as pips
            public int spd;         // 1..5
            public int cost;        // 0 = free
            public float hpMult;
            public float speedMult;
            public float damageMult;
            public float pickupMult;
        }

        public static readonly Chef[] Chefs =
        {
            new Chef { name = "Line Cook",     tagline = "Balanced and dependable",  hp = 3, spd = 4, cost = 0,
                       hpMult = 1f,    speedMult = 1f,    damageMult = 1f,    pickupMult = 1f },
            new Chef { name = "Pit Master",    tagline = "Tanky. Smells like smoke", hp = 5, spd = 2, cost = 500,
                       hpMult = 1.30f, speedMult = 0.92f, damageMult = 1f,    pickupMult = 1f },
            new Chef { name = "Sous Ninja",    tagline = "Fast hands, faster feet",  hp = 2, spd = 5, cost = 900,
                       hpMult = 0.85f, speedMult = 1.18f, damageMult = 1f,    pickupMult = 1.12f },
            new Chef { name = "Pastry Wizard", tagline = "Sweet and mysterious",     hp = 3, spd = 3, cost = 1500,
                       hpMult = 1f,    speedMult = 1f,    damageMult = 1.25f, pickupMult = 1f },
        };

        public struct Tool
        {
            public string name;
            public string sub;
            public bool locked;
            public System.Type weapon;
            /// <summary>Sprite in `Resources/kit` for the picker. Locked tools show the lock instead,
            /// so they need none until they unlock.</summary>
            public string icon;
        }

        // First entry is the default pick (`SelectedTool` falls back to index 0), so the order is a
        // product decision, not cosmetic.
        public static readonly Tool[] Tools =
        {
            new Tool { name = "Ketchup",     sub = "Squirts sauce", locked = false, weapon = typeof(KetchupWeapon), icon = "tool_ketchup" },
            new Tool { name = "Frying Pan",  sub = "Throws a pan",  locked = false, weapon = typeof(KnifeWeapon),   icon = "tool_pan" },
            new Tool { name = "Ladle",       sub = "Sizzling aura", locked = false, weapon = typeof(AuraWeapon),    icon = "tool_ladle" },
            new Tool { name = "Pepper Mill", sub = "Locked",        locked = true,  weapon = null },
        };

        // ---------- chef ----------

        public static int Selected
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("loadout_chef", 0), 0, Chefs.Length - 1);
            set { PlayerPrefs.SetInt("loadout_chef", Mathf.Clamp(value, 0, Chefs.Length - 1)); PlayerPrefs.Save(); }
        }

        public static Chef Current => Chefs[Selected];

        /// <summary>Chef 0 is free; the rest unlock once with coins and stay unlocked.</summary>
        public static bool Owned(int i) => i == 0 || PlayerPrefs.GetInt("chef_owned_" + i, 0) == 1;

        public static bool Unlock(int i)
        {
            if (i <= 0 || i >= Chefs.Length || Owned(i)) return false;
            if (!MetaProgress.SpendCoins(Chefs[i].cost)) return false;
            PlayerPrefs.SetInt("chef_owned_" + i, 1);
            PlayerPrefs.Save();
            return true;
        }

        // ---------- starting tool ----------

        /// <summary>Bump this whenever <see cref="Tools"/> is reordered. The pick is stored as an index,
        /// so an old save would otherwise select whatever tool now happens to sit at that slot — which is
        /// exactly what happened when ketchup became the default. A stale version reads as "no choice
        /// made yet", so the player gets the current default until they tap a slot themselves.</summary>
        private const int ToolOrderVersion = 2;

        public static int SelectedTool
        {
            get
            {
                if (PlayerPrefs.GetInt("loadout_tool_v", 1) != ToolOrderVersion) return 0;
                return Mathf.Clamp(PlayerPrefs.GetInt("loadout_tool", 0), 0, Tools.Length - 1);
            }
            set
            {
                PlayerPrefs.SetInt("loadout_tool", Mathf.Clamp(value, 0, Tools.Length - 1));
                PlayerPrefs.SetInt("loadout_tool_v", ToolOrderVersion);
                PlayerPrefs.Save();
            }
        }

        public static Tool CurrentTool => Tools[SelectedTool];

        /// <summary>Multiplies the run's starting stats by the chosen chef. Call after meta upgrades.</summary>
        public static void Apply(PlayerStats s)
        {
            if (s == null) return;
            var c = Current;
            s.MaxHP *= c.hpMult;
            s.MoveSpeed *= c.speedMult;
            s.Damage *= c.damageMult;
            s.PickupRadius *= c.pickupMult;
        }
    }
}
