using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Flat-vector UI toolkit matching the Chef Survivor design export (solid fills + hard offset
    /// shadows + rounded corners, Lilita One / Nunito). Rounded-rect sprites are generated procedurally
    /// once and 9-sliced, so any element gets clean constant-radius corners tinted to an exact colour.
    /// </summary>
    public static class FlatUI
    {
        // --- exact design tokens (from the .dc.html export) ---
        public static readonly Color PageBg      = C(253, 242, 220);   // #FDF2DC
        public static readonly Color Cream       = C(255, 249, 236);   // #FFF9EC
        public static readonly Color Ink         = C(58, 36, 24);      // #3A2418
        public static readonly Color CreamBorder = C(234, 219, 190);   // #EADBBE
        public static readonly Color Red         = C(237, 76, 50);     // #ED4C32 (PLAY gradient mid)
        public static readonly Color RedShadow   = C(163, 35, 20);     // #A32314
        public static readonly Color Teal        = C(35, 160, 138);    // #23A08A
        public static readonly Color TealShadow  = C(22, 116, 95);     // #16745F
        public static readonly Color Blue        = C(93, 143, 209);    // #5D8FD1
        public static readonly Color BlueShadow  = C(61, 104, 163);    // #3D68A3
        public static readonly Color Yellow      = C(255, 197, 61);    // #FFC53D
        public static readonly Color YellowShadow= C(209, 147, 16);    // #D19310
        public static readonly Color Grey        = C(201, 191, 174);   // neutral
        public static readonly Color GreyShadow  = C(160, 150, 133);
        public static readonly Color Diorama     = C(255, 208, 148);   // #FFD094
        public static readonly Color DioramaEdge = C(217, 160, 91);    // #D9A05B

        static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        static Sprite _round, _pill, _circle;
        /// <summary>General rounded rectangle (radius 26), 9-sliced.</summary>
        public static Sprite Round  => _round  != null ? _round  : (_round  = MakeRounded(96, 26));
        /// <summary>Capsule / pill (fully rounded short axis).</summary>
        public static Sprite Pill   => _pill   != null ? _pill   : (_pill   = MakeRounded(64, 32));
        /// <summary>Full circle (use Simple, square rect).</summary>
        public static Sprite Circle => _circle != null ? _circle : (_circle = MakeRounded(64, 32));

        // ---- builders ----

        public static Image Panel(string name, Transform parent, Color fill, Vector2 aMin, Vector2 aMax, Sprite shape = null)
        {
            var img = Raw(name, parent, shape != null ? shape : Round, fill, aMin, aMax);
            img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>A flat button: a hard shadow copy offset down by <paramref name="shadowPx"/> plus a
        /// coloured face on top. Returns the face transform to parent a label/icon to.</summary>
        public static Button Button(string name, Transform parent, Color fill, Color shadow,
            Vector2 aMin, Vector2 aMax, UnityAction onClick, out Transform face, float shadowPx = 6f, Sprite shape = null)
        {
            var s = shape != null ? shape : Round;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, aMin, aMax);

            var sh = Raw("Shadow", go.transform, s, shadow, Vector2.zero, Vector2.one);
            sh.type = Image.Type.Sliced; sh.rectTransform.anchoredPosition = new Vector2(0f, -shadowPx);
            var fc = Raw("Face", go.transform, s, fill, Vector2.zero, Vector2.one);
            fc.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = fc; btn.transition = Selectable.Transition.None;
            if (onClick != null) btn.onClick.AddListener(onClick);
            face = fc.transform;
            return btn;
        }

        /// <summary>A flat label — no drop-shadow outline (the design's type sits directly on the fill,
        /// so <see cref="UIBuilder.Text"/>'s outline would muddy ink-on-cream). Stretches to its parent;
        /// re-anchor afterwards when a sub-rect is needed. Pass <see cref="Fonts.Body"/> for Nunito copy.</summary>
        public static Text Label(string name, Transform parent, string text, int size, Color c,
            TextAnchor anchor = TextAnchor.MiddleCenter, Font font = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font != null ? font : Fonts.Display;
            t.text = text; t.fontSize = size; t.color = c; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Stretch(t.rectTransform, Vector2.zero, Vector2.one);
            return t;
        }

        /// <summary>Dark pill (coin / gem counter): ink fill, fully rounded.</summary>
        public static Image DarkPill(string name, Transform parent, Vector2 aMin, Vector2 aMax)
            => Panel(name, parent, Ink, aMin, aMax, Pill);

        /// <summary>Cream circle button with a hard cream-border shadow (settings / info).</summary>
        public static Image CircleButton(string name, Transform parent, Vector2 center, float rH, out Image face)
        {
            float rW = rH * (1920f / 1080f);
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, new Vector2(center.x - rW, center.y - rH), new Vector2(center.x + rW, center.y + rH));
            var sh = Raw("Shadow", go.transform, Circle, CreamBorder, Vector2.zero, Vector2.one);
            sh.rectTransform.anchoredPosition = new Vector2(0f, -3f);
            face = Raw("Face", go.transform, Circle, Cream, Vector2.zero, Vector2.one);
            return face;
        }

        static Image Raw(string name, Transform parent, Sprite s, Color c, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s; img.color = c;
            Stretch(img.rectTransform, aMin, aMax);
            return img;
        }

        static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // ---- procedural rounded-rect sprite (signed-distance, 1px anti-aliased) ----

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float h = size * 0.5f, hi = h - radius;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - h) - hi;
                    float qy = Mathf.Abs(y + 0.5f - h) - hi;
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float sdf = outside + inside - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - sdf) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
