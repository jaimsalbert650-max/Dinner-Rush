using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DinnerRush
{
    /// <summary>
    /// Shared runtime-UI construction helpers. Existing panels each duplicate their own
    /// NewImage/NewText/WhiteSprite; new UI (the lobby) uses this instead to avoid piling on
    /// more duplication. Adds sliced-sprite + icon support for the GUI Pro art packs.
    /// Text still uses a dynamic OS font (no TextMeshPro).
    /// </summary>
    public static class UIBuilder
    {
        /// <summary>Default UI font — Lilita One (display) once the TTF is dropped in Resources/fonts,
        /// OS fallback until then. See <see cref="Fonts"/>.</summary>
        public static Font Font => Fonts.Display;

        private static Sprite _white;
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var tex = Texture2D.whiteTexture;
                    _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                return _white;
            }
        }

        public static void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color c,
            Vector2 aMin, Vector2 aMax, bool sliced = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : White;
            img.color = c;
            if (sliced && sprite != null)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
            }
            else if (sprite != null)
            {
                // A real (non-sliced) sprite is art/icon — keep its native proportions instead of
                // stretching it to fill a non-matching rect. (Solid-color fills pass a null sprite.)
                img.preserveAspect = true;
            }
            SetAnchors(img.rectTransform, aMin, aMax);
            return img;
        }

        public static Text Text(string name, Transform parent, string content, int size, Color c,
            Vector2 aMin, Vector2 aMax, TextAnchor anchor = TextAnchor.MiddleCenter, Font font = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font != null ? font : Font; t.text = content; t.fontSize = size; t.color = c;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            SetAnchors(t.rectTransform, aMin, aMax);
            var _ol = go.AddComponent<UnityEngine.UI.Outline>();
            _ol.effectColor = new Color(0f, 0f, 0f, 0.6f);
            _ol.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        /// <summary>Full-rect button image with a click handler. Caller adds label/icon children.</summary>
        public static Button Button(string name, Transform parent, Sprite sprite, Color c,
            Vector2 aMin, Vector2 aMax, UnityAction onClick, bool sliced = true)
        {
            var img = Image(name, parent, sprite, c, aMin, aMax, sliced);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }
    }
}
