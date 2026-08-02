using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Player hit points, seeded from <see cref="PlayerStats.MaxHP"/>. Enemies call <see cref="Damage"/>;
    /// the health bar and game-over screen subscribe to the events. Dies once at 0 HP.
    /// </summary>
    [RequireComponent(typeof(PlayerRoot))]
    public class PlayerHealth : MonoBehaviour
    {
        private PlayerStats _stats;
        private float _hp;
        private bool _dead;
        private float _invulnUntil;

        /// <summary>(current, max)</summary>
        public event Action<float, float> OnChanged;
        public event Action OnDied;

        public float Max => _stats != null ? _stats.MaxHP : 100f;
        public float Current => _hp;
        public bool IsDead => _dead;

        private void Awake()
        {
            var root = GetComponent<PlayerRoot>();
            _stats = root != null ? root.Stats : new PlayerStats();
            _hp = _stats.MaxHP;
        }

        private void Start() => OnChanged?.Invoke(_hp, Max);

        private void Update()
        {
            // Coffee passive: regenerate HP over time (no-op at full HP or when dead).
            if (_dead || _stats == null || _stats.HpRegen <= 0f || _hp >= Max) return;
            float before = _hp;
            _hp = Mathf.Min(Max, _hp + _stats.HpRegen * Time.deltaTime);
            // Only notify when the shown value changes (or we top out) — avoids a per-frame HUD rebuild.
            if (_hp >= Max || Mathf.CeilToInt(_hp) != Mathf.CeilToInt(before)) OnChanged?.Invoke(_hp, Max);
        }

        public void Heal(float amount)
        {
            if (_dead || amount <= 0f) return;
            _hp = Mathf.Min(Max, _hp + amount);
            OnChanged?.Invoke(_hp, Max);
        }

        /// <summary>Bring the player back from death (used by the revive screen): clears the dead flag,
        /// restores a fraction of max HP, and grants a short invulnerability window so they don't die
        /// again on the frame they return.</summary>
        public void Revive(float fraction = 1f, float invulnSeconds = 1.5f)
        {
            _dead = false;
            _hp = Mathf.Clamp(Max * fraction, 1f, Max);
            _invulnUntil = Time.time + invulnSeconds;
            OnChanged?.Invoke(_hp, Max);
        }

        public void Damage(float amount)
        {
            if (_dead || amount <= 0f || Time.time < _invulnUntil) return;
            // Apron passive: ignore a fraction of incoming contact damage.
            if (_stats != null) amount *= 1f - Mathf.Clamp(_stats.DamageReduction, 0f, 0.8f);
            _hp = Mathf.Max(0f, _hp - amount);
            OnChanged?.Invoke(_hp, Max);
            if (_hp <= 0f)
            {
                _dead = true;
                OnDied?.Invoke();
            }
        }
    }
}
