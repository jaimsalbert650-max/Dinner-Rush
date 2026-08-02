using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Shared builders for the Chef Survivor screen specs (`Assets/Assets cook/sprites/*.md`).
    ///
    /// Every spec is written in **prototype px on a 402x874 logical screen**, so this class owns the
    /// one conversion (<see cref="U"/>) and exposes rect helpers that take those numbers directly —
    /// a screen's code then reads like its spec. U is *measured* from the canvas, never assumed to be
    /// 1080/402: with Canvas Scaler match 0.5 the canvas' logical width is 1080 only on an exactly
    /// 9:16 screen (a 1170x2532 phone gives ~979), and assuming it pushes content off the edges.
    ///
    /// Fixed-height rows anchor to the top or bottom edge and flexible ones stretch between, which is
    /// how the design's flex columns actually reflow on a different aspect.
    /// </summary>
    public static class DesignUI
    {
        public const float DW = 402f, DH = 874f;

        // --- palette beyond FlatUI's core tokens ---
        public static readonly Color Brown        = C(0x8A, 0x6B, 0x4A);   // section labels
        public static readonly Color Muted        = C(0xC9, 0xB1, 0x8A);   // secondary label
        public static readonly Color Purple       = C(0x9B, 0x5D, 0xE5);
        public static readonly Color PurpleDeep   = C(0x7A, 0x3F, 0xC9);
        public static readonly Color PurpleShadow = C(0x5B, 0x2A, 0x9E);
        public static readonly Color Selected     = C(0xFF, 0xE1, 0xB8);   // selected tile fill
        public static readonly Color Caramel      = C(0xA0, 0x6B, 0x2E);

        static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        static float _u;
        static float _measuredWidth;

        /// <summary>Canvas px per prototype px, measured from the root canvas.</summary>
        public static float U
        {
            get { if (_u <= 0f) Recalc(); return _u; }
        }

        public static void Recalc()
        {
            float w = 1080f;
            // A screen built in the first frame's Start would otherwise measure the canvas *before* its
            // first layout pass and latch a stale width — on a 1170px device that reads 978, so every
            // px-sized row and font ends up 20% short while the fraction-anchored widths are right.
            Canvas.ForceUpdateCanvases();
            var c = Object.FindAnyObjectByType<Canvas>();
            if (c != null)
            {
                var root = c.rootCanvas != null ? c.rootCanvas : c;
                float rw = ((RectTransform)root.transform).rect.width;
                if (rw > 1f) w = rw;
            }
            _measuredWidth = w;
            _u = w / DW;
        }

        /// <summary>Re-measures if the canvas has resized since the last measurement, so a screen built
        /// after a resolution change gets the right scale instead of inheriting a stale one.</summary>
        public static void RecalcIfStale()
        {
            var c = Object.FindAnyObjectByType<Canvas>();
            if (c == null) { if (_u <= 0f) Recalc(); return; }
            var root = c.rootCanvas != null ? c.rootCanvas : c;
            float rw = ((RectTransform)root.transform).rect.width;
            if (rw > 1f && Mathf.Abs(rw - _measuredWidth) > 0.5f) Recalc();
            else if (_u <= 0f) Recalc();
        }

        /// <summary>A prototype font size in canvas units.</summary>
        public static int F(float px) => Mathf.Max(1, Mathf.RoundToInt(px * U));

        /// <summary>
        /// Editor screenshot hook: a panel calls this at the end of its Start and opens itself when the
        /// name matches `ui_autoopen`. The editor's player loop does not tick while the Game View is
        /// unfocused, so a canvas activated *after* the first frame never rebuilds its geometry and
        /// captures as if it were never opened — opening on frame 1 is the only way to photograph a
        /// panel headlessly. No effect in a build.
        /// </summary>
        public static bool AutoOpen(string name)
            => Application.isEditor && PlayerPrefs.GetString("ui_autoopen", "") == name;

        // ---------------- screens ----------------

        /// <summary>Full-bleed cream page with its own sorting canvas + raycaster — the design's meta
        /// screens are opaque screens, not dimmed overlays.</summary>
        public static GameObject Page(string name, Transform canvas, int sortingOrder = 31000)
        {
            var img = Raw(name, canvas, null, FlatUI.PageBg);
            Stretch(img.rectTransform, Vector2.zero, Vector2.one);
            var c = img.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true; c.sortingOrder = sortingOrder;
            img.gameObject.AddComponent<GraphicRaycaster>();
            ScreenTransition.AddScreen(img.gameObject);
            return img.gameObject;
        }

        /// <summary>Dimmed scrim + its own sorting canvas — for the design's true overlays
        /// (level-up, pause, revive).</summary>
        public static GameObject Overlay(string name, Transform canvas, int sortingOrder = 31000, float alpha = 0.6f)
        {
            var img = Raw(name, canvas, null, new Color(0f, 0f, 0f, alpha));
            Stretch(img.rectTransform, Vector2.zero, Vector2.one);
            var c = img.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true; c.sortingOrder = sortingOrder;
            img.gameObject.AddComponent<GraphicRaycaster>();
            ScreenTransition.AddOverlay(img.gameObject);
            return img.gameObject;
        }

        /// <summary>The shared header row: 44px circular dark back button left, centred title, and a
        /// 44px spacer right so the title stays optically centred (`00-README.md`).</summary>
        public static void Header(Transform p, string title, UnityAction onBack)
        {
            // Left arrow, like the prototype — Lilita One does have U+2190, so it draws (checked; the
            // glyphs dropped in 42d9351a were the ones it does not).
            if (onBack != null) CircleDark(p, "Back", 18f, 64f, "←", onBack);
            var t = Label("Title", p, title, F(26), FlatUI.Ink);
            TopRect(t.rectTransform, 62f, 64f, 340f, 108f);
        }

        /// <summary>44px dark circle button (back / close).</summary>
        public static Button CircleDark(Transform p, string name, float x0, float yTop, string glyph, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(p, false);
            TopRect((RectTransform)go.transform, x0, yTop, x0 + 44f, yTop + 44f);

            var shadow = Raw("Shadow", go.transform, Kit("btn_circle_dark_shadow"), Color.white);
            Stretch(shadow.rectTransform, Vector2.zero, Vector2.one);
            shadow.raycastTarget = false;
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -3f * U);
            var face = Raw("Face", go.transform, Kit("btn_circle_dark"), Color.white);
            Stretch(face.rectTransform, Vector2.zero, Vector2.one);

            var lbl = Label("Glyph", face.transform, glyph, F(20), FlatUI.Cream);
            lbl.rectTransform.anchoredPosition = new Vector2(0f, 2f * U);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = face; btn.transition = Selectable.Transition.None;
            if (onClick != null) btn.onClick.AddListener(onClick);
            go.AddComponent<ChunkyPress>().Init(face.rectTransform, shadow.rectTransform, 3f * U);
            return btn;
        }

        // ---------------- counter pills ----------------

        // The counter-pill art (`kit/pill_gold_dark`, 626x200 — cut from the user's
        // "Resource counter pill.png"): a dark bar whose left end carries a gold ring socket for the
        // currency icon. Measured off the sprite once, in sprite px: the socket's centre and the
        // diameter of the dark hole inside the ring (y is from the bottom, like a UI anchor).
        private const float PillArtH = 200f;
        private const float PillSocketCx = 99.7f, PillSocketCy = 105.7f, PillSocketD = 128f;
        /// <summary>Left edge of the free bar area in sprite px. The value has to clear the ring's dark
        /// outline, not just its gold (which ends at 186) — at 186 the first digit sits on the outline.</summary>
        private const float PillTextX = 226f;
        /// <summary>The pill height every counter is drawn at (`00-README.md`: 36px rows).</summary>
        public const float PillH = 36f;

        /// <summary>The gold-ringed counter pill. Sliced **horizontally only**, so the left cap (with
        /// its socket) and the right cap keep the aspect they were drawn at while the bar between them
        /// stretches to whatever width the counter needs.</summary>
        public static Image GoldPill(string name, Transform p, float heightPx = PillH, float u = 0f)
        {
            if (u <= 0f) u = U;
            var img = Raw(name, p, Kit("pill_gold_dark"), Color.white);
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            // Sliced borders render at spriteBorder / pixelsPerUnitMultiplier, so this is the one
            // multiplier that makes both caps land at the scale the art was drawn for.
            img.pixelsPerUnitMultiplier = PillArtH / (heightPx * u);
            return img;
        }

        /// <summary>Drops the currency icon inside the pill's gold ring. The socket sits a fixed
        /// distance from the left edge — a fractional anchor would drift with the pill's width.</summary>
        public static Image PillSocket(Image pill, Sprite icon, Color fallback, float heightPx = PillH,
            float fill = 0.74f, float u = 0f)
        {
            if (u <= 0f) u = U;
            var ic = Raw("Icon", pill.transform, icon != null ? icon : FlatUI.Circle,
                icon != null ? Color.white : fallback);
            ic.preserveAspect = icon != null;
            ic.raycastTarget = false;
            float s = heightPx * u / PillArtH;                 // canvas px per sprite px
            var rt = ic.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PillSocketD * fill * s, PillSocketD * fill * s);
            rt.anchoredPosition = new Vector2(PillSocketCx * s, PillSocketCy * s);
            return ic;
        }

        /// <summary>Design px from a pill's left edge where its value text clears the gold ring.</summary>
        public static float PillTextStart(float heightPx = PillH)
            => PillTextX * (heightPx / PillArtH);

        /// <summary>Dark currency pill with an icon and a value. Returns the value label.</summary>
        public static Text Pill(Transform p, string name, float x0, float yTop, float width, Sprite icon,
            Color iconFallback, string value, UnityAction onClick = null)
        {
            var img = GoldPill(name, p, PillH);
            TopRect(img.rectTransform, x0, yTop, x0 + width, yTop + PillH);
            PillSocket(img, icon, iconFallback);

            // 13px, not the design's 14: the socket eats more of the pill than the flat art it replaces,
            // so a six-glyph total ("15,000") only fits at 13.
            var t = Label("Value", img.transform, value, F(13), FlatUI.Cream, TextAnchor.MiddleLeft, Fonts.Body);
            t.fontStyle = FontStyle.Bold;
            Frac(t.rectTransform, PillTextStart() / width, 0.05f, 1f - 9f / width, 0.95f);

            if (onClick != null)
            {
                var b = img.gameObject.AddComponent<Button>();
                b.targetGraphic = img; b.transition = Selectable.Transition.None;
                b.onClick.AddListener(onClick);
            }
            return t;
        }

        /// <summary>Section heading (`GEMS`, `AUDIO`, …) — Lilita One 16px brown.</summary>
        public static Text Section(Transform p, string text, float x0, float yTop, float x1 = 384f)
        {
            var t = Label("Section", p, text, F(16), Brown, TextAnchor.MiddleLeft);
            TopRect(t.rectTransform, x0, yTop, x1, yTop + 22f);
            return t;
        }

        /// <summary>Cream card with the design's 3px bottom bevel. Returns the face to fill.</summary>
        public static Image Card(Transform p, string name, float radiusPx = 14f, float bevelPx = 3f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(p, false);
            var shadow = Raw("Bevel", go.transform, Kit("panel_cream"), FlatUI.CreamBorder);
            Stretch(shadow.rectTransform, Vector2.zero, Vector2.one);
            shadow.type = Image.Type.Sliced; shadow.pixelsPerUnitMultiplier = 60f / (radiusPx * U);
            shadow.raycastTarget = false;
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -bevelPx * U);
            var face = Raw("Face", go.transform, Kit("panel_cream"), Color.white);
            Stretch(face.rectTransform, Vector2.zero, Vector2.one);
            face.type = Image.Type.Sliced; face.pixelsPerUnitMultiplier = 60f / (radiusPx * U);
            return face;
        }

        /// <summary>Small coloured chip (price / LV / NEW tag) with a label.</summary>
        public static Image Chip(Transform p, string kitRes, string text, int fontPx, Color textColor, Font font = null)
        {
            var img = Raw("Chip", p, Kit(kitRes), Color.white);
            img.type = Image.Type.Sliced; img.raycastTarget = false;
            var t = Label("Label", img.transform, text, F(fontPx), textColor, TextAnchor.MiddleCenter, font);
            t.fontStyle = FontStyle.Bold;
            return img;
        }

        // ---------------- scrolling ----------------

        /// <summary>Vertical scroll area from `yTop` down to the screen's bottom edge. `content` is a
        /// top-anchored rect the caller fills top-down and then sizes via <see cref="SetContentHeight"/>.</summary>
        public static ScrollRect Scroll(Transform p, float yTop, out RectTransform content)
        {
            var go = new GameObject("Scroll", typeof(RectTransform));
            go.transform.SetParent(p, false);
            var vp = (RectTransform)go.transform;
            TopRect(vp, 0f, yTop, DW, DH);
            go.AddComponent<RectMask2D>();

            var cgo = new GameObject("Content", typeof(RectTransform));
            cgo.transform.SetParent(go.transform, false);
            content = (RectTransform)cgo.transform;
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.viewport = vp; sr.content = content;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = 0.1f; sr.scrollSensitivity = 40f * U;
            return sr;
        }

        public static void SetContentHeight(RectTransform content, float heightPx)
            => content.sizeDelta = new Vector2(0f, heightPx * U);

        // ---------------- placement (all args in prototype px) ----------------

        // Horizontal placement is expressed as *anchors* — a fraction of the design's 402px width —
        // never as px insets from both edges. An inset pair only produces the right width while U
        // still matches the parent's width, so the moment the canvas resizes (editor resolution
        // change, rotation) every rect built earlier silently narrows, and a small one like a 13px
        // stat pip goes negative and vanishes. Anchors are correct at any width, by construction.
        // Vertical stays in px, because the design's rows must not scale with the aspect ratio.

        /// <summary>Anchors a rect to the parent's TOP edge; y measured down from the design's top.</summary>
        public static RectTransform TopRect(RectTransform rt, float x0, float yTop, float x1, float yBot)
        {
            rt.anchorMin = new Vector2(x0 / DW, 1f); rt.anchorMax = new Vector2(x1 / DW, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -yBot * U);
            rt.offsetMax = new Vector2(0f, -yTop * U);
            return rt;
        }

        /// <summary>Same, anchored to the parent's BOTTOM edge (y still measured from the design's top).</summary>
        public static RectTransform BotRect(RectTransform rt, float x0, float yTop, float x1, float yBot)
        {
            rt.anchorMin = new Vector2(x0 / DW, 0f); rt.anchorMax = new Vector2(x1 / DW, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, (DH - yBot) * U);
            rt.offsetMax = new Vector2(0f, (DH - yTop) * U);
            return rt;
        }

        /// <summary>Stretches a rect between the parent's top and bottom edges (the design's flex:1).</summary>
        public static RectTransform FlexRect(RectTransform rt, float x0, float yTop, float x1, float yBotFromBottom)
        {
            rt.anchorMin = new Vector2(x0 / DW, 0f); rt.anchorMax = new Vector2(x1 / DW, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f, yBotFromBottom * U);
            rt.offsetMax = new Vector2(0f, -yTop * U);
            return rt;
        }

        /// <summary>A centred fixed-size column — the shape every overlay in the design uses.</summary>
        public static RectTransform Column(Transform parent, float widthPx, float heightPx)
        {
            var go = new GameObject("Column", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(widthPx * U, heightPx * U);
            return rt;
        }

        /// <summary>A full-width row inside a <see cref="Column"/>, y measured down from its top.</summary>
        public static RectTransform ColRect(RectTransform rt, float yTop, float yBot, float inset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(inset * U, -yBot * U);
            rt.offsetMax = new Vector2(-inset * U, -yTop * U);
            return rt;
        }

        public static void Frac(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // ---------------- primitives ----------------

        public static Text Label(string name, Transform parent, string text, int size, Color c,
            TextAnchor anchor = TextAnchor.MiddleCenter, Font font = null)
            => FlatUI.Label(name, parent, text, size, c, anchor, font);

        public static Image Raw(string name, Transform parent, Sprite s, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s != null ? s : UIBuilder.White;
            img.color = c;
            Stretch(img.rectTransform, Vector2.zero, Vector2.one);
            return img;
        }

        public static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        public static Sprite Kit(string name) => Resources.Load<Sprite>("kit/" + name);

        /// <summary>Thousands-separated, compact only once it would overflow a pill (`1,250`, `123K`).</summary>
        public static string Num(int n)
        {
            if (n >= 1000000) return (n / 1000000f).ToString("0.#") + "M";
            if (n >= 100000) return (n / 1000f).ToString("0.#") + "K";
            return n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>The coin / gem icon art wired on the lobby, so every screen shows the same one.</summary>
        public static Sprite CoinIcon => Lobby != null ? Lobby.CoinIcon : null;
        public static Sprite GemIcon => Lobby != null ? Lobby.GemIcon : null;
        public static Sprite StarIcon => Lobby != null ? Lobby.StarIcon : null;
        public static Sprite LockIcon => Lobby != null ? Lobby.LockIcon : null;

        static LobbyScreen _lobby;
        static LobbyScreen Lobby => _lobby != null ? _lobby : (_lobby = Object.FindAnyObjectByType<LobbyScreen>());
    }
}
