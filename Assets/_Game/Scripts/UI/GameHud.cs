using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// In-run HUD (spec `09-hud.md`): four numbers and a joystick, none of them in the way — this is a
    /// bullet-heaven, so the centre of the screen stays clear. Owns the whole run-time HUD: top bar
    /// (pause, timer, run coins), XP bar + level chip, bottom-centre health, the joystick and the
    /// first-runs hint.
    ///
    /// Nothing here formats a string per frame: the timer only rebuilds when the integer second
    /// changes and each counter only when its value does, so an idle HUD allocates nothing.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private const string RunCountKey = "hud_runs_started";
        private const int HintRuns = 3;                 // show the control hint for the first 3 runs

        private PlayerHealth _hp;
        private PlayerExperience _xp;
        private PauseMenu _pause;

        private Text _timer, _coins, _hpText, _level, _waveText;
        private WaveManager _waves;
        private int _lastWaveShown = -1, _lastWaveKills = -1;
        private Image _xpFill, _hpFill;
        private RectTransform _joystick, _joystickArea;
        private FloatingJoystick _stick;
        private Sprite _hpGreen, _hpRed;

        private int _lastSecond = -1, _lastCoins = -1, _lastHp = -1, _lastLevel = -1;
        private char _side;
        private float _hpShown = 1f, _xpShown;

        private void Start()
        {
            _hp = FindAnyObjectByType<PlayerHealth>();
            _xp = FindAnyObjectByType<PlayerExperience>();
            _pause = FindAnyObjectByType<PauseMenu>();
            _waves = FindAnyObjectByType<WaveManager>();
            _hpGreen = DesignUI.Kit("bar_fill_green");
            _hpRed = DesignUI.Kit("bar_fill_red");
            PlayerPrefs.SetInt(RunCountKey, PlayerPrefs.GetInt(RunCountKey, 0) + 1);
            Build();
        }

        /// <summary>
        /// Snaps an eased value onto its target once it is within half a screen pixel of it.
        ///
        /// `Mathf.Lerp` toward a target is asymptotic — it never actually arrives. Feeding that
        /// straight into `Image.fillAmount` meant the bar changed by a millionth every frame
        /// forever, and `Image` only skips work when the new value is *exactly* the old one. So the
        /// HUD canvas re-tessellated and re-batched every single frame of the run, including while
        /// the player stood still at full health. Landing on the target lets uGUI go quiet.
        /// </summary>
        private static float Settle(float value, float target)
        {
            // The bars are ~330 design px wide, so this is well under one pixel of movement.
            return Mathf.Abs(value - target) < 0.0015f ? target : value;
        }

        private void Update()
        {
            var gs = GameStats.Instance;

            // Timer — clamped at 59:59, re-formatted only when the integer second changes.
            int total = Mathf.Min(3599, (int)(gs != null ? gs.Elapsed : 0f));
            if (total != _lastSecond)
            {
                _lastSecond = total;
                if (_timer != null) _timer.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
            }

            int coins = gs != null ? gs.Coins : 0;
            if (coins != _lastCoins)
            {
                _lastCoins = coins;
                if (_coins != null) _coins.text = DesignUI.Num(coins);
            }

            if (_hp != null && _hpFill != null)
            {
                float target = _hp.Max > 0f ? Mathf.Clamp01(_hp.Current / _hp.Max) : 0f;
                _hpShown = Settle(Mathf.Lerp(_hpShown, target, Time.unscaledDeltaTime / 0.3f), target);
                _hpFill.fillAmount = _hpShown;

                // Below 30% the fill turns red and pulses slowly — no audio nag (spec §Feedback).
                bool low = target < 0.3f;
                _hpFill.sprite = low ? _hpRed : _hpGreen;
                _hpFill.color = low
                    ? new Color(1f, 1f, 1f, 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / 1.2f))))
                    : Color.white;

                int cur = Mathf.CeilToInt(_hp.Current);
                if (cur != _lastHp)
                {
                    _lastHp = cur;
                    if (_hpText != null) _hpText.text = "HP " + cur + "/" + Mathf.RoundToInt(_hp.Max);
                }
            }

            if (_xp != null && _xpFill != null)
            {
                float xpTarget = _xp.Progress01;
                _xpShown = Settle(Mathf.Lerp(_xpShown, xpTarget, Time.unscaledDeltaTime / 0.25f), xpTarget);
                _xpFill.fillAmount = _xpShown;
                if (_xp.Level != _lastLevel)
                {
                    _lastLevel = _xp.Level;
                    if (_level != null) _level.text = "LV " + _lastLevel;
                }
            }

            // Wave quota — rebuilt only when a number actually moves, like every other counter here.
            if (_waveText != null && _waves != null)
            {
                int w = _waves.CurrentWave, k = _waves.KillsThisWave;
                if (w != _lastWaveShown || k != _lastWaveKills)
                {
                    _lastWaveShown = w; _lastWaveKills = k;
                    _waveText.text = _waves.Running
                        ? "WAVE " + w + "/" + _waves.WaveCount + "  ·  " + k + "/" + _waves.KillsRequired
                        : "ALL WAVES CLEARED";
                }
            }

            // The joystick side must apply live when it changes in Settings (spec §Edge cases).
            if (_joystick != null && SettingsPanel.JoystickSide != _side) PlaceJoystick();
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            DesignUI.RecalcIfStale();

            // Below the pause layer (15000) and every overlay, above the arena.
            var root = DesignUI.Page("GameHud", canvas.transform, 1000);
            var bg = root.GetComponent<Image>();
            // Disabled, not merely transparent: a zero-alpha Image is still tessellated and blended
            // across the whole screen every frame. It must not eat arena taps either.
            bg.raycastTarget = false;
            bg.enabled = false;

            // The kit sprite already carries the dark-to-transparent gradient — tinting it would only
            // wash it out, so it is drawn as authored and just stretched across the top.
            var scrim = DesignUI.Raw("TopScrim", root.transform, DesignUI.Kit("scrim_top_fade"), Color.white);
            scrim.type = Image.Type.Sliced; scrim.raycastTarget = false;
            DesignUI.TopRect(scrim.rectTransform, 0f, 0f, DesignUI.DW, 150f);

            BuildTopRow(root.transform);
            BuildXpRow(root.transform);
            BuildHealth(root.transform);
            BuildJoystick(root.transform);

            if (PlayerPrefs.GetInt(RunCountKey, 1) <= HintRuns)
            {
                var hint = DesignUI.Label("Hint", root.transform, "auto-fire · joystick to move", DesignUI.F(10),
                    new Color(1f, 0.976f, 0.925f, 0.55f), TextAnchor.MiddleCenter, Fonts.Body);
                hint.fontStyle = FontStyle.Bold;
                DesignUI.BotRect(hint.rectTransform, 14f, DesignUI.DH - 66f, DesignUI.DW - 14f, DesignUI.DH - 52f);
            }
        }

        private void BuildTopRow(Transform root)
        {
            // Pause: glass circle with the design's 3px bevel, left of the timer.
            var holder = new GameObject("Pause", typeof(RectTransform));
            holder.transform.SetParent(root, false);
            DesignUI.TopRect((RectTransform)holder.transform, 14f, 58f, 58f, 102f);
            var shadow = DesignUI.Raw("Shadow", holder.transform, DesignUI.Kit("btn_circle_hud_glass"), new Color(0f, 0f, 0f, 0.3f));
            shadow.raycastTarget = false;
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -3f * DesignUI.U);
            var face = DesignUI.Raw("Face", holder.transform, DesignUI.Kit("btn_circle_hud_glass"), Color.white);
            DesignUI.Label("Glyph", face.transform, "II", DesignUI.F(18), FlatUI.Cream);
            var btn = holder.AddComponent<Button>();
            btn.targetGraphic = face; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { if (_pause != null) _pause.Toggle(); });
            holder.AddComponent<ChunkyPress>().Init(face.rectTransform, shadow.rectTransform, 3f * DesignUI.U);

            _timer = DesignUI.Label("Timer", root, "00:00", DesignUI.F(22), FlatUI.Cream, TextAnchor.MiddleLeft);
            DesignUI.TopRect(_timer.rectTransform, 68f, 58f, 220f, 102f);

            var pill = DesignUI.Raw("CoinPill", root, DesignUI.Kit("panel_dark_pill_soft"), Color.white);
            pill.type = Image.Type.Sliced; pill.raycastTarget = false;
            DesignUI.TopRect(pill.rectTransform, 292f, 64f, 382f, 96f);
            var ic = DesignUI.Raw("Icon", pill.transform, DesignUI.CoinIcon != null ? DesignUI.CoinIcon : FlatUI.Circle,
                DesignUI.CoinIcon != null ? Color.white : FlatUI.Yellow);
            ic.preserveAspect = DesignUI.CoinIcon != null; ic.raycastTarget = false;
            DesignUI.Frac(ic.rectTransform, 0.08f, 0.2f, 0.32f, 0.8f);
            _coins = DesignUI.Label("Value", pill.transform, "0", DesignUI.F(14), FlatUI.Yellow, TextAnchor.MiddleLeft, Fonts.Body);
            _coins.fontStyle = FontStyle.Bold;
            DesignUI.Frac(_coins.rectTransform, 0.38f, 0.05f, 0.94f, 0.95f);
        }

        private void BuildXpRow(Transform root)
        {
            var track = DesignUI.Raw("XpTrack", root, DesignUI.Kit("bar_track_dark"), Color.white);
            track.type = Image.Type.Sliced; track.raycastTarget = false;
            DesignUI.TopRect(track.rectTransform, 14f, 110f, 340f, 124f);

            _xpFill = DesignUI.Raw("XpFill", track.transform, DesignUI.Kit("bar_fill_yellow"), Color.white);
            DesignUI.Stretch(_xpFill.rectTransform, new Vector2(0.005f, 0.12f), new Vector2(0.995f, 0.88f));
            _xpFill.type = Image.Type.Filled; _xpFill.fillMethod = Image.FillMethod.Horizontal;
            _xpFill.fillOrigin = (int)Image.OriginHorizontal.Left; _xpFill.fillAmount = 0f;
            _xpFill.raycastTarget = false;

            var chip = DesignUI.Raw("LevelChip", root, DesignUI.Kit("chip_yellow_face"), Color.white);
            chip.type = Image.Type.Sliced; chip.pixelsPerUnitMultiplier = 30f / (10f * DesignUI.U);
            chip.raycastTarget = false;
            DesignUI.TopRect(chip.rectTransform, 346f, 104f, 388f, 130f);
            _level = DesignUI.Label("Label", chip.transform, "LV 1", DesignUI.F(14), FlatUI.Ink);

            // Waves are cleared by killing a quota, so the player needs to see how far along the
            // current one is. Kept as a line of text rather than a third bar: the HUD already has
            // XP and health bars, and the design does not have a slot for another one.
            _waveText = DesignUI.Label("WaveProgress", root, "", DesignUI.F(11),
                new Color(1f, 0.976f, 0.925f, 0.85f), TextAnchor.MiddleLeft, Fonts.Body);
            _waveText.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(_waveText.rectTransform, 16f, 132f, DesignUI.DW - 14f, 150f);
        }

        private void BuildHealth(Transform root)
        {
            var track = DesignUI.Raw("HpTrack", root, DesignUI.Kit("bar_track_dark"), Color.white);
            track.type = Image.Type.Sliced; track.raycastTarget = false;
            DesignUI.BotRect(track.rectTransform, (DesignUI.DW - 190f) * 0.5f, DesignUI.DH - 226f,
                (DesignUI.DW + 190f) * 0.5f, DesignUI.DH - 210f);

            _hpFill = DesignUI.Raw("HpFill", track.transform, _hpGreen, Color.white);
            DesignUI.Stretch(_hpFill.rectTransform, new Vector2(0.005f, 0.12f), new Vector2(0.995f, 0.88f));
            _hpFill.type = Image.Type.Filled; _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left; _hpFill.fillAmount = 1f;
            _hpFill.raycastTarget = false;

            _hpText = DesignUI.Label("HpText", root, "HP", DesignUI.F(11),
                new Color(1f, 0.976f, 0.925f, 0.9f), TextAnchor.MiddleCenter, Fonts.Body);
            _hpText.fontStyle = FontStyle.Bold;
            DesignUI.BotRect(_hpText.rectTransform, 14f, DesignUI.DH - 208f, DesignUI.DW - 14f, DesignUI.DH - 192f);
        }

        /// <summary>
        /// The scene's joystick was decorative — it carried an Image and a skin script but no input
        /// component at all, so touch never moved the player. It is now driven by
        /// <see cref="FloatingJoystick"/> against the same `&lt;Gamepad&gt;/leftStick` binding
        /// <see cref="PlayerMovement"/> already listens for: a touch anywhere in the bottom 40% of the
        /// screen re-centres the base under the finger, and releasing returns it to its resting corner.
        /// </summary>
        private void BuildJoystick(Transform root)
        {
            // Touch region: the bottom 40% of the screen, transparent but raycastable.
            //
            // This stays a transparent Image on purpose. It is a full-width blended quad every
            // frame and a no-geometry Graphic would remove that, but whether such a region still
            // receives touches could not be verified here: `GraphicRaycaster` skips graphics with
            // `depth == -1`, and in this editor a probe canvas reports depth -1 for an ordinary
            // transparent Image too — so the test cannot tell a working region from a dead one.
            // Not worth risking the control the player moves with for one quad of fill.
            var area = DesignUI.Raw("JoystickArea", root, null, new Color(0f, 0f, 0f, 0f));
            var art = area.rectTransform;
            art.anchorMin = Vector2.zero; art.anchorMax = new Vector2(1f, 0.4f);
            art.offsetMin = Vector2.zero; art.offsetMax = Vector2.zero;

            var existing = GameObject.Find("Joystick");
            GameObject go;
            if (existing != null)
            {
                go = existing;
                go.transform.SetParent(area.transform, false);
            }
            else
            {
                go = new GameObject("Joystick", typeof(RectTransform));
                go.transform.SetParent(area.transform, false);
                go.AddComponent<Image>();
            }

            var baseImg = go.GetComponent<Image>();
            if (baseImg == null) baseImg = go.AddComponent<Image>();
            baseImg.sprite = DesignUI.Kit("joystick_base");
            baseImg.color = Color.white;
            baseImg.preserveAspect = false;
            baseImg.raycastTarget = false;          // the whole area is the target, not the ring

            Image knob = null;
            if (go.transform.childCount > 0) knob = go.transform.GetChild(0).GetComponent<Image>();
            if (knob == null) knob = DesignUI.Raw("Knob", go.transform, null, Color.white);
            knob.sprite = DesignUI.Kit("joystick_knob");
            knob.color = Color.white;
            knob.preserveAspect = true;
            knob.raycastTarget = false;

            // Fixed-size base and knob, centred on the base's own pivot so the stick can be moved by
            // simply writing anchoredPosition.
            var brt = (RectTransform)go.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(110f * DesignUI.U, 110f * DesignUI.U);

            var krt = knob.rectTransform;
            krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f);
            krt.pivot = new Vector2(0.5f, 0.5f);
            krt.sizeDelta = new Vector2(52f * DesignUI.U, 52f * DesignUI.U);
            krt.anchoredPosition = Vector2.zero;

            _joystick = brt;
            _joystickArea = art;
            PlaceJoystick();

            var stick = area.gameObject.AddComponent<FloatingJoystick>();
            stick.Init(art, brt, krt, 55f * DesignUI.U);
            _stick = stick;
        }

        /// <summary>110px base resting 34px in from its side edge and 80px up from the bottom, inside
        /// the bottom-40% touch region.</summary>
        private void PlaceJoystick()
        {
            _side = SettingsPanel.JoystickSide;
            float halfW = _joystickArea.rect.width * 0.5f, halfH = _joystickArea.rect.height * 0.5f;
            float x = _side == 'L'
                ? -halfW + (34f + 55f) * DesignUI.U
                : halfW - (34f + 55f) * DesignUI.U;
            float y = -halfH + (80f + 55f) * DesignUI.U;
            var home = new Vector2(x, y);
            _joystick.anchoredPosition = home;
            if (_stick != null) _stick.SetHome(home);
        }
    }
}
