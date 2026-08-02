using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Daily-reward bookkeeping (spec `05-daily.md`): a 7-day return loop, one claim per device-local
    /// day, day 7 being the jackpot the whole screen is built to make feel close. Persists the streak
    /// position and the last-claimed day in PlayerPrefs; rollover is local midnight.
    ///
    /// Missing a day resets the streak to day 1 — the spec flags forgiving one miss as an open product
    /// question, and that would be a one-line change here.
    /// </summary>
    public static class DailyService
    {
        private const string DayKey = "daily_last_day";
        private const string StreakKey = "daily_streak";

        /// <summary>Coin reward per streak day (spec §Reward table).</summary>
        public static readonly int[] Rewards = { 25, 50, 75, 100, 150, 250, 500 };

        /// <summary>Day 7 pays its coins plus this many gems.</summary>
        public const int JackpotGems = 20;

        private static int Today =>
            (int)(System.DateTime.Now.Date - new System.DateTime(2020, 1, 1)).TotalDays;

        private static int LastClaimDay => PlayerPrefs.GetInt(DayKey, -1);

        /// <summary>The 0-based streak day the player is on / about to claim.</summary>
        public static int Streak => Mathf.Clamp(PlayerPrefs.GetInt(StreakKey, 0), 0, 6);

        /// <summary>True when today's reward hasn't been claimed yet. A clock tampered backwards
        /// (now &lt; lastClaim) withholds the reward rather than punishing beyond that.</summary>
        public static bool CanClaim => LastClaimDay < Today;

        public static int Reward(int day) => Rewards[Mathf.Clamp(day, 0, 6)];

        /// <summary>Whole hours until the next claim unlocks — for the locked CTA's countdown.</summary>
        public static int HoursUntilNext
        {
            get
            {
                if (CanClaim) return 0;
                var tomorrow = System.DateTime.Now.Date.AddDays(1);
                return Mathf.Max(1, Mathf.CeilToInt((float)(tomorrow - System.DateTime.Now).TotalHours));
            }
        }

        /// <summary>Grant today's reward and advance the streak. Returns false if already claimed
        /// today, which doubles as the double-tap guard.</summary>
        public static bool Claim()
        {
            if (!CanClaim) return false;
            int day = Streak;
            MetaProgress.AddCoins(Reward(day));
            if (day == 6) PlayerGems.Add(JackpotGems);
            PlayerPrefs.SetInt(StreakKey, (day + 1) % 7);
            PlayerPrefs.SetInt(DayKey, Today);
            PlayerPrefs.Save();
            return true;
        }
    }
}
