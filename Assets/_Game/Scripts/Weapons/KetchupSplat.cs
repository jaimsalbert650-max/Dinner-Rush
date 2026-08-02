using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// The red mark the ketchup leaves where it lands: it pops in, holds for a moment and fades out.
    /// Pooled and self-contained — <see cref="Spawn"/> is the whole API, so a weapon can call it in a
    /// damage loop without owning any lifetime.
    /// </summary>
    public class KetchupSplat : MonoBehaviour
    {
        private const float Life = 0.34f;
        private const float Diameter = 0.62f;

        // Static, so it survives a scene load while its GameObjects do not — every pop has to skip the
        // corpses that leaves behind (same rule as EnemyProjectile's pool).
        private static readonly Stack<KetchupSplat> Pool = new Stack<KetchupSplat>();
        private static Sprite _sprite;

        private SpriteRenderer _sr;
        private float _t;

        public static void Spawn(Vector2 pos)
        {
            KetchupSplat s = null;
            while (s == null && Pool.Count > 0) s = Pool.Pop();
            if (s == null)
            {
                var go = new GameObject("KetchupSplat");
                s = go.AddComponent<KetchupSplat>();
                s._sr = go.AddComponent<SpriteRenderer>();
                s._sr.sprite = Art();
                s._sr.sortingOrder = 6;         // over the floor and the crowd, under the cook (20)
            }
            var t = s.transform;
            t.position = pos;
            t.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            s._t = 0f;
            s.gameObject.SetActive(true);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = _t / Life;
            if (k >= 1f)
            {
                gameObject.SetActive(false);
                Pool.Push(this);
                return;
            }

            // Pops to full size in the first fifth, then fades where it is — a splash, not a bubble.
            float grow = k < 0.2f ? Mathf.Lerp(0.55f, 1.08f, k / 0.2f) : Mathf.Lerp(1.08f, 1f, (k - 0.2f) / 0.8f);
            transform.localScale = new Vector3(grow, grow, 1f);
            var c = _sr.color;
            c.a = k < 0.5f ? 1f : 1f - (k - 0.5f) / 0.5f;
            _sr.color = c;
        }

        /// <summary>A splat drawn once and shared: one fat centre, a few lobes, a scatter of droplets.
        /// Procedural because the sheet's droplets are part of the stream frames, not separate art.</summary>
        private static Sprite Art()
        {
            if (_sprite != null) return _sprite;

            const int D = 96;
            var blobs = new Vector3[]
            {
                new Vector3(0.48f, 0.50f, 0.28f),
                new Vector3(0.31f, 0.61f, 0.17f),
                new Vector3(0.65f, 0.39f, 0.15f),
                new Vector3(0.60f, 0.68f, 0.12f),
                new Vector3(0.36f, 0.35f, 0.11f),
                new Vector3(0.14f, 0.31f, 0.055f),
                new Vector3(0.86f, 0.63f, 0.048f),
                new Vector3(0.52f, 0.13f, 0.042f),
                new Vector3(0.79f, 0.85f, 0.052f),
            };
            var red = new Color(0.702f, 0.129f, 0.098f);
            var gloss = new Color(0.878f, 0.286f, 0.220f);

            var tex = new Texture2D(D, D, TextureFormat.RGBA32, false);
            var cols = new Color[D * D];
            for (int y = 0; y < D; y++)
                for (int x = 0; x < D; x++)
                {
                    var p = new Vector2((x + 0.5f) / D, (y + 0.5f) / D);
                    float best = 1f;
                    for (int i = 0; i < blobs.Length; i++)
                    {
                        float d = (p - new Vector2(blobs[i].x, blobs[i].y)).magnitude - blobs[i].z;
                        if (d < best) best = d;
                    }
                    float aa = 1.4f / D;
                    float a = Mathf.Clamp01((aa - best) / aa);
                    // One highlight on the upper-left of the centre lobe, like the art's wet look.
                    float h = (p - new Vector2(0.40f, 0.60f)).magnitude - 0.085f;
                    var c = h < 0f ? gloss : red;
                    cols[y * D + x] = new Color(c.r, c.g, c.b, a);
                }
            tex.SetPixels(cols);
            tex.Apply();
            _sprite = Sprite.Create(tex, new Rect(0, 0, D, D), new Vector2(0.5f, 0.5f), D / Diameter);
            return _sprite;
        }
    }
}
