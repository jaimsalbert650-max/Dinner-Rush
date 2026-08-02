using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Persistent account progression (separate from the in-run level). Total XP is earned each run;
    /// the account level and its progress bar are derived from it. Shown in the lobby top bar. #2.
    /// </summary>
    public static class PlayerProfile
    {
        private const string KXp = "acct_xp";
        private const string KName = "acct_name";

        public static int TotalXp => PlayerPrefs.GetInt(KXp, 0);

        /// <summary>
        /// Persistent display name. Generated once ("Player #####") on first read, then editable by
        /// the player via the lobby name pill (tap → rename popup). Empty sets are ignored; trimmed
        /// and capped at 16 chars.
        /// </summary>
        public static string Name
        {
            get
            {
                string n = PlayerPrefs.GetString(KName, "");
                if (string.IsNullOrEmpty(n))
                {
                    n = "Player " + Random.Range(10000, 100000);
                    PlayerPrefs.SetString(KName, n);
                    PlayerPrefs.Save();
                }
                return n;
            }
            set
            {
                string t = (value ?? "").Trim();
                if (t.Length == 0) return;               // ignore empty — keep the current name
                if (t.Length > 16) t = t.Substring(0, 16);
                PlayerPrefs.SetString(KName, t);
                PlayerPrefs.Save();
            }
        }

        /// <summary>XP required to advance FROM the given account level to the next (rising curve).</summary>
        private static int XpForLevel(int level) => 80 + level * 40;

        public static void AddXp(int n)
        {
            if (n <= 0) return;
            PlayerPrefs.SetInt(KXp, TotalXp + n);
            PlayerPrefs.Save();
        }

        public static int Level
        {
            get { Split(out int lvl, out _, out _); return lvl; }
        }

        /// <summary>0..1 progress toward the next account level.</summary>
        public static float LevelProgress
        {
            get { Split(out _, out int into, out int need); return need > 0 ? (float)into / need : 0f; }
        }

        public static int IntoLevel { get { Split(out _, out int into, out _); return into; } }
        public static int NeedForNext { get { Split(out _, out _, out int need); return need; } }

        private static void Split(out int level, out int intoLevel, out int need)
        {
            int xp = TotalXp;
            level = 1;
            while (xp >= XpForLevel(level)) { xp -= XpForLevel(level); level++; }
            intoLevel = xp;
            need = XpForLevel(level);
        }
    }
}
