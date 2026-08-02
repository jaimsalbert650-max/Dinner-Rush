using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Meta Upgrades (spec `07-upgrades.md`): the coin sink between runs. Cream page, coin pill (no
    /// gems — gems don't buy upgrades), and one scrolling row per upgrade with an icon tile, name,
    /// description, a 6-pip level track and a buy button.
    ///
    /// Reads and writes <see cref="MetaProgress"/>, whose table this screen renders verbatim — adding
    /// an upgrade there needs no change here. Opened by UPGRADE on the Main Menu and after a run.
    /// </summary>
    public class UpgradesPanel : MonoBehaviour
    {
        private static readonly MetaProgress.Meta[] All =
            (MetaProgress.Meta[])System.Enum.GetValues(typeof(MetaProgress.Meta));

        private static readonly Color PipGold = new Color(0.909f, 0.651f, 0.078f);   // #E8A614

        private class Row
        {
            public MetaProgress.Meta meta;
            public Text cost;
            public Text label;
            public Image[] pips;
            public Image face, shadow;
            public CanvasGroup group;
        }

        private GameObject _root;
        private Text _coins;
        private Row[] _rows;

        private void Start()
        {
            Build();
            _root.SetActive(false);
            if (DesignUI.AutoOpen("upgrades")) Open();
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        private void Buy(MetaProgress.Meta m)
        {
            if (MetaProgress.IsMax(m)) return;
            if (!MetaProgress.CanBuy(m)) { UiSfx.PlayDenied(); Toast.Show("Not enough coins!"); return; }
            MetaProgress.Buy(m);
            UiSfx.Play(UiSfx.Purchase);
            Toast.Show(MetaProgress.Name(m) + " → LV " + MetaProgress.Level(m));
            Refresh();      // a purchase can make every other row unaffordable (spec §Purchase flow 4)
        }

        private void Refresh()
        {
            if (_coins != null) _coins.text = DesignUI.Num(MetaProgress.Coins);
            foreach (var r in _rows)
            {
                int lvl = MetaProgress.Level(r.meta);
                bool max = MetaProgress.IsMax(r.meta);
                bool afford = MetaProgress.CanBuy(r.meta);

                for (int i = 0; i < r.pips.Length; i++)
                    r.pips[i].sprite = DesignUI.Kit(i < lvl ? "pip_full" : "pip_empty");

                r.label.text = max ? "MAX" : "BUY";
                r.cost.gameObject.SetActive(!max);
                if (!max) r.cost.text = DesignUI.Num(MetaProgress.Cost(r.meta));

                // Never tint — the design swaps the sprite pair for the disabled state, because a tint
                // muddies the face's top highlight.
                string skin = (max || !afford) ? "grey" : "teal";
                r.face.sprite = DesignUI.Kit("btn_" + skin + "_face");
                r.shadow.sprite = DesignUI.Kit("btn_" + skin + "_shadow");
                r.group.alpha = (max || !afford) ? 0.7f : 1f;
            }
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("UpgradesPanel", canvas.transform);
            DesignUI.Header(_root.transform, "UPGRADES", Close);

            _coins = DesignUI.Pill(_root.transform, "CoinPill", (DesignUI.DW - 94f) * 0.5f, 118f, 94f,
                DesignUI.CoinIcon, new Color(1f, 0.78f, 0.2f), "0");

            var sub = DesignUI.Label("Subtitle", _root.transform, "Permanent boosts. They stick between runs!",
                DesignUI.F(13), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            sub.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(sub.rectTransform, 18f, 164f, 384f, 188f);

            DesignUI.Scroll(_root.transform, 196f, out var content);

            const float rowH = 78f, gap = 10f;
            _rows = new Row[All.Length];
            for (int i = 0; i < All.Length; i++)
                _rows[i] = BuildRow(content, All[i], i * (rowH + gap), rowH);

            DesignUI.SetContentHeight(content, All.Length * (rowH + gap) + 50f);
        }

        /// <summary>The stat each permanent upgrade raises, in the same art the level-up cards use.
        /// Lucky Tips pays out in coins, so it borrows the currency icon rather than a stat one.</summary>
        private static Sprite IconFor(MetaProgress.Meta meta)
        {
            switch (meta)
            {
                case MetaProgress.Meta.MaxHealth: return DesignUI.Kit("up_maxhp");
                case MetaProgress.Meta.MoveSpeed: return DesignUI.Kit("up_movespeed");
                case MetaProgress.Meta.CoinMagnet: return DesignUI.Kit("up_pickup");
                case MetaProgress.Meta.PanPower: return DesignUI.Kit("up_damage");
                case MetaProgress.Meta.LuckyTips: return DesignUI.CoinIcon;
                default: return null;
            }
        }

        private Row BuildRow(Transform content, MetaProgress.Meta meta, float yTop, float h)
        {
            var face = DesignUI.Card(content, "Row_" + meta, 18f);
            DesignUI.TopRect((RectTransform)face.transform.parent, 18f, yTop, 384f, yTop + h);

            // Icon tile — the same upgrade art the level-up cards use, so a permanent boost and the
            // in-run pick that raises the same stat read as the same thing. Dashed tile only if missing.
            var art = IconFor(meta);
            var tile = DesignUI.Raw("IconTile", face.transform, art, Color.white);
            tile.preserveAspect = true;
            tile.enabled = art != null;      // an empty slot, never a dashed box pretending to be art
            tile.raycastTarget = false;
            DesignUI.Frac(tile.rectTransform, 14f / 366f, 0.18f, 64f / 366f, 0.82f);

            var name = DesignUI.Label("Name", face.transform, MetaProgress.Name(meta), DesignUI.F(16), FlatUI.Ink, TextAnchor.MiddleLeft);
            DesignUI.Frac(name.rectTransform, 76f / 366f, 0.60f, 0.72f, 0.92f);

            var desc = DesignUI.Label("Desc", face.transform, MetaProgress.Describe(meta), DesignUI.F(12), DesignUI.Brown, TextAnchor.MiddleLeft, Fonts.Body);
            desc.fontStyle = FontStyle.Bold;
            DesignUI.Frac(desc.rectTransform, 76f / 366f, 0.36f, 0.72f, 0.60f);

            var pips = new Image[MetaProgress.MaxLevel];
            for (int i = 0; i < pips.Length; i++)
            {
                float x = 76f + i * 16f;
                pips[i] = DesignUI.Raw("Pip" + i, face.transform, DesignUI.Kit("pip_empty"), PipGold);
                pips[i].preserveAspect = true; pips[i].raycastTarget = false;
                DesignUI.Frac(pips[i].rectTransform, x / 366f, 0.10f, (x + 12f) / 366f, 0.32f);
            }

            var btn = Chunky.Button("Buy", face.transform, "teal", Vector2.zero, Vector2.one,
                () => Buy(meta), out var btnFace, 4f * DesignUI.U, 60f / (14f * DesignUI.U));
            DesignUI.Frac((RectTransform)btn.transform, 1f - 88f / 366f, 0.17f, 1f - 14f / 366f, 0.83f);
            var group = btn.gameObject.AddComponent<CanvasGroup>();

            var label = DesignUI.Label("Label", btnFace, "BUY", DesignUI.F(14), FlatUI.Cream);
            DesignUI.Frac(label.rectTransform, 0f, 0.42f, 1f, 0.92f);
            var cost = DesignUI.Label("Cost", btnFace, "", DesignUI.F(11), FlatUI.Cream, TextAnchor.MiddleCenter, Fonts.Body);
            cost.fontStyle = FontStyle.Bold;
            DesignUI.Frac(cost.rectTransform, 0f, 0.10f, 1f, 0.44f);

            return new Row
            {
                meta = meta, pips = pips, group = group, label = label, cost = cost,
                face = btnFace.GetComponent<Image>(),
                shadow = btn.transform.Find("Shadow").GetComponent<Image>(),
            };
        }
    }
}
