using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Drives a fixed sequence of waves. A wave is cleared by <b>killing its quota of enemies</b>,
    /// not by surviving a timer — so the run advances at the pace the player actually fights, and
    /// standing still gets you nowhere. Owns the current wave number, the kill quota, and the
    /// difficulty curve (enemy HP + spawn interval). The spawner reads its parameters from here;
    /// HUD / win UI subscribe to the static events. Honours StageConfig tiers.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Waves")]
        [SerializeField] private int waveCount = 20;

        [Header("Kill quota")]
        [Tooltip("Enemies to kill to clear wave 1.")]
        [SerializeField] private int killsFirstWave = 8;
        [Tooltip("Added to the quota with each wave, so wave N needs killsFirstWave + (N-1)*killsPerWave.")]
        [SerializeField] private int killsPerWave = 4;

        [Header("Difficulty")]
        [SerializeField] private float baseEnemyHp = 20f;
        [SerializeField] private float hpPerWave = 0.18f;       // +18% enemy HP per wave
        [SerializeField] private float startInterval = 1.2f;    // spawn gap at wave 1
        [SerializeField] private float minInterval = 0.35f;     // spawn gap at last wave
        [SerializeField] private float baseEnemySpeed = 2.2f;

        /// <summary>Raised when a new wave begins (arg = new wave number, 1-based).</summary>
        public static event Action<int> OnWaveChanged;
        /// <summary>Raised once, when the final wave's quota is filled.</summary>
        public static event Action OnAllWavesCleared;
        /// <summary>Raised on every kill that counts toward the current wave (killed, required).</summary>
        public static event Action<int, int> OnWaveProgress;

        private int _wave;
        private int _killed;        // kills credited to the current wave
        private bool _running;
        private bool _started;

        private float _diffHp = 1f, _diffSpd = 1f, _diffRate = 1f;   // StageConfig tier multipliers

        public int CurrentWave => _wave;
        public int WaveCount => waveCount;
        public bool Running => _running;

        /// <summary>Kills credited to the current wave.</summary>
        public int KillsThisWave => _killed;

        /// <summary>Kills needed to clear the current wave.</summary>
        public int KillsRequired => QuotaFor(_wave);

        /// <summary>Current wave's completion, 0..1 — for the HUD bar.</summary>
        public float WaveProgress01
        {
            get
            {
                int need = KillsRequired;
                return need <= 0 ? 1f : Mathf.Clamp01((float)_killed / need);
            }
        }

        private int QuotaFor(int wave) =>
            Mathf.Max(1, killsFirstWave + Mathf.Max(0, wave - 1) * killsPerWave);

        public float CurrentEnemyHp => baseEnemyHp * (1f + (_wave - 1) * hpPerWave) * _diffHp;
        public float EnemySpeed => baseEnemySpeed * _diffSpd;

        public float CurrentSpawnInterval
        {
            get
            {
                float t = waveCount <= 1 ? 1f : (float)(_wave - 1) / (waveCount - 1);
                return Mathf.Lerp(startInterval, minInterval, t) / _diffRate;
            }
        }

        private void Awake()
        {
            var tier = StageConfig.Current;
            _diffHp = tier.hpMult; _diffSpd = tier.spdMult; _diffRate = tier.spawnRateMult;
        }

        private void OnEnable() => EnemyHealth.AnyKilled += OnKill;
        private void OnDisable() => EnemyHealth.AnyKilled -= OnKill;

        private void Update()
        {
            // First frame: kick off wave 1 here (not in Start) so every subscriber's Start() has
            // already run and none miss the initial OnWaveChanged.
            if (!_started)
            {
                _started = true;
                _wave = 1;
                _killed = 0;
                _running = true;
                OnWaveChanged?.Invoke(_wave);
                OnWaveProgress?.Invoke(_killed, KillsRequired);
            }
        }

        /// <summary>
        /// A kill is what moves the run forward. Every enemy counts, including the spawnlings a
        /// splitter leaves behind and anything a Summoner calls in — they are enemies the player
        /// still has to clear, so making them worth nothing would only punish fighting the
        /// characters that produce them.
        /// </summary>
        private void OnKill()
        {
            if (!_running) return;

            _killed++;

            // Loop rather than a single check: the carried surplus can clear a whole wave on its
            // own, and the run should not sit on a finished wave waiting for one more kill.
            while (_running && _killed >= KillsRequired)
            {
                if (_wave >= waveCount)
                {
                    _running = false;
                    OnAllWavesCleared?.Invoke();
                    break;
                }

                // Carry the overkill forward rather than dropping it: several enemies often die in
                // the same frame (a molotov pool, an aura tick), and discarding the surplus would
                // quietly waste kills the player earned.
                _killed -= KillsRequired;
                _wave++;
                OnWaveChanged?.Invoke(_wave);
            }

            OnWaveProgress?.Invoke(_killed, KillsRequired);
        }
    }
}
