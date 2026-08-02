using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Chef Select (spec `03-chef-select.md`): pick a chef, and make the locked ones look worth their
    /// coins — which is why a locked card still shows its stats at full strength behind a dimmed
    /// portrait. Reached by PLAY on the Main Menu; SELECT goes on to the Loadout.
    ///
    /// Unlocking deliberately does **not** auto-advance: the player should see the card become theirs.
    /// </summary>
    public class ChefSelectPanel : MonoBehaviour
    {
        private GameObject _root;
        private Text _coins, _gems, _name, _tagline;
        private Image _portrait, _lock, _accent, _art;

        /// <summary>One backdrop hue per chef, in roster order — the only thing distinguishing the
        /// portraits while every chef is drawn with the same mascot art.</summary>
        private static readonly Color[] Accents =
        {
            new Color(0.98f, 0.78f, 0.42f),   // Line Cook  — warm butter
            new Color(0.93f, 0.47f, 0.36f),   // Pit Master — ember red
            new Color(0.45f, 0.74f, 0.86f),   // Sous Ninja — cool steel
            new Color(0.79f, 0.62f, 0.93f),   // Pastry Wizard — icing violet
        };
        private CanvasGroup _portraitGroup;
        private Image[] _hpPips, _spdPips;
        private Image[] _dots;
        private Image _ctaFace, _ctaShadow;
        private CanvasGroup _ctaGroup;
        private Text _ctaLabel;
        private int _index;
        private LoadoutPanel _loadout;

        private void Start()
        {
            _loadout = FindAnyObjectByType<LoadoutPanel>();
            Build();
            _root.SetActive(false);
            if (DesignUI.AutoOpen("chefs")) Open();
        }

        public void Open()
        {
            _index = PlayerLoadout.Selected;
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        private void Step(int delta)
        {
            int n = PlayerLoadout.Chefs.Length;
            _index = (_index + delta % n + n) % n;
            Refresh();
        }

        private void Confirm()
        {
            if (!PlayerLoadout.Owned(_index))
            {
                var chef = PlayerLoadout.Chefs[_index];
                if (MetaProgress.Coins < chef.cost) { UiSfx.PlayDenied(); Toast.Show("Not enough coins!"); return; }
                PlayerLoadout.Unlock(_index);
                UiSfx.Play(UiSfx.UnlockChef);
                Toast.Show("Chef unlocked!");
                Refresh();          // the CTA becomes SELECT in place — no auto-advance
                return;
            }

            PlayerLoadout.Selected = _index;
            Close();
            if (_loadout != null) _loadout.Open();
            else Toast.Show("Loadout is coming soon!");
        }

        private void Refresh()
        {
            var chef = PlayerLoadout.Chefs[_index];
            bool owned = PlayerLoadout.Owned(_index);
            bool afford = MetaProgress.Coins >= chef.cost;

            _coins.text = DesignUI.Num(MetaProgress.Coins);
            _gems.text = DesignUI.Num(PlayerGems.Gems);
            _name.text = "“" + chef.name + "”";
            _tagline.text = chef.tagline;

            for (int i = 0; i < _hpPips.Length; i++)
                _hpPips[i].sprite = DesignUI.Kit(i < chef.hp ? "pip_full" : "pip_empty");
            for (int i = 0; i < _spdPips.Length; i++)
                _spdPips[i].sprite = DesignUI.Kit(i < chef.spd ? "pip_full" : "pip_empty");

            if (_accent != null)
                _accent.color = Accents[_index % Accents.Length];
            // A locked chef reads as a silhouette: the backdrop stays coloured so the card is still
            // legible at a glance, but the cook himself goes dark behind the padlock.
            if (_art != null)
                _art.color = owned ? Color.white : new Color(0.32f, 0.26f, 0.24f, 0.85f);

            // Locked portraits dim, but the stats stay bright — they are the sell.
            _portraitGroup.alpha = owned ? 1f : 0.55f;
            _lock.enabled = !owned;

            for (int i = 0; i < _dots.Length; i++)
                _dots[i].sprite = DesignUI.Kit(i == _index ? "dot_active" : "dot_inactive");

            string skin = owned ? "red" : afford ? "teal" : "grey";
            _ctaFace.sprite = DesignUI.Kit("btn_" + skin + "_face");
            _ctaShadow.sprite = DesignUI.Kit("btn_" + skin + "_shadow");
            _ctaGroup.alpha = (!owned && !afford) ? 0.7f : 1f;
            _ctaLabel.text = owned
                ? (PlayerLoadout.Selected == _index ? "✓ SELECT" : "SELECT")
                : (afford ? "UNLOCK · " : "🔒 UNLOCK · ") + DesignUI.Num(chef.cost);
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("ChefSelectPanel", canvas.transform);
            DesignUI.Header(_root.transform, "CHOOSE CHEF", Close);

            float x = (DesignUI.DW - 196f) * 0.5f;
            _coins = DesignUI.Pill(_root.transform, "CoinPill", x, 118f, 94f, DesignUI.CoinIcon, FlatUI.Yellow, "0");
            _gems = DesignUI.Pill(_root.transform, "GemPill", x + 102f, 118f, 94f, DesignUI.GemIcon, FlatUI.Teal, "0");

            // Carousel: arrows flank a centred card column.
            Arrow("Prev", 18f, "◀", () => Step(-1));
            Arrow("Next", DesignUI.DW - 62f, "▶", () => Step(1));

            _portrait = DesignUI.Raw("Portrait", _root.transform, DesignUI.Kit("panel_diorama_dashed"), Color.white);
            _portrait.type = Image.Type.Sliced;
            _portrait.pixelsPerUnitMultiplier = 84f / (26f * DesignUI.U);
            DesignUI.TopRect(_portrait.rectTransform, (DesignUI.DW - 200f) * 0.5f, 176f, (DesignUI.DW + 200f) * 0.5f, 406f);
            _portraitGroup = _portrait.gameObject.AddComponent<CanvasGroup>();

            // A coloured disc behind the mascot, one hue per chef. The cook art is almost entirely
            // white, so tinting the sprite itself would recolour his face too — the backdrop gives
            // each chef its own identity while leaving the character alone.
            _accent = DesignUI.Raw("Accent", _portrait.transform, FlatUI.Circle, Color.white);
            _accent.raycastTarget = false;
            DesignUI.Frac(_accent.rectTransform, 0.16f, 0.12f, 0.84f, 0.80f);

            _art = DesignUI.Raw("ChefArt", _portrait.transform, ChefArt.Figure, Color.white);
            _art.preserveAspect = true; _art.raycastTarget = false;
            // Sized a little short of the frame so the 1.12 vertical correction below still fits it.
            DesignUI.Frac(_art.rectTransform, 0.16f, 0.14f, 0.84f, 0.80f);
            ChefArt.Straighten(_art);

            _lock = DesignUI.Raw("Lock", _portrait.transform,
                DesignUI.LockIcon != null ? DesignUI.LockIcon : DesignUI.Kit("btn_circle_dark"), Color.white);
            _lock.preserveAspect = true; _lock.raycastTarget = false;
            DesignUI.Frac(_lock.rectTransform, 0.40f, 0.40f, 0.60f, 0.60f);

            // Swipe the card area as well as the arrows — both must produce identical state.
            var swipe = _portrait.gameObject.AddComponent<SwipeArea>();
            swipe.Init(() => Step(-1), () => Step(1));

            _name = DesignUI.Label("Name", _root.transform, "", DesignUI.F(28), FlatUI.Ink);
            DesignUI.TopRect(_name.rectTransform, 18f, 418f, 384f, 456f);
            _tagline = DesignUI.Label("Tagline", _root.transform, "", DesignUI.F(14), DesignUI.Brown,
                TextAnchor.MiddleCenter, Fonts.Body);
            _tagline.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(_tagline.rectTransform, 18f, 458f, 384f, 480f);

            _hpPips = StatRow("HP", FlatUI.Red, 492f);
            _spdPips = StatRow("SPD", FlatUI.Teal, 520f);

            _dots = new Image[PlayerLoadout.Chefs.Length];
            float dotsW = _dots.Length * 12f + (_dots.Length - 1) * 10f;
            for (int i = 0; i < _dots.Length; i++)
            {
                float dx = (DesignUI.DW - dotsW) * 0.5f + i * 22f;
                _dots[i] = DesignUI.Raw("Dot" + i, _root.transform, DesignUI.Kit("dot_inactive"), Color.white);
                _dots[i].preserveAspect = true; _dots[i].raycastTarget = false;
                DesignUI.TopRect(_dots[i].rectTransform, dx, 556f, dx + 12f, 568f);
            }

            var cta = Chunky.Button("Cta", _root.transform, "red", Vector2.zero, Vector2.one, Confirm, out var face,
                6f * DesignUI.U, 60f / (20f * DesignUI.U));
            DesignUI.BotRect((RectTransform)cta.transform, 18f, DesignUI.DH - 46f - 58f, 384f, DesignUI.DH - 46f);
            _ctaGroup = cta.gameObject.AddComponent<CanvasGroup>();
            _ctaFace = face.GetComponent<Image>();
            _ctaShadow = cta.transform.Find("Shadow").GetComponent<Image>();
            _ctaLabel = DesignUI.Label("Label", face, "SELECT", DesignUI.F(20), FlatUI.Cream);
        }

        private void Arrow(string name, float x0, string glyph, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            DesignUI.TopRect((RectTransform)go.transform, x0, 269f, x0 + 44f, 313f);
            var shadow = DesignUI.Raw("Shadow", go.transform, DesignUI.Kit("btn_circle_cream_shadow"), Color.white);
            shadow.raycastTarget = false;
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -3f * DesignUI.U);
            var face = DesignUI.Raw("Face", go.transform, DesignUI.Kit("btn_circle_cream"), Color.white);
            DesignUI.Label("Glyph", face.transform, glyph, DesignUI.F(16), FlatUI.Ink);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = face; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            go.AddComponent<ChunkyPress>().Init(face.rectTransform, shadow.rectTransform, 3f * DesignUI.U);
        }

        private Image[] StatRow(string label, Color color, float yTop)
        {
            var l = DesignUI.Label(label, _root.transform, label, DesignUI.F(15), color, TextAnchor.MiddleRight);
            DesignUI.TopRect(l.rectTransform, 110f, yTop, 152f, yTop + 22f);

            var pips = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                float x = 162f + i * 20f;
                pips[i] = DesignUI.Raw(label + "Pip" + i, _root.transform, DesignUI.Kit("pip_empty"), color);
                pips[i].preserveAspect = true; pips[i].raycastTarget = false;
                DesignUI.TopRect(pips[i].rectTransform, x, yTop + 5f, x + 13f, yTop + 18f);
            }
            return pips;
        }
    }

    /// <summary>Horizontal swipe over a rect: 40px past the start commits, either direction.</summary>
    public class SwipeArea : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private System.Action _left, _right;
        private float _startX;
        private bool _fired;

        public void Init(System.Action onSwipeRight, System.Action onSwipeLeft)
        {
            _right = onSwipeRight; _left = onSwipeLeft;
        }

        public void OnBeginDrag(PointerEventData e) { _startX = e.position.x; _fired = false; }

        public void OnDrag(PointerEventData e)
        {
            if (_fired) return;
            float dx = e.position.x - _startX;
            if (Mathf.Abs(dx) < 40f * DesignUI.U) return;
            _fired = true;
            if (dx > 0f) _right?.Invoke(); else _left?.Invoke();
        }

        public void OnEndDrag(PointerEventData e) => _fired = false;
    }
}
