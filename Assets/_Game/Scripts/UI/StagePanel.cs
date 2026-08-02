using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Stage / difficulty select. Lists <see cref="StageConfig"/> tiers; the player selects one (higher
    /// tiers locked behind a best-survival-time gate). The choice persists and the spawner reads it on
    /// the next run. Built at runtime. #5.
    /// </summary>
    public class StagePanel : MonoBehaviour
    {
        private Font _font;
        private Sprite _rowArt, _btnArt;
        private GameObject _root;
        private Row[] _rows;
        private System.Action _onChanged;

        private static readonly Color Ink = new Color(0.23f, 0.14f, 0.09f);

        private class Row { public int index; public Text info; public Text btnLabel; public Image btnBg; public Button btn; }

        public void SetOnChanged(System.Action cb) => _onChanged = cb;

        private void Start()
        {
            _font = Fonts.Display;   // Lilita One once the TTF is dropped
            Build();
            _root.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        private void Select(int i)
        {
            if (!StageConfig.Unlocked(i)) return;
            StageConfig.Selected = i;
            _onChanged?.Invoke();
            Refresh();
        }

        private void Refresh()
        {
            int sel = StageConfig.Selected;
            foreach (var r in _rows)
            {
                var tier = StageConfig.Tiers[r.index];
                bool unlocked = StageConfig.Unlocked(r.index);
                bool chosen = r.index == sel;
                r.info.text = tier.name + "\n" + tier.desc;
                if (!unlocked)
                {
                    int m = (int)(tier.unlockTime / 60f), s = (int)(tier.unlockTime % 60f);
                    r.btnLabel.text = "LOCKED\nBest " + m + ":" + s.ToString("00");
                    r.btnBg.color = new Color(0.55f, 0.55f, 0.6f);
                }
                else
                {
                    r.btnLabel.text = chosen ? "SELECTED" : "SELECT";
                    r.btnBg.color = chosen ? new Color(1f, 0.82f, 0.35f) : Color.white;
                }
                r.btn.interactable = unlocked && !chosen;
            }
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            var sprite = WhiteSprite();
            var winArt = Resources.Load<Sprite>("kit/panel_cream_round_lg");
            var scrim  = Resources.Load<Sprite>("kit/scrim_overlay");
            _rowArt = Resources.Load<Sprite>("kit/panel_cream");
            _btnArt = Resources.Load<Sprite>("kit/btn_teal_face");

            var rootImg = NewImage("StagePanel", canvas.transform, scrim != null ? scrim : sprite, scrim != null ? Color.white : new Color(0.06f, 0.07f, 0.10f, 0.96f), Vector2.zero, Vector2.one);
            if (scrim != null) rootImg.type = Image.Type.Sliced;
            _root = rootImg.gameObject;
            var oc = _root.AddComponent<Canvas>(); oc.overrideSorting = true; oc.sortingOrder = 31000;
            _root.AddComponent<GraphicRaycaster>();

            var win = NewImage("Window", _root.transform, winArt != null ? winArt : sprite, winArt != null ? Color.white : new Color(0.1f, 0.11f, 0.15f, 1f), new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.96f));
            if (winArt != null) win.type = Image.Type.Sliced;
            win.raycastTarget = false;

            NewText("StageTitle", _root.transform, "SELECT STAGE", 50, new Color(0.82f, 0.32f, 0.16f), new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.95f));

            var tiers = StageConfig.Tiers;
            _rows = new Row[tiers.Length];
            float top = 0.79f, h = 0.15f, gap = 0.03f;
            for (int i = 0; i < tiers.Length; i++)
            {
                float y1 = top - i * (h + gap), y0 = y1 - h;
                _rows[i] = BuildRow(sprite, i, new Vector2(0.08f, y0), new Vector2(0.92f, y1));
            }

            var closeImg = NewImage("StageClose", _root.transform, _btnArt != null ? _btnArt : sprite, _btnArt != null ? Color.white : new Color(0.4f, 0.4f, 0.45f), new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.16f));
            if (_btnArt != null) closeImg.type = Image.Type.Sliced;
            var closeBtn = closeImg.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(Close);
            var cl = NewText("CloseLabel", closeImg.transform, "BACK", 32, Color.white, Vector2.zero, Vector2.one);
            cl.raycastTarget = false;
        }

        private Row BuildRow(Sprite sprite, int index, Vector2 aMin, Vector2 aMax)
        {
            var bg = NewImage("StageRow", _root.transform, _rowArt != null ? _rowArt : sprite, _rowArt != null ? Color.white : new Color(0.14f, 0.15f, 0.19f, 0.95f), aMin, aMax);
            if (_rowArt != null) bg.type = Image.Type.Sliced;
            var info = NewText("Info", bg.transform, "", 25, Ink, new Vector2(0.05f, 0.05f), new Vector2(0.63f, 0.95f));
            info.alignment = TextAnchor.MiddleLeft;
            info.raycastTarget = false;

            var btnBg = NewImage("Select", bg.transform, _btnArt != null ? _btnArt : sprite, _btnArt != null ? Color.white : new Color(0.35f, 0.6f, 0.28f), new Vector2(0.66f, 0.15f), new Vector2(0.97f, 0.85f));
            if (_btnArt != null) btnBg.type = Image.Type.Sliced;
            var btn = btnBg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            var row = new Row { index = index, btnBg = btnBg, btn = btn };
            btn.onClick.AddListener(() => Select(row.index));
            var label = NewText("SelLabel", btnBg.transform, "", 22, Color.white, Vector2.zero, Vector2.one);
            label.raycastTarget = false;

            row.info = info; row.btnLabel = label;
            return row;
        }

        private Image NewImage(string name, Transform parent, Sprite s, Color c, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s; img.color = c;
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
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
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
