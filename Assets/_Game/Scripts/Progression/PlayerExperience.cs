using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// MonoBehaviour wrapper around <see cref="LevelProgression"/>. Gems call <see cref="AddXp"/>;
    /// the HUD and level-up systems subscribe to the events. Lives on the player.
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        [SerializeField] private int baseXp = 5;
        [SerializeField] private float growth = 1.3f;

        private LevelProgression _prog;
        private PlayerStats _stats;

        /// <summary>(xpIntoLevel, xpForNextLevel, level) — fired whenever XP or level changes.</summary>
        public event Action<int, int, int> OnXpChanged;
        /// <summary>New level — fired once per level gained.</summary>
        public event Action<int> OnLevelUp;

        public int Level => _prog?.Level ?? 1;
        public float Progress01 => _prog?.Progress01 ?? 0f;

        private void Awake() => _prog = new LevelProgression(baseXp, growth);

        private void Start()
        {
            if (TryGetComponent<PlayerRoot>(out var root)) _stats = root.Stats;
            Emit();
        }

        public void AddXp(int amount)
        {
            if (_prog == null || amount <= 0) return;
            // Notepad passive: scale incoming XP.
            if (_stats != null) amount = Mathf.Max(1, Mathf.RoundToInt(amount * _stats.XpMult));
            int levelBefore = _prog.Level;
            int gained = _prog.Add(amount);
            // Fire once per level actually gained, with that level's number (not the final level each time).
            for (int i = 0; i < gained; i++) OnLevelUp?.Invoke(levelBefore + 1 + i);
            Emit();
        }

        private void Emit() => OnXpChanged?.Invoke(_prog.XpIntoLevel, _prog.XpForNextLevel, _prog.Level);
    }
}
