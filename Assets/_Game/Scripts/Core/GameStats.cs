using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Run-wide stats: survival time and kill count. Singleton, read by the HUD.
    /// Time advances with the scaled clock, so it pauses during the level-up screen.
    /// </summary>
    public class GameStats : MonoBehaviour
    {
        public static GameStats Instance { get; private set; }

        public int Kills { get; private set; }
        public int Coins { get; private set; }
        public float Elapsed { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Kills = 0;
            Coins = 0;
            Elapsed = 0f;
        }

        public void AddCoin(int amount = 1) => Coins += amount;

        private void OnEnable() => EnemyHealth.AnyKilled += OnKill;
        private void OnDisable() => EnemyHealth.AnyKilled -= OnKill;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnKill() => Kills++;

        private void Update() => Elapsed += Time.deltaTime;
    }
}
