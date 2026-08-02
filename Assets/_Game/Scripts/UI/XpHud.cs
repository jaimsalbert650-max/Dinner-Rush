using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Builds a top-of-screen XP bar at runtime and drives its green fill from
    /// <see cref="PlayerExperience"/>. Image-only (no font), added to the UI canvas.
    /// </summary>
    public class XpHud : MonoBehaviour
    {
        [SerializeField] private PlayerExperience xp;
        [SerializeField] private Color barBg = new Color(0.12f, 0.13f, 0.16f, 0.9f);
        [SerializeField] private Color barFill = new Color(0.55f, 0.85f, 0.30f);
        [SerializeField] private Sprite barFrame;

        private Image _fill;
        private Text _level;

        private void Start()
        {
            if (xp == null) xp = FindAnyObjectByType<PlayerExperience>();
            BuildBar();
            if (xp != null)
            {
                xp.OnXpChanged += OnXp;
                if (_fill != null) _fill.fillAmount = xp.Progress01;
            }
        }

        private void OnDestroy()
        {
            if (xp != null) xp.OnXpChanged -= OnXp;
        }

        private void OnXp(int into, int need, int level)
        {
            if (_fill != null) _fill.fillAmount = need > 0 ? (float)into / need : 0f;
            if (_level != null) _level.text = level.ToString();
        }

        private void BuildBar()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var sprite = WhiteSprite();
            var trackSprite = Resources.Load<Sprite>("kit/bar_track_dark");
            var fillSprite  = Resources.Load<Sprite>("kit/bar_fill_yellow");

            Image bg;
            if (trackSprite != null) { bg = NewImage("XpBarBg", canvas.transform, trackSprite, Color.white); bg.type = Image.Type.Sliced; }
            else if (barFrame != null) { bg = NewImage("XpBarBg", canvas.transform, barFrame, Color.white); bg.type = Image.Type.Sliced; }
            else { bg = NewImage("XpBarBg", canvas.transform, sprite, barBg); }
            var rt = bg.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 0.945f);
            rt.anchorMax = new Vector2(0.85f, 0.978f);   // leaves room for the level chip
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var fill = NewImage("XpBarFill", bg.transform, fillSprite != null ? fillSprite : sprite, fillSprite != null ? Color.white : barFill);
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(10f, 6f);
            frt.offsetMax = new Vector2(-10f, -6f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            _fill = fill;

            // Level chip at the right end of the XP row (09-hud).
            var chipArt = Resources.Load<Sprite>("kit/chip_yellow_face");
            var chip = NewImage("XpLevelChip", canvas.transform, chipArt != null ? chipArt : sprite, chipArt != null ? Color.white : new Color(1f, 0.8f, 0.2f));
            if (chipArt != null) chip.type = Image.Type.Sliced;
            var crt = chip.rectTransform;
            crt.anchorMin = new Vector2(0.865f, 0.94f);
            crt.anchorMax = new Vector2(0.975f, 0.983f);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            _level = UIBuilder.Text("XpLevel", chip.transform, "1", 22, new Color(0.25f, 0.16f, 0.03f), Vector2.zero, Vector2.one);
        }

        private Image NewImage(string name, Transform parent, Sprite s, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            return img;
        }

        private static Sprite WhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
