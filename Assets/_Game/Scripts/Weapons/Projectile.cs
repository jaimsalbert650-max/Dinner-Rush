using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Flies straight, damages the first IDamageable it hits, then despawns.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float spinSpeed = 150f;   // deg/sec — a slow, readable tumble (was a fast blur)

        private Rigidbody2D _rb;
        private float _damage;
        private float _age;
        private bool _alive;
        private Action<Projectile> _despawn;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        public void Launch(Vector2 direction, float damage, Action<Projectile> despawn)
        {
            _damage = damage;
            _despawn = despawn;
            _age = 0f;
            _alive = true;
            transform.rotation = Quaternion.identity;
            _rb.linearVelocity = direction.normalized * speed;
        }

        private void Update()
        {
            if (!_alive) return;
            _age += Time.deltaTime;
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);   // tumble
            if (_age >= lifetime) Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_alive) return;
            if (other.TryGetComponent<IDamageable>(out var d))
            {
                VfxSpawner.Play(VfxKind.Impact, transform.position);
                d.TakeDamage(_damage);
                Despawn();
            }
        }

        private void Despawn()
        {
            if (!_alive) return;   // guard against double-return to the pool (multi-hit / hit+lifetime same frame)
            _alive = false;
            _rb.linearVelocity = Vector2.zero;
            _despawn?.Invoke(this);
        }
    }
}
