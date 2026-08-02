using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Simple achievement-style quests derived from persisted stats (account level, best time, total
    /// kills). Each can be claimed once complete for a gem reward. PlayerPrefs-backed. #7.
    /// </summary>
    public static class PlayerQuests
    {
        public struct Quest
        {
            public string id;
            public string desc;
            public int target;
            public int reward;
            public Func<int> current;
        }

        public static Quest[] All => new[]
        {
            Mk("lvl3", "Reach account Level 3", 3, 8, () => PlayerProfile.Level),
            Mk("surv4", "Survive 4:00 in one run", 240, 8, () => (int)PlayerPrefs.GetFloat("meta_best_time", 0f)),
            Mk("kill200", "Defeat 200 visitors", 200, 10, () => PlayerPrefs.GetInt("total_kills", 0)),
            Mk("lvl6", "Reach account Level 6", 6, 15, () => PlayerProfile.Level),
        };

        private static Quest Mk(string id, string d, int t, int r, Func<int> c)
            => new Quest { id = id, desc = d, target = t, reward = r, current = c };

        public static bool Claimed(string id) => PlayerPrefs.GetInt("q_" + id, 0) == 1;
        public static bool Complete(Quest q) => q.current() >= q.target;

        public static bool Claim(Quest q)
        {
            if (!Complete(q) || Claimed(q.id)) return false;
            PlayerPrefs.SetInt("q_" + q.id, 1);
            PlayerGems.Add(q.reward);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Number of quests that are complete but not yet claimed (drives the lobby badge).</summary>
        public static int UnclaimedCount()
        {
            int n = 0;
            foreach (var q in All) if (Complete(q) && !Claimed(q.id)) n++;
            return n;
        }
    }
}
