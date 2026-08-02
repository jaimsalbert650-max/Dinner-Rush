using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Endless restaurant-hall floor. Cool-toned tiled floor (so the warm chef stands out)
    /// with outlined cartoon props (tables, chairs, crates, plants) scattered
    /// deterministically per chunk. Decoration only - nothing blocks the player.
    /// Chunks are pooled as the player moves. Self-contained: builds its own sprites.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Sprite floorSprite;   // optional tiled floor texture
        [SerializeField] private Sprite[] props;       // optional real prop sprites
        [SerializeField] private float propScale = 1.4f;
        [Header("Structured layout sprites")]
        [SerializeField] private Sprite tableSprite;
        [SerializeField] private Sprite chairSprite;
        [SerializeField] private Sprite plantSprite;
        [SerializeField] private Sprite lampSprite;
        [SerializeField] private float chunkSize = 12f;
        [SerializeField] private int radius = 2;
        [SerializeField] private int propsPerChunk = 3;
        [SerializeField] private int tilesPerChunk = 4;

        // Diner checkered-floor palette.
        private static readonly Color Cream = new Color(0.95f, 0.90f, 0.80f);
        private static readonly Color Red = new Color(0.80f, 0.30f, 0.28f);

        private Sprite _square;
        private readonly Dictionary<Vector2Int, GameObject> _active = new Dictionary<Vector2Int, GameObject>();
        private readonly List<Vector2Int> _toRemove = new List<Vector2Int>(16);
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private Vector2Int _lastCenter = new Vector2Int(int.MinValue, int.MinValue);

        private void Awake()
        {
            _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            if (floorSprite == null) floorSprite = Resources.Load<Sprite>("kit/tile_diner_floor");   // clean kit floor
            if (player == null)
            {
                var root = FindAnyObjectByType<PlayerRoot>();   // name-independent, matches the rest of the codebase
                if (root != null) player = root.transform;
            }
        }

        private void Update()
        {
            if (player == null) return;
            Vector2Int center = WorldToChunk(player.position);
            if (center == _lastCenter) return;
            _lastCenter = center;
            Refresh(center);
        }

        private Vector2Int WorldToChunk(Vector3 pos)
            => new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.y / chunkSize));

        private void Refresh(Vector2Int center)
        {
            _toRemove.Clear();   // reused: Refresh runs on every chunk crossing
            var toRemove = _toRemove;
            foreach (var kv in _active)
                if (Mathf.Abs(kv.Key.x - center.x) > radius || Mathf.Abs(kv.Key.y - center.y) > radius)
                    toRemove.Add(kv.Key);
            foreach (var c in toRemove)
            {
                _active[c].SetActive(false);
                _pool.Push(_active[c]);
                _active.Remove(c);
            }

            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var coord = new Vector2Int(center.x + dx, center.y + dy);
                    if (!_active.ContainsKey(coord)) _active[coord] = BuildChunk(coord);
                }
        }

        private void AddSprite(Transform parent, Sprite sprite, Vector3 lp, Vector3 scale, Color color, int order)
        {
            var go = new GameObject("Piece");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        private GameObject BuildChunk(Vector2Int coord)
        {
            GameObject chunk = _pool.Count > 0 ? _pool.Pop() : new GameObject("Chunk");
            chunk.SetActive(true);
            chunk.transform.SetParent(transform);
            chunk.transform.position = new Vector3(coord.x * chunkSize, coord.y * chunkSize, 0f);

            // Every chunk's floor is identical — DrawDinerFloor does not even take the coordinate,
            // and the prop fields are unused — so a recycled chunk already carries exactly the
            // children it needs. This used to DestroyImmediate every child and build them again on
            // each reuse, which is object churn every time the player crosses a chunk border (and
            // 33 objects per chunk on the fallback path). Moving it is enough.
            if (chunk.transform.childCount > 0) return chunk;

            if (floorSprite != null)
            {
                // Tiled floor texture.
                var g = new GameObject("Floor");
                g.transform.SetParent(chunk.transform, false);
                g.transform.localPosition = new Vector3(chunkSize * 0.5f, chunkSize * 0.5f, 0f);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = floorSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(chunkSize, chunkSize);
                sr.sortingOrder = -100;
            }
            else
            {
                DrawDinerFloor(chunk.transform);
            }

            return chunk;
        }

        /// <summary>Draws a red-and-cream checkerboard diner floor. A cream slab fills the chunk and
        /// red tiles are painted on alternating cells. Cell count is even so the pattern stays
        /// continuous across chunk borders; a small gap between red tiles reads as grout.</summary>
        private void DrawDinerFloor(Transform chunk)
        {
            float S = chunkSize;
            const int cells = 8;                 // 8x8 tiles per chunk (even -> seamless across chunks)
            float t = S / cells;

            AddSprite(chunk, _square, new Vector3(S * 0.5f, S * 0.5f, 0f), new Vector3(S, S, 1f), Cream, -102);
            for (int i = 0; i < cells; i++)
                for (int j = 0; j < cells; j++)
                    if (((i + j) & 1) == 0)
                        AddSprite(chunk, _square,
                            new Vector3((i + 0.5f) * t, (j + 0.5f) * t, 0f),
                            new Vector3(t * 0.96f, t * 0.96f, 1f), Red, -101);
        }
    }
}
