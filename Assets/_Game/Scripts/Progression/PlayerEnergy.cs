using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Energy pool that regenerates in real time, persisted via PlayerPrefs (mirrors MetaProgress).
    /// Starting a run costs <see cref="LobbyScreen"/>'s energy cost; energy refills 1 per
    /// <see cref="RegenSeconds"/> of wall-clock time, capped at <see cref="Max"/>. Sub-project #3.
    /// </summary>
    public static class PlayerEnergy
    {
        public const int Max = 60;
        public const int RegenSeconds = 120;   // 1 energy every 2 minutes (tunable)

        private const string KValue = "energy_value";
        private const string KStamp = "energy_stamp";   // UTC ticks of the last settled regen tick

        /// <summary>Current energy, after applying any regen accrued since last read.</summary>
        public static int Current
        {
            get { Settle(); return Mathf.Clamp(PlayerPrefs.GetInt(KValue, Max), 0, Max); }
        }

        /// <summary>Whole seconds until the next +1 energy (0 when full).</summary>
        public static int SecondsToNext
        {
            get
            {
                Settle();
                if (PlayerPrefs.GetInt(KValue, Max) >= Max) return 0;
                var last = new DateTime(StampTicks(), DateTimeKind.Utc);
                int since = (int)(DateTime.UtcNow - last).TotalSeconds;
                return Mathf.Max(0, RegenSeconds - since);
            }
        }

        /// <summary>Spend n energy; returns false (and changes nothing) if there isn't enough.</summary>
        public static bool Spend(int n)
        {
            Settle();
            int cur = PlayerPrefs.GetInt(KValue, Max);
            if (cur < n) return false;
            bool wasFull = cur >= Max;
            PlayerPrefs.SetInt(KValue, Mathf.Clamp(cur - n, 0, Max));
            if (wasFull) PlayerPrefs.SetString(KStamp, DateTime.UtcNow.Ticks.ToString()); // start the clock
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Grant energy (e.g. from a reward), capped at Max.</summary>
        public static void Add(int n)
        {
            Settle();
            PlayerPrefs.SetInt(KValue, Mathf.Min(Max, PlayerPrefs.GetInt(KValue, Max) + n));
            PlayerPrefs.Save();
        }

        // Apply whatever regen has accrued since the stored timestamp.
        private static void Settle()
        {
            int cur = PlayerPrefs.GetInt(KValue, Max);
            if (cur >= Max) { PlayerPrefs.SetString(KStamp, DateTime.UtcNow.Ticks.ToString()); return; }

            var last = new DateTime(StampTicks(), DateTimeKind.Utc);
            double sec = (DateTime.UtcNow - last).TotalSeconds;
            if (sec < RegenSeconds) return;

            int gained = (int)(sec / RegenSeconds);
            PlayerPrefs.SetInt(KValue, Mathf.Min(Max, cur + gained));
            long consumedTicks = (long)gained * RegenSeconds * TimeSpan.TicksPerSecond;
            PlayerPrefs.SetString(KStamp, (last.Ticks + consumedTicks).ToString());
            PlayerPrefs.Save();
        }

        private static long StampTicks()
        {
            if (long.TryParse(PlayerPrefs.GetString(KStamp, ""), out long t) && t > 0) return t;
            long now = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(KStamp, now.ToString());
            return now;
        }
    }
}
