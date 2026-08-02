using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// On enemy death, plays a poof VFX at the death position via the shared <see cref="VfxSpawner"/>.
    /// The enemy deactivates itself on death, so the poof is spawned detached (not parented to the enemy).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class DeathEffect : MonoBehaviour
    {
        [SerializeField] private float hitStopSeconds = 0.035f;

        private EnemyHealth _health;

        private void Awake() => _health = GetComponent<EnemyHealth>();
        private void OnEnable() { if (_health != null) _health.OnDied += Play; }
        private void OnDisable() { if (_health != null) _health.OnDied -= Play; }

        private void Play(Vector3 pos)
        {
            VfxSpawner.Play(VfxKind.DeathPoof, pos);
            HitStop.Do(hitStopSeconds);
        }
    }
}
