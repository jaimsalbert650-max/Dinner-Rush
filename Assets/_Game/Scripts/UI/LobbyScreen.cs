using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Main Menu (spec `02-main-menu.md`), built 1:1 from the Chef Survivor prototype: cream page,
    /// dark currency pills, cream circle buttons, two-colour wordmark, a dashed chef diorama that
    /// eats the remaining height, one red PLAY, and a teal/blue/yellow nav row.
    ///
    /// Geometry is written in *prototype px* on the design's 402x874 logical screen and converted
    /// once (<see cref="U"/>) — top-bar/wordmark hang off the top edge, PLAY/nav off the bottom, and
    /// the diorama stretches between them, so the column reflows on any aspect exactly like the
    /// design's flexbox does.
    ///
    /// Systems the design has no slot for are kept reachable rather than dropped: quests is the third
    /// top-bar circle, stage select is a tap on the diorama, and the run's energy cost rides as a chip
    /// on PLAY. Holds the run via Time.timeScale = 0 until PLAY, like the screen it replaces.
    /// </summary>
    public class LobbyScreen : MonoBehaviour
    {
        [Header("Frames / buttons")]
        [SerializeField] private Sprite pillFrame;        // BasicFrame_Round20 (legacy fallback)
        [SerializeField] private Sprite avatarFrame;      // Button_Circle82
        [SerializeField] private Sprite avatarPortrait;   // legacy chef (fallback)
        [SerializeField] private Texture2D chefSheet;     // chef2_cut.png (matches the in-game chef)

        // Region within chefSheet: the full assembled chef, shown in the menu diorama.
        private static readonly Rect RChefFigure = new Rect(67, 364, 505, 482);
        private Sprite _chefFigure;

        private Sprite ChefCut(Rect r)
            => chefSheet != null ? Sprite.Create(chefSheet, r, new Vector2(0.5f, 0.5f), 100f) : avatarPortrait;
        [SerializeField] private Sprite circleButton;     // Button_Circle82 (legacy fallback)
        [SerializeField] private Sprite stageFrame;       // BasicFrame_Round24
        [SerializeField] private Sprite stageArt;         // Background_Stage01_Icon
        [SerializeField] private Sprite stageDiorama;     // island2.png — illustrated diner diorama
        [SerializeField] private Sprite stageCounter;     // prop-counter (Assets cook/props)
        [SerializeField] private Sprite stagePlant;       // prop-plant (flowering)
        [SerializeField] private Sprite stageWallplant;   // prop-wallplant (tall floor plant)
        [SerializeField] private Sprite stageRug;         // prop-rug
        [SerializeField] private Sprite startButtonSprite;// Button01_l_Green
        [SerializeField] private Sprite dinerButton;      // button_green (diner)
        [SerializeField] private Sprite navButtonSprite;  // Button01_s_White_Bg
        [SerializeField] private Sprite battleButtonSprite;// Button_Circle118

        [Header("Icons")]
        [SerializeField] private Sprite energyIcon;       // Icon_Battery
        [SerializeField] private Sprite gemIcon;          // Icon_Gem
        [SerializeField] private Sprite coinIcon;         // Icon_Coin
        [SerializeField] private Sprite giftIcon;         // BtnIcon_Gift
        [SerializeField] private Sprite questsIcon;       // Icon_Bell
        [SerializeField] private Sprite badgeIcon;        // Icon_Exclamation (red "!" badge)
        [SerializeField] private Sprite bagIcon;          // Icon_Bag
        [SerializeField] private Sprite shopIcon;         // Icon_MenuIcon02_Shop
        [SerializeField] private Sprite rankIcon;         // Icon_Trophy01
        [SerializeField] private Sprite lockIcon;         // Icon_Lock
        [SerializeField] private Sprite cashIcon;         // Icon_Coin (placeholder)
        [SerializeField] private Sprite battleIcon;       // Icon_Sword01
        [SerializeField] private Sprite starIcon;         // GradeIcon_Star_l_Yellow

        /// <summary>The currency art wired here in the scene, shared with every other screen
        /// through <see cref="DesignUI.CoinIcon"/> / <see cref="DesignUI.GemIcon"/>.</summary>
        public Sprite CoinIcon => coinIcon;
        public Sprite GemIcon => gemIcon;
        public Sprite StarIcon => starIcon;
        public Sprite LockIcon => lockIcon;

        // ---- design units: the prototype's logical screen, converted once ----
        private const float DW = 402f, DH = 874f;
        /// <summary>Canvas px per prototype px. NOT a constant: with Canvas Scaler match 0.5 the canvas'
        /// logical width is only 1080 on an exactly 9:16 screen — on a 1170x2532 phone it is ~979 — so
        /// this is measured from the container the design's 402px maps onto (Build sets it).</summary>
        private float U = 1080f / DW;
        private int F(float px) => Mathf.Max(1, Mathf.RoundToInt(px * U));

        /// <summary>Energy a run costs. Spent on START in the Loadout, which is where the run really
        /// begins now that PLAY only opens Chef Select.</summary>
        public const int EnergyPerRun = 5;
        /// <summary>Diorama art baseline, in design px relative to the frame's centre: art (96) sits on
        /// it, the caption block (8 gap + 38) hangs under it, so -25 centres the whole column.</summary>
        private const float ChefBottom = -25f;

        private GameObject _root;
        private Text _coins, _gems, _stageName;
        private Text _energyCost;
        private Image _questsBadge, _dailyBadge;
        private RectTransform _chefTf;                   // gentle float, like the design's 3s loop
        private float _chefBaseY;
        private ShopPanel _shop;
        private QuestsPanel _quests;
        private StagePanel _stage;
        private DailyPanel _daily;
        private SettingsPanel _settings;
        private UpgradesPanel _upgrades;
        private ChefSelectPanel _chefs;
        private bool _raised;

        // Last values pushed into the labels, so a frame with no change does no work at all.
        // -1 means "nothing shown yet", which forces the first refresh to apply.
        private int _shownCoins = -1, _shownGems = -1, _shownQuests = -1, _shownDaily = -1;

        // Set when the player presses PLAY: the scene reloads so every system's Awake re-reads the
        // freshly-picked stage/loadout, then this flag makes the fresh lobby begin the run immediately
        // instead of showing itself. Static so it survives the scene reload. (#5/#8 correctness)
        private static bool _autoStart;

        // Reset the static once per play-session / build launch (NOT on LoadScene, so the reload flow
        // in Play() is preserved). Guards against a stale 'true' leaking across editor play sessions
        // when domain reload is disabled, which would wrongly skip the lobby.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAutoStart() => _autoStart = false;

        private void Start()
        {
            _shop = FindAnyObjectByType<ShopPanel>();
            _quests = FindAnyObjectByType<QuestsPanel>();
            _stage = FindAnyObjectByType<StagePanel>();
            if (_stage != null) _stage.SetOnChanged(RefreshStageName);
            _daily = FindAnyObjectByType<DailyPanel>();
            _settings = FindAnyObjectByType<SettingsPanel>();
            _upgrades = FindAnyObjectByType<UpgradesPanel>();
            _chefs = FindAnyObjectByType<ChefSelectPanel>();
            Build();

            if (_autoStart)                      // came from pressing PLAY in the previous lobby
            {
                _autoStart = false;
                Time.timeScale = 1f;
                _root.SetActive(false);          // run begins now with fresh selections applied
                return;
            }

            Time.timeScale = 0f;                 // hold the run until PLAY
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Refresh();
        }

        private void Update()
        {
            if (!_raised && _root != null && _root.activeSelf)
            {
                _root.transform.SetAsLastSibling();
                _raised = true;
            }
            if (_root == null || !_root.activeSelf) return;

            Refresh();

            // The lobby runs on timeScale 0, so everything here is unscaled.
            float ut = Time.unscaledTime;

            // Chef art floats 0 -> -8 -> 0 prototype px over 3s (the design's `floatY` keyframes).
            if (_chefTf != null)
                _chefTf.anchoredPosition = new Vector2(0f, _chefBaseY + (1f - Mathf.Cos(ut * (Mathf.PI * 2f / 3f))) * 0.5f * 8f * U);
        }

        private void OnDestroy()
        {
            if (_root != null && _root.activeSelf) Time.timeScale = 1f;
        }

        /// <summary>Called every frame, so it does nothing unless a value actually changed: the old
        /// version re-read PlayerPrefs and built a fresh formatted string per counter per frame, which
        /// is pure garbage generation while the player just looks at the menu.</summary>
        private void Refresh()
        {
            int coins = MetaProgress.Coins;
            if (coins != _shownCoins && _coins != null) { _shownCoins = coins; _coins.text = Format(coins); }

            int gems = PlayerGems.Gems;
            if (gems != _shownGems && _gems != null) { _shownGems = gems; _gems.text = Format(gems); }

            int quests = PlayerQuests.UnclaimedCount();
            if (quests != _shownQuests && _questsBadge != null) { _shownQuests = quests; _questsBadge.enabled = quests > 0; }

            int daily = DailyService.CanClaim ? 1 : 0;
            if (daily != _shownDaily && _dailyBadge != null) { _shownDaily = daily; _dailyBadge.enabled = daily == 1; }
        }

        private void RefreshStageName()
        {
            if (_stageName != null) _stageName.text = StageConfig.Current.name.ToUpperInvariant();
        }

        // ---------- actions ----------

        /// <summary>PLAY opens Chef Select (spec 02 §Exit); the run itself starts from the Loadout's
        /// START, which is where the energy is spent.</summary>
        private void Play()
        {
            if (_chefs == null) { Toast.Show("Chef select is coming soon!"); return; }
            _chefs.Open();
        }

        /// <summary>Arms the auto-start so the scene reload — which lets the spawner and the player
        /// re-read the fresh picks in their Awake — begins the run instead of showing the lobby.</summary>
        public static void BeginRun() => _autoStart = true;

        private void OpenShop()
        {
            if (_shop == null) { Toast.Show("Shop is coming soon!"); return; }
            Refresh();
            _shop.Open();
        }

        private void OpenUpgrades()
        {
            if (_upgrades == null) { Toast.Show("Upgrades are coming soon!"); return; }
            _upgrades.Open();
        }

        /// <summary>Tapping the chef art is a shortcut into Chef Select — the same place PLAY goes.</summary>
        private void OpenLoadout() => Play();

        private void OpenDaily()
        {
            if (_daily == null) { Toast.Show("Daily reward is coming soon!"); return; }
            _daily.Open();
        }

        private void OpenQuests()
        {
            if (_quests == null) { Toast.Show("Quests are coming soon!"); return; }
            _quests.Open();
        }

        private void OpenStages()
        {
            if (_stage == null) { Toast.Show("Only one stage in v1!"); return; }
            _stage.Open();
        }

        private void OpenSettings()
        {
            if (_settings == null) { Toast.Show("Settings are coming soon!"); return; }
            _settings.Open();
        }

        // ---------- build ----------
        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

            // Settles the canvas layout before anything is measured — the lobby is built in the first
            // frame, when the canvas rect is still the stale pre-layout width. Every screen shares the
            // scale this establishes, so the lobby's pills match the ones the panels draw.
            DesignUI.RecalcIfStale();

            // Page: flat cream (#FDF2DC), no gradient/vignette — the design is flat vector.
            _root = UIBuilder.Image("Lobby", canvas.transform, null, FlatUI.PageBg, Vector2.zero, Vector2.one).gameObject;

            // Draw the whole lobby ABOVE the paused game world — otherwise world sprites (the player
            // chef + its thrown spatula) poke through this screen-space-camera canvas. Same fix the
            // pause overlay uses. Below the title (25000) and pause (30000) overlays.
            var lobbyCanvas = _root.AddComponent<Canvas>();
            lobbyCanvas.overrideSorting = true;
            lobbyCanvas.sortingOrder = 20000;
            _root.AddComponent<GraphicRaycaster>();
            ScreenTransition.AddScreen(_root);   // the kit's screen-in (fade + rise), like every page

            // Safe-area content holder: insets by the device notch / front-camera cutout so the top
            // bar + nav row sit in the visible area (the cream page stays full-bleed behind it).
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(_root.transform, false);
            var crt = (RectTransform)content.transform;
            var sa = Screen.safeArea;
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            float x0 = Mathf.Clamp01(sa.xMin / sw), y0 = Mathf.Clamp01(sa.yMin / sh);
            float x1 = Mathf.Clamp01(sa.xMax / sw), y1 = Mathf.Clamp01(sa.yMax / sh);
            // The editor's device simulator can report a safe area from a different device than the
            // Game View resolution — a nonsense inset would push the whole menu off screen, so fall
            // back to full-bleed whenever the result isn't plausible.
            if (x1 - x0 < 0.5f || y1 - y0 < 0.5f) { x0 = 0f; y0 = 0f; x1 = 1f; y1 = 1f; }
            crt.anchorMin = new Vector2(x0, y0);
            crt.anchorMax = new Vector2(x1, y1);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var c = content.transform;

            // The design's 402px-wide screen maps onto this container — measure it instead of assuming
            // the 1080 reference width, which only holds on an exactly 9:16 screen.
            float containerW = crt.rect.width;
            if (containerW <= 1f) containerW = ((RectTransform)canvas.transform).rect.width;
            if (containerW > 1f) U = containerW / DW;

            _chefFigure = ChefCut(RChefFigure);

            BuildTopBar(c);
            BuildWordmark(c);
            BuildDiorama(c);
            BuildPlay(c);
            BuildNav(c);
            RefreshStageName();
        }

        // Row 1 — 44px tall: coin pill, gem pill (+ routes to shop), spacer, quests / settings / info.
        private void BuildTopBar(Transform p)
        {
            var coin = DarkPill("CoinPill", p, 18f, 112f);
            PillIcon(coin, coinIcon, new Color(1f, 0.78f, 0.20f));
            _coins = PillValue(coin, "0", 94f);

            var gem = DarkPill("GemPill", p, 120f, 228f);
            PillIcon(gem, gemIcon, FlatUI.Teal);
            // The + button eats the pill's right end, so the value stops short of it.
            _gems = PillValue(gem, "0", 108f, 27f);
            var plus = UIBuilder.Image("Plus", gem.transform, FlatUI.Circle, FlatUI.Teal, new Vector2(0.775f, 0.222f), new Vector2(0.955f, 0.778f));
            plus.preserveAspect = false; plus.raycastTarget = false;
            FlatUI.Label("PlusLabel", plus.transform, "+", F(14), FlatUI.Cream);
            var gemBtn = gem.gameObject.AddComponent<Button>();
            gemBtn.targetGraphic = gem; gemBtn.transition = Selectable.Transition.None;
            gemBtn.onClick.AddListener(OpenShop);

            // Quests is not in the design's top bar — it is kept here as a third circle so the system
            // stays reachable (the row's three 44px circles still end flush with the right padding).
            var questsFace = CircleButton(p, "Quests", 236f, OpenQuests);
            IconOrGlyph(questsFace, questsIcon, "!", 0.30f);
            _questsBadge = UIBuilder.Image("Badge", questsFace.transform, Kit("badge_notify"), Color.white,
                new Vector2(0.62f, 0.62f), new Vector2(1.06f, 1.06f));
            _questsBadge.raycastTarget = false;

            var setFace = CircleButton(p, "Settings", 288f, OpenSettings);
            IconOrGlyph(setFace, null, "⚙", 0.28f);

            var infoFace = CircleButton(p, "Info", 340f, () => Toast.Show("Chef Survivor — v1 UI shell, placeholders only"));
            FlatUI.Label("i", infoFace.transform, "i", F(20), FlatUI.Ink);
        }

        // Row 2 — the wordmark: `CHEF ` red + `SURVIVOR` ink, Lilita One 34px, one line.
        private void BuildWordmark(Transform p)
        {
            var go = new GameObject("Wordmark", typeof(RectTransform));
            go.transform.SetParent(p, false);
            TopRect((RectTransform)go.transform, 18f, 126f, 384f, 172f);
            var t = FlatUI.Label("Text", go.transform, "<color=#E8432C>CHEF </color><color=#3A2418>SURVIVOR</color>",
                F(34), Color.white);
            t.supportRichText = true;
        }

        // Row 3 — the diorama: takes every px left between the wordmark and PLAY (the design's flex:1).
        private void BuildDiorama(Transform p)
        {
            var frame = UIBuilder.Image("Diorama", p, Kit("panel_diorama_dashed"), Color.white, Vector2.zero, Vector2.one, true);
            frame.preserveAspect = false;
            frame.pixelsPerUnitMultiplier = 84f / (26f * U);   // sprite corner 84px -> the design's 26px
            var rt = frame.rectTransform;
            rt.anchorMin = new Vector2(28f / DW, 0f);          // 10px side inset, as a fraction
            rt.anchorMax = new Vector2((DW - 28f) / DW, 1f);
            rt.offsetMin = new Vector2(0f, 186f * U);          // PLAY row above the bottom
            rt.offsetMax = new Vector2(0f, -186f * U);         // wordmark row below the top

            // Art + caption are one *centred* column (the design's flex column: art, 8px gap, caption)
            // — NOT a stretched fill. Everything hangs off the diorama's centre so the group stays put
            // however tall the flexible frame ends up on a given aspect. ChefBottom is the offset that
            // puts the 96 + 8 + 38 px tall group's midpoint exactly on that centre.
            if (_chefFigure != null)
            {
                var chef = UIBuilder.Image("Chef", frame.transform, _chefFigure, Color.white, Vector2.zero, Vector2.one);
                chef.preserveAspect = true;
                var chefBtn = chef.gameObject.AddComponent<Button>();
                chefBtn.targetGraphic = chef; chefBtn.transition = Selectable.Transition.None;
                chefBtn.onClick.AddListener(OpenLoadout);
                var crt = chef.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0f);
                crt.sizeDelta = new Vector2(96f * U, 96f * U);
                crt.anchoredPosition = new Vector2(0f, ChefBottom * U);
                ChefArt.Straighten(chef);   // the sheet art is squat; match the in-game proportions
                _chefTf = crt;
                _chefBaseY = ChefBottom * U;
            }

            // Stage name + hint replace the design's `[PLACEHOLDER: Chef art]` caption.
            _stageName = FlatUI.Label("StageName", frame.transform, "", F(15), new Color(0.63f, 0.42f, 0.18f));
            Caption(_stageName.rectTransform, -ChefBottom + 8f, 20f);
            var hint = FlatUI.Label("Hint", frame.transform, "TAP TO CHANGE STAGE", F(11), new Color(0.63f, 0.42f, 0.18f, 0.9f), TextAnchor.MiddleCenter, Fonts.Body);
            hint.fontStyle = FontStyle.Bold;
            Caption(hint.rectTransform, -ChefBottom + 28f, 18f);

            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OpenStages);
        }

        // Row 4 — PLAY: full width, 60px, the only red button on the screen.
        private void BuildPlay(Transform p)
        {
            var play = Chunky.Button("Play", p, "red", Vector2.zero, Vector2.one, Play, out var face, 6f * U, 60f / (20f * U));
            BotRect((RectTransform)play.transform, 18f, 702f, 384f, 762f);
            FlatUI.Label("Label", face, "▶ PLAY", F(26), FlatUI.Cream);

            // Energy cost chip (not in the design — the run still costs energy, so it has to read here).
            var chip = UIBuilder.Image("EnergyChip", face, Kit("panel_dark_pill_soft"), Color.white,
                new Vector2(0.795f, 0.24f), new Vector2(0.965f, 0.76f), true);
            chip.preserveAspect = false; chip.raycastTarget = false;
            if (energyIcon != null)
            {
                var ic = UIBuilder.Image("Ic", chip.transform, energyIcon, Color.white, new Vector2(0.10f, 0.18f), new Vector2(0.46f, 0.82f));
                ic.raycastTarget = false;
            }
            _energyCost = FlatUI.Label("Cost", chip.transform, EnergyPerRun.ToString(), F(16), FlatUI.Cream, TextAnchor.MiddleRight);
            Frac(_energyCost.rectTransform, 0.46f, 0.05f, 0.86f, 0.95f);
        }

        // Row 5 — nav: three equal buttons, 10px gap, 52px tall.
        private void BuildNav(Transform p)
        {
            NavButton(p, "Shop", "teal", "SHOP", FlatUI.Cream, 18f, 133.33f, OpenShop);
            NavButton(p, "Upgrade", "blue", "UPGRADE", FlatUI.Cream, 143.33f, 258.67f, OpenUpgrades);
            var dailyFace = NavButton(p, "Daily", "yellow", "DAILY", FlatUI.Ink, 268.67f, 384f, OpenDaily);

            // badge_notify, 18px, pinned to the button's top-right corner (spec: clears on claim).
            _dailyBadge = UIBuilder.Image("Badge", dailyFace, Kit("badge_notify"), Color.white, Vector2.zero, Vector2.one);
            var brt = _dailyBadge.rectTransform;
            brt.anchorMin = brt.anchorMax = Vector2.one;
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(18f * U, 18f * U);
            brt.anchoredPosition = Vector2.zero;          // straddles the corner, half on the face
            _dailyBadge.raycastTarget = false;
        }

        private Transform NavButton(Transform p, string name, string color, string label, Color labelColor,
            float x0, float x1, UnityAction onClick)
        {
            var btn = Chunky.Button(name, p, color, Vector2.zero, Vector2.one, onClick, out var face, 4f * U, 60f / (16f * U));
            BotRect((RectTransform)btn.transform, x0, 776f, x1, 828f);
            FlatUI.Label("Label", face, label, F(16), labelColor);
            return face;
        }


        // ---------- small builders ----------

        /// <summary>Gold-ringed currency pill in the top-bar row (36px tall, centred in the 44px row).
        /// <see cref="DesignUI.GoldPill"/> owns the art; the lobby only passes its own measured U, which
        /// comes from the safe-area container rather than the whole canvas.</summary>
        private Image DarkPill(string name, Transform p, float x0, float x1)
        {
            var img = DesignUI.GoldPill(name, p, DesignUI.PillH, U);
            TopRect(img.rectTransform, x0, 68f, x1, 104f);
            return img;
        }

        /// <summary>Currency icon, dropped into the pill's gold ring.</summary>
        private void PillIcon(Image pill, Sprite icon, Color fallback)
            => DesignUI.PillSocket(pill, icon, fallback, DesignUI.PillH, 0.74f, U);

        /// <summary>Value text, starting where the socket ends. `width` is the pill's design width, so
        /// the px clearance can be turned back into the fractional anchor the rect rule wants.</summary>
        private Text PillValue(Image pill, string value, float width, float rightPx = 9f)
        {
            var t = FlatUI.Label("Value", pill.transform, value, F(13), FlatUI.Cream, TextAnchor.MiddleLeft, Fonts.Body);
            t.fontStyle = FontStyle.Bold;                    // Nunito 900 in the design
            Frac(t.rectTransform, DesignUI.PillTextStart() / width, 0.05f, 1f - rightPx / width, 0.95f);
            return t;
        }

        /// <summary>44px cream circle with the design's 3px hard border-shadow. Returns the face.</summary>
        private Image CircleButton(Transform p, string name, float x0, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(p, false);
            TopRect((RectTransform)go.transform, x0, 64f, x0 + 44f, 108f);

            var shadow = UIBuilder.Image("Shadow", go.transform, Kit("btn_circle_cream_shadow"), Color.white, Vector2.zero, Vector2.one);
            shadow.preserveAspect = false; shadow.raycastTarget = false;
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -3f * U);
            var face = UIBuilder.Image("Face", go.transform, Kit("btn_circle_cream"), Color.white, Vector2.zero, Vector2.one);
            face.preserveAspect = false;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = face; btn.transition = Selectable.Transition.None;
            if (onClick != null) btn.onClick.AddListener(onClick);
            go.AddComponent<ChunkyPress>().Init(face.rectTransform, shadow.rectTransform, 3f * U);
            return face;
        }

        /// <summary>Icon art when the slot is wired, otherwise the design's glyph so nothing renders blank.</summary>
        private void IconOrGlyph(Image face, Sprite icon, string glyph, float inset)
        {
            if (icon != null)
            {
                var img = UIBuilder.Image("Ic", face.transform, icon, FlatUI.Ink, new Vector2(inset, inset), new Vector2(1f - inset, 1f - inset));
                img.raycastTarget = false;
            }
            else FlatUI.Label("Glyph", face.transform, glyph, F(20), FlatUI.Ink);
        }

        // ---------- design-px placement ----------

        // Width comes from anchors (a fraction of the design's 402px), never from px insets on both
        // edges — an inset pair is only the right width while U matches the parent, so a canvas
        // resize would silently squash everything built earlier. See DesignUI for the full note.

        /// <summary>Anchors a rect to the parent's TOP edge using prototype-px coordinates measured
        /// from the top of the design's screen.</summary>
        private RectTransform TopRect(RectTransform rt, float x0, float yTop, float x1, float yBot)
        {
            rt.anchorMin = new Vector2(x0 / DW, 1f); rt.anchorMax = new Vector2(x1 / DW, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -yBot * U);
            rt.offsetMax = new Vector2(0f, -yTop * U);
            return rt;
        }

        /// <summary>Same, anchored to the parent's BOTTOM edge (y still measured from the design's top).</summary>
        private RectTransform BotRect(RectTransform rt, float x0, float yTop, float x1, float yBot)
        {
            rt.anchorMin = new Vector2(x0 / DW, 0f); rt.anchorMax = new Vector2(x1 / DW, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, (DH - yBot) * U);
            rt.offsetMax = new Vector2(0f, (DH - yTop) * U);
            return rt;
        }

        /// <summary>A caption line hanging below the parent's centre, `dropPx` design-px down.</summary>
        private void Caption(RectTransform rt, float dropPx, float heightPx)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(320f * U, heightPx * U);
            rt.anchoredPosition = new Vector2(0f, -dropPx * U);
        }

        /// <summary>Fractional sub-rect of a parent (for contents of a pill / card).</summary>
        private static void Frac(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static Sprite Kit(string name) => Resources.Load<Sprite>("kit/" + name);

        /// <summary>Thousands-separated (`1,250`), compact only once it would overflow the pill.</summary>
        private static string Format(int n)
        {
            if (n >= 1000000) return (n / 1000000f).ToString("0.#") + "M";
            if (n >= 100000) return (n / 1000f).ToString("0.#") + "K";
            return n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
