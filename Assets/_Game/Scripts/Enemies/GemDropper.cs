using UnityEngine;

namespace DinnerRush
{
    /// <summary>Drops an XP gem at the enemy's death position via the shared <see cref="GemSpawner"/>.</summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class GemDropper : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float coinChance = 0.14f;
        [SerializeField, Range(0f, 1f)] private float foodChance = 0.06f;
        // No hot sauce here: it is earned, not rolled for. Every 20th gem the cook picks up drops a
        // bottle (HotSaucePickup.NoteGemCollected), so it arrives on a rhythm the player can feel
        // instead of on a 3.5% coin flip per kill.

        private EnemyHealth _health;

        private void Awake() => _health = GetComponent<EnemyHealth>();
        private void OnEnable() { if (_health != null) _health.OnDied += Drop; }
        private void OnDisable() { if (_health != null) _health.OnDied -= Drop; }

        private void Drop(Vector3 pos)
        {
            GemSpawner.Spawn(pos);
            if (Random.value < coinChance) CoinSpawner.Spawn(pos);
            if (Random.value < foodChance) FoodSpawner.Spawn(pos);
        }
    }
}
