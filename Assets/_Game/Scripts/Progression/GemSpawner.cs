using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Pooled spawner for XP gems. Singleton — gameplay code calls the static <see cref="Spawn"/>.
    /// Resolves the player references lazily so gems know who to home toward and which stats to read.
    /// </summary>
    public class GemSpawner : MonoBehaviour
    {
        public static GemSpawner Instance { get; private set; }

        [SerializeField] private GameObject gemPrefab;
        [SerializeField] private Sprite gemArt;

        private Transform _player;
        private PlayerExperience _xp;
        private PlayerStats _stats;
        private readonly Stack<XpGem> _pool = new();

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
            if (gemPrefab == null) return;
            EnsureRefs();

            XpGem gem = _pool.Count > 0 ? _pool.Pop() : Instantiate(gemPrefab, transform).GetComponent<XpGem>();
            if (gem == null) return;
            gem.transform.position = pos;
            gem.gameObject.SetActive(true);
            gem.Init(_player, _xp, _stats, Return, gemArt);
        }

        private void EnsureRefs()
        {
            if (_player != null) return;
            var root = FindAnyObjectByType<PlayerRoot>();
            if (root == null) return;
            _player = root.transform;
            _stats = root.Stats;
            _xp = root.GetComponent<PlayerExperience>();
        }

        private void Return(XpGem gem)
        {
            gem.gameObject.SetActive(false);
            _pool.Push(gem);
        }
    }
}
