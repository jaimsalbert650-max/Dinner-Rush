using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Pooled floating combat text. Call <see cref="Show"/> with a world position and amount; each
    /// number rises and fades over its lifetime. World-space TextMesh with a dynamic OS font.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        public static DamageNumbers Instance { get; private set; }

        [SerializeField] private float rise = 1.3f;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private float characterSize = 0.14f;
        [SerializeField] private Color color = new Color(1f, 0.95f, 0.4f);

        private Font _font;
        private Material _mat;
        private readonly List<Num> _active = new();
        private readonly Stack<TextMesh> _pool = new();

        private struct Num { public TextMesh tm; public Vector3 start; public float t; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Helvetica", "Verdana" }, fontSize);
            _mat = _font.material;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Show(Vector3 pos, float amount)
        {
            if (Instance != null) Instance.Do(pos, amount);
        }

        private void Do(Vector3 pos, float amount)
        {
            var tm = _pool.Count > 0 ? _pool.Pop() : Create();
            tm.gameObject.SetActive(true);
            tm.text = Label(Mathf.Max(1, Mathf.RoundToInt(amount)));
            var c = color; c.a = 1f; tm.color = c;
            pos += new Vector3(Random.Range(-0.15f, 0.15f), 0.2f, 0f);
            tm.transform.position = pos;
            _active.Add(new Num { tm = tm, start = pos, t = 0f });
        }

        // Damage numbers are the most frequent text in the game — an aura ticking against a full
        // crowd fires hundreds of these a second, and each `int.ToString()` was a fresh string.
        // Almost every hit lands in the low hundreds, so those are built once and reused.
        private static readonly string[] SmallNumbers = BuildSmallNumbers();

        private static string[] BuildSmallNumbers()
        {
            var a = new string[1000];
            for (int i = 0; i < a.Length; i++) a[i] = i.ToString();
            return a;
        }

        private static string Label(int v) =>
            (uint)v < (uint)SmallNumbers.Length ? SmallNumbers[v] : v.ToString();

        private TextMesh Create()
        {
            var go = new GameObject("DmgNum");
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = _font;
            tm.fontSize = fontSize;
            tm.characterSize = characterSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.sortingOrder = 50;
            return tm;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var n = _active[i];
                n.t += dt;
                float u = n.t / lifetime;
                if (u >= 1f)
                {
                    n.tm.gameObject.SetActive(false);
                    _pool.Push(n.tm);
                    _active.RemoveAt(i);
                    continue;
                }
                n.tm.transform.position = n.start + Vector3.up * (rise * u);
                var c = color; c.a = 1f - u; n.tm.color = c;
                _active[i] = n;
            }
        }
    }
}
