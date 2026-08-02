using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Pooled spawner for food-heal pickups. Singleton — gameplay calls the static <see cref="Spawn"/>.
    /// Unlike GemSpawner it needs no prefab: FoodPickup builds its own visual, so pooled instances are
    /// created in code. Resolves the player references lazily.
    /// </summary>
    public class FoodSpawner : MonoBehaviour
    {
        public static FoodSpawner Instance { get; private set; }

        [SerializeField] private Sprite foodArt;

        private Transform _player;
        private PlayerHealth _health;
        private PlayerStats _stats;
        private readonly Stack<FoodPickup> _pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Spawn(Vector3 pos)
        {
            if (Instance != null) Instance.Do(pos);
        }

        private void Do(Vector3 pos)
        {
            EnsureRefs();

            FoodPickup food = _pool.Count > 0 ? _pool.Pop() : Create();
            if (food == null) return;
            food.transform.position = pos;
            food.gameObject.SetActive(true);
            food.Init(_player, _health, _stats, Return, foodArt);
        }

        private FoodPickup Create()
        {
            var go = new GameObject("FoodPickup");
            go.transform.SetParent(transform);
            return go.AddComponent<FoodPickup>();
        }

        private void EnsureRefs()
        {
            if (_player != null) return;
            var root = FindAnyObjectByType<PlayerRoot>();
            if (root == null) return;
            _player = root.transform;
            _stats = root.Stats;
            _health = root.GetComponent<PlayerHealth>();
        }

        private void Return(FoodPickup food)
        {
            food.gameObject.SetActive(false);
            _pool.Push(food);
        }
    }
}
