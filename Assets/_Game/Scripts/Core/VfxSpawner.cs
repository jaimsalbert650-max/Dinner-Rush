using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    public enum VfxKind { DeathPoof, Impact }

    /// <summary>
    /// Plays pooled one-shot particle VFX by kind. Singleton — gameplay code calls the static
    /// <see cref="Play"/>. Instances are pooled per-prefab and auto-returned after a fixed lifetime,
    /// so frequent enemy deaths don't churn the GC.
    /// </summary>
    public class VfxSpawner : MonoBehaviour
    {
        public static VfxSpawner Instance { get; private set; }

        [Header("Death poof (enemy dies)")]
        [SerializeField] private GameObject deathPoofPrefab;
        [SerializeField] private float deathPoofScale = 0.3f;

        [Header("Impact (knife hits)")]
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private float impactScale = 0.2f;

        [SerializeField] private float lifetime = 1.2f;

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private readonly List<ActiveVfx> _active = new();

        private struct ActiveVfx { public GameObject go; public GameObject prefab; public float returnAt; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Play a one-shot VFX of the given kind at a world position. No-op if no spawner exists.</summary>
        public static void Play(VfxKind kind, Vector3 pos)
        {
            if (Instance != null) Instance.Spawn(kind, pos);
        }

        private void Spawn(VfxKind kind, Vector3 pos)
        {
            GameObject prefab = kind == VfxKind.DeathPoof ? deathPoofPrefab : impactPrefab;
            float scale = kind == VfxKind.DeathPoof ? deathPoofScale : impactScale;
            if (prefab == null) return;

            GameObject go = Get(prefab);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.SetActive(true);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Clear(true);
                ps.Play(true);
            }
            _active.Add(new ActiveVfx { go = go, prefab = prefab, returnAt = Time.time + lifetime });
        }

        private GameObject Get(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var q) && q.Count > 0) return q.Dequeue();
            return Instantiate(prefab, transform);
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.time < _active[i].returnAt) continue;
                var e = _active[i];
                if (e.go != null)
                {
                    e.go.SetActive(false);
                    if (!_pools.TryGetValue(e.prefab, out var q)) { q = new Queue<GameObject>(); _pools[e.prefab] = q; }
                    q.Enqueue(e.go);
                }
                _active.RemoveAt(i);
            }
        }
    }
}
