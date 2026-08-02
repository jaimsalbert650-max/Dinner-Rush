using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Difficulty tiers for a run. The player picks one in the lobby; the spawner reads the selected
    /// tier's multipliers at Awake and game-over scales rewards by it. Higher tiers unlock once the
    /// player's best survival time clears a threshold. PlayerPrefs-backed. #5.
    /// </summary>
    public static class StageConfig
    {
        public struct Tier
        {
            public string name;
            public string desc;
            public float hpMult;
            public float spdMult;
            public float spawnRateMult;   // >1 = enemies arrive faster
            public float rewardMult;      // coins + gems earned scaled by this
            public float unlockTime;      // best survival seconds required to unlock
        }

        public static readonly Tier[] Tiers =
        {
            new Tier { name = "1. Rush Hour Diner",  desc = "A normal lunch service.",             hpMult = 1.0f, spdMult = 1.00f, spawnRateMult = 1.00f, rewardMult = 1.0f, unlockTime = 0f },
            new Tier { name = "2. Dinner Rush",      desc = "Tougher, faster crowd. +50% rewards.", hpMult = 1.5f, spdMult = 1.15f, spawnRateMult = 1.25f, rewardMult = 1.5f, unlockTime = 120f },
            new Tier { name = "3. Kitchen Nightmare", desc = "Brutal wave of visitors. +150% rewards.", hpMult = 2.4f, spdMult = 1.35f, spawnRateMult = 1.6f, rewardMult = 2.5f, unlockTime = 240f },
        };

        public static int Selected
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("stage_tier", 0), 0, Tiers.Length - 1);
            set { PlayerPrefs.SetInt("stage_tier", Mathf.Clamp(value, 0, Tiers.Length - 1)); PlayerPrefs.Save(); }
        }

        public static Tier Current => Tiers[Mathf.Clamp(Selected, 0, Tiers.Length - 1)];

        public static bool Unlocked(int i)
            => i >= 0 && i < Tiers.Length && PlayerPrefs.GetFloat("meta_best_time", 0f) >= Tiers[i].unlockTime;
    }
}
