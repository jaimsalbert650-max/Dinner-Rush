using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Premium gem currency, persisted via PlayerPrefs (mirrors MetaProgress coins). Earned at
    /// run-end based on survival time; <see cref="Spend"/> is ready for a future premium shop. #4.
    /// </summary>
    public static class PlayerGems
    {
        private const string Key = "meta_gems";

        public static int Gems => PlayerPrefs.GetInt(Key, 0);

        public static void Add(int n)
        {
            if (n <= 0) return;
            PlayerPrefs.SetInt(Key, Gems + n);
            PlayerPrefs.Save();
        }

        public static bool Spend(int n)
        {
            if (n <= 0 || Gems < n) return false;
            PlayerPrefs.SetInt(Key, Gems - n);
            PlayerPrefs.Save();
            return true;
        }
    }
}
