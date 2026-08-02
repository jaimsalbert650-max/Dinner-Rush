using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Enemy HP. Raises OnDied(position) when it dies so drops can spawn.</summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 20f;

        private float _health;
        private bool _dead;

        public float Current => _health;
        public float Max => maxHealth;

        /// <summary>Invoked with the death position. Spawner/drops subscribe.</summary>
        public event Action<Vector3> OnDied;

        /// <summary>Invoked whenever the enemy takes a (non-fatal or fatal) hit. Feedback subscribes.</summary>
        public event Action OnDamaged;

        /// <summary>Raised whenever any enemy dies. Global stats (kill count) subscribe once.</summary>
        public static event Action AnyKilled;

        private void OnEnable() { _health = maxHealth; _dead = false; }

        /// <summary>Set this enemy's max health (and refill) — used by the spawner for scaling/variants.</summary>
        public void Configure(float maxHp)
        {
            maxHealth = maxHp;
            _health = maxHp;
            _dead = false;
        }

        /// <summary>Restore HP (capped at max). Used by Healer enemies to mend nearby allies.</summary>
        public void Heal(float amount)
        {
            if (_dead || amount <= 0f) return;
            _health = Mathf.Min(maxHealth, _health + amount);
        }

        public void TakeDamage(float amount)
        {
            if (_dead) return;   // ignore extra hits landing the same frame as the fatal one
            _health -= amount;
            OnDamaged?.Invoke();
            DamageNumbers.Show(transform.position, amount);
            if (_health <= 0f)
            {
                _dead = true;
                OnDied?.Invoke(transform.position);
                AnyKilled?.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
}
