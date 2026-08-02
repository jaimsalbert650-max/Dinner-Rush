using System;

namespace DinnerRush
{
    /// <summary>
    /// Pure C# XP curve. Tracks total XP as a current level plus progress into that level.
    /// The XP required for each level grows geometrically. Unit-testable (no UnityEngine).
    /// </summary>
    public class LevelProgression
    {
        private readonly int _baseXp;
        private readonly double _growth;

        public int Level { get; private set; } = 1;
        public int XpIntoLevel { get; private set; }
        public int XpForNextLevel { get; private set; }

        public LevelProgression(int baseXp = 5, double growth = 1.3)
        {
            _baseXp = Math.Max(1, baseXp);
            _growth = Math.Max(1.0, growth);
            XpForNextLevel = Needed(Level);
        }

        private int Needed(int level) => Math.Max(1, (int)Math.Round(_baseXp * Math.Pow(_growth, level - 1)));

        /// <summary>Add XP. Returns how many levels were gained (0 if none).</summary>
        public int Add(int amount)
        {
            if (amount <= 0) return 0;
            int gained = 0;
            XpIntoLevel += amount;
            while (XpIntoLevel >= XpForNextLevel)
            {
                XpIntoLevel -= XpForNextLevel;
                Level++;
                gained++;
                XpForNextLevel = Needed(Level);
            }
            return gained;
        }

        /// <summary>Progress toward the next level, 0..1.</summary>
        public float Progress01 => (float)XpIntoLevel / XpForNextLevel;
    }
}
