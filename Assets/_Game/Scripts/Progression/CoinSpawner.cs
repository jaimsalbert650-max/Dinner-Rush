using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Pooled spawner for coins. Singleton; call the static <see cref="Spawn"/>.</summary>
    public class CoinSpawner : MonoBehaviour
    {
        public static CoinSpawner Instance { get; private set; }

        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private Sprite coinArt;

        private Transform _player;
        private PlayerStats _stats;
        private readonly Stack<Coin> _pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Spawn(Vector3 pos)
        {
            if (Instance != null) Instance.Do(pos);
        }

        private void Do(Vector3 pos)
        {
            if (coinPrefab == null) return;
            EnsureRefs();
            Coin coin = _pool.Count > 0 ? _pool.Pop() : Instantiate(coinPrefab, transform).GetComponent<Coin>();
            if (coin == null) return;
            coin.transform.position = pos;
            coin.gameObject.SetActive(true);
            coin.Init(_player, _stats, Return, coinArt);
        }

        private void EnsureRefs()
        {
            if (_player != null) return;
            var root = FindAnyObjectByType<PlayerRoot>();
            if (root == null) return;
            _player = root.transform;
            _stats = root.Stats;
        }

        private void Return(Coin coin)
        {
            coin.gameObject.SetActive(false);
            _pool.Push(coin);
        }
    }
}
