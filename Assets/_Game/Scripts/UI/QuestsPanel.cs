using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Quests screen: lists achievement-style quests with progress and a Claim button that grants
    /// gems. Opened from the lobby quests button. Built at runtime; reads <see cref="PlayerQuests"/>. #7.
    /// </summary>
    public class QuestsPanel : MonoBehaviour
    {
        private Font _font;
        private Sprite _rowArt, _btnArt;
        private GameObject _root;
        private Row[] _rows;

        private static readonly Color Ink = new Color(0.23f, 0.14f, 0.09f);

        private class Row { public PlayerQuests.Quest quest; public Text info; public Text claimLabel; public Image claimBg; public Button claim; }

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

        private void ClaimRow(Row r)
        {
            PlayerQuests.Claim(r.quest);
            Refresh();
        }

        private void Refresh()
        {
            foreach (var r in _rows)
            {
                int cur = Mathf.Min(r.quest.current(), r.quest.target);
                bool done = PlayerQuests.Complete(r.quest);
                bool claimed = PlayerQuests.Claimed(r.quest.id);
                r.info.text = r.quest.desc + "\n" + cur + " / " + r.quest.target + "   (+" + r.quest.reward + " gems)";
                r.claimLabel.text = claimed ? "DONE" : done ? "CLAIM" : cur + "/" + r.quest.target;
                r.claim.interactable = done && !claimed;
                r.claimBg.color = claimed ? new Color(0.6f, 0.6f, 0.64f)
                                  : done ? Color.white
                                  : new Color(0.72f, 0.74f, 0.8f);
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

            var rootImg = NewImage("QuestsPanel", canvas.transform, scrim != null ? scrim : sprite, scrim != null ? Color.white : new Color(0.06f, 0.07f, 0.10f, 0.96f), Vector2.zero, Vector2.one);
            if (scrim != null) rootImg.type = Image.Type.Sliced;
            _root = rootImg.gameObject;
            var oc = _root.AddComponent<Canvas>(); oc.overrideSorting = true; oc.sortingOrder = 31000;
            _root.AddComponent<GraphicRaycaster>();

            var win = NewImage("Window", _root.transform, winArt != null ? winArt : sprite, winArt != null ? Color.white : new Color(0.1f, 0.11f, 0.15f, 1f), new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.96f));
            if (winArt != null) win.type = Image.Type.Sliced;
            win.raycastTarget = false;

            NewText("QuestsTitle", _root.transform, "QUESTS", 52, new Color(0.82f, 0.32f, 0.16f), new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.95f));

            var quests = PlayerQuests.All;
            _rows = new Row[quests.Length];
            float top = 0.79f, h = 0.12f, gap = 0.025f;
            for (int i = 0; i < quests.Length; i++)
            {
                float y1 = top - i * (h + gap), y0 = y1 - h;
                _rows[i] = BuildRow(sprite, quests[i], new Vector2(0.08f, y0), new Vector2(0.92f, y1));
            }

            var closeImg = NewImage("QuestsClose", _root.transform, _btnArt != null ? _btnArt : sprite, _btnArt != null ? Color.white : new Color(0.4f, 0.4f, 0.45f), new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.18f));
            if (_btnArt != null) closeImg.type = Image.Type.Sliced;
            var closeBtn = closeImg.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(Close);
            var cl = NewText("CloseLabel", closeImg.transform, "BACK", 32, Color.white, Vector2.zero, Vector2.one);
            cl.raycastTarget = false;
        }

        private Row BuildRow(Sprite sprite, PlayerQuests.Quest quest, Vector2 aMin, Vector2 aMax)
        {
            var bg = NewImage("QuestRow", _root.transform, _rowArt != null ? _rowArt : sprite, _rowArt != null ? Color.white : new Color(0.14f, 0.15f, 0.19f, 0.95f), aMin, aMax);
            if (_rowArt != null) bg.type = Image.Type.Sliced;
            var info = NewText("Info", bg.transform, "", 24, Ink, new Vector2(0.05f, 0.05f), new Vector2(0.66f, 0.95f));
            info.alignment = TextAnchor.MiddleLeft;
            info.raycastTarget = false;

            var claimBg = NewImage("Claim", bg.transform, _btnArt != null ? _btnArt : sprite, _btnArt != null ? Color.white : new Color(0.35f, 0.6f, 0.28f), new Vector2(0.69f, 0.15f), new Vector2(0.97f, 0.85f));
            if (_btnArt != null) claimBg.type = Image.Type.Sliced;
            var claim = claimBg.gameObject.AddComponent<Button>();
            claim.targetGraphic = claimBg;
            var row = new Row { quest = quest, claimBg = claimBg, claim = claim };
            claim.onClick.AddListener(() => ClaimRow(row));
            var label = NewText("ClaimLabel", claimBg.transform, "", 24, Color.white, Vector2.zero, Vector2.one);
            label.raycastTarget = false;

            row.info = info; row.claimLabel = label;
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
