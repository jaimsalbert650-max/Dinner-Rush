using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// A row of weapon slots under the top HUD showing the player's owned weapons (colored by type)
    /// and their levels. Polls <see cref="WeaponManager.Weapons"/>.
    /// </summary>
    public class WeaponBar : MonoBehaviour
    {
        [SerializeField] private int slots = 6;

        private WeaponManager _weapons;
        private Font _font;
        private Image[] _icon;
        private Text[] _lvl;

        private void Start()
        {
            _weapons = FindAnyObjectByType<WeaponManager>();
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Verdana" }, 22);
            Build();
        }

        private void Update()
        {
            if (_weapons == null) return;
            var ws = _weapons.Weapons;
            for (int i = 0; i < slots; i++)
            {
                bool has = i < ws.Count;
                if (_icon[i] != null) { _icon[i].enabled = has; if (has) _icon[i].color = ColorFor(ws[i]); }
                if (_lvl[i] != null) _lvl[i].text = has ? ws[i].Level.ToString() : "";
            }
        }

        private static Color ColorFor(Weapon w)
        {
            switch (w.GetType().Name)
            {
                case "KnifeWeapon": return new Color(0.55f, 0.75f, 0.95f);
                case "AuraWeapon": return new Color(0.4f, 0.9f, 0.45f);
                case "KetchupWeapon": return new Color(0.85f, 0.2f, 0.15f);
                case "DroneWeapon": return new Color(0.9f, 0.35f, 0.32f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            var sprite = WhiteSprite();
            var slotSprite = Resources.Load<Sprite>("kit/panel_slot_empty");
            _icon = new Image[slots];
            _lvl = new Text[slots];

            float x0 = 0.02f, w = 0.088f, gap = 0.006f, y0 = 0.852f, y1 = 0.898f;
            for (int i = 0; i < slots; i++)
            {
                float ax = x0 + i * (w + gap);
                var bg = NewImage("WSlot", canvas.transform, slotSprite != null ? slotSprite : sprite,
                    slotSprite != null ? Color.white : new Color(0.1f, 0.11f, 0.14f, 0.85f),
                    new Vector2(ax, y0), new Vector2(ax + w, y1));
                if (slotSprite != null) bg.type = Image.Type.Sliced;
                var icon = NewImage("WIcon", bg.transform, sprite, new Color(0.5f, 0.5f, 0.5f),
                    new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
                icon.enabled = false;
                var lvl = NewText("WLvl", bg.transform, "", 20, Color.white,
                    new Vector2(0.4f, 0.02f), new Vector2(0.97f, 0.5f));
                _icon[i] = icon;
                _lvl[i] = lvl;
            }
        }

        private Image NewImage(string name, Transform parent, Sprite s, Color c, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            var rt = img.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }

        private Text NewText(string name, Transform parent, string content, int size, Color c, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.text = content; t.fontSize = size; t.color = c;
            t.alignment = TextAnchor.LowerRight;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return t;
        }

        private static Sprite WhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
