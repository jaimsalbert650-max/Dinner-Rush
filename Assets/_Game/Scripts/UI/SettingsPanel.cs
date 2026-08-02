using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Settings (spec `08-settings.md`): boring on purpose — four groups, no surprises, and every
    /// control writes immediately, so there is no save/apply button. The pause overlay shows a reduced
    /// set of the same toggles and both write the same keys, so the two can never disagree.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        // Shared with the pause overlay. "opt_muted" stays in sync so the old mute path keeps working.
        public const string SoundKey = "opt_sfx_vol", MusicKey = "opt_music_vol", VibKey = "opt_haptics";
        public const string JoystickSideKey = "opt_joystick_side", LangKey = "opt_lang";

        private static readonly string[] Languages = { "EN", "ES", "FR", "DE" };

        private GameObject _root;
        private Slider _sound, _music;
        private Image _vibTrack; private RectTransform _vibKnob;
        private Image _joyL, _joyR; private Text _joyLText, _joyRText;
        private Text _langLabel, _cloudStatus;
        private float _writeAt = -1f;

        public static float SoundVolume => PlayerPrefs.GetFloat(SoundKey, 80f) / 100f;
        public static float MusicVolume => PlayerPrefs.GetFloat(MusicKey, 55f) / 100f;
        public static bool Vibration => PlayerPrefs.GetInt(VibKey, 0) == 1;
        /// <summary>'L' or 'R' — the in-run joystick reads this without needing a restart.</summary>
        public static char JoystickSide => PlayerPrefs.GetString(JoystickSideKey, "R")[0];

        private void Start()
        {
            Build();
            ApplyAudio();
            _root.SetActive(false);
            if (DesignUI.AutoOpen("settings")) Open();
        }

        public void Open()
        {
            Reflect();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        private void Update()
        {
            // Controls write to PlayerPrefs immediately but flush to disk debounced (spec §Persistence).
            if (_writeAt > 0f && Time.unscaledTime >= _writeAt) { _writeAt = -1f; PlayerPrefs.Save(); }
        }

        private void QueueWrite() => _writeAt = Time.unscaledTime + 0.5f;

        // ---------- state ----------

        private void SetVolume(string key, float v)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp(v, 0f, 100f));
            QueueWrite();
            ApplyAudio();
        }

        private void ToggleVibration()
        {
            PlayerPrefs.SetInt(VibKey, Vibration ? 0 : 1);
            QueueWrite();
            Reflect();
        }

        private void SetJoystickSide(char side)
        {
            PlayerPrefs.SetString(JoystickSideKey, side.ToString());
            QueueWrite();
            Reflect();
        }

        /// <summary>Cycles the stored locale, but says out loud that nothing is translated yet — the
        /// spec wants this bound to Unity Localization's available locales, and until it is, silently
        /// switching to "ES" would be a lie the UI tells the player.</summary>
        private void CycleLanguage()
        {
            int i = System.Array.IndexOf(Languages, PlayerPrefs.GetString(LangKey, "EN"));
            PlayerPrefs.SetString(LangKey, Languages[(i + 1 + Languages.Length) % Languages.Length]);
            QueueWrite();
            Reflect();
            Toast.Show("English only for now — translations land with localization.");
        }

        /// <summary>Sound at 0 mutes outright rather than sitting at -80dB, to save battery (spec).</summary>
        private void ApplyAudio()
        {
            float loudest = Mathf.Max(SoundVolume, MusicVolume);
            AudioListener.volume = loudest <= 0.001f ? 0f : loudest;
            PlayerPrefs.SetInt("opt_muted", loudest <= 0.001f ? 1 : 0);
        }

        private void Reflect()
        {
            if (_sound != null) _sound.SetValueWithoutNotify(PlayerPrefs.GetFloat(SoundKey, 80f));
            if (_music != null) _music.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicKey, 55f));

            bool vib = Vibration;
            if (_vibTrack != null) _vibTrack.sprite = DesignUI.Kit(vib ? "toggle_track_on" : "toggle_track_off");
            if (_vibKnob != null) DesignUI.Frac(_vibKnob, vib ? 0.52f : 0.06f, 0.10f, vib ? 0.94f : 0.48f, 0.90f);

            bool right = JoystickSide == 'R';
            if (_joyL != null) _joyL.enabled = !right;
            if (_joyR != null) _joyR.enabled = right;
            if (_joyLText != null) _joyLText.color = right ? DesignUI.Brown : FlatUI.Cream;
            if (_joyRText != null) _joyRText.color = right ? FlatUI.Cream : DesignUI.Brown;

            if (_langLabel != null) _langLabel.text = PlayerPrefs.GetString(LangKey, "EN") + " ▾";
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("SettingsPanel", canvas.transform);
            DesignUI.Header(_root.transform, "SETTINGS", Close);

            DesignUI.Scroll(_root.transform, 122f, out var content);

            float y = 8f;
            y = Group(content, "AUDIO", y, 3, out var audio);
            SliderRow(audio, 0, "Sound", SoundKey, 80f, out _sound);
            SliderRow(audio, 1, "Music", MusicKey, 55f, out _music);
            ToggleRow(audio, 2, "Vibration");

            y = Group(content, "GAME", y, 2, out var game);
            JoystickRow(game, 0);
            LanguageRow(game, 1);

            y = Group(content, "ACCOUNT", y, 2, out var account);
            ActionRow(account, 0, "↻ Restore Purchases", null, () => Toast.Show("Nothing to restore."));
            _cloudStatus = ActionRow(account, 1, "☁ Cloud Save", "✓ synced", () => Toast.Show("Cloud save synced"));
            _cloudStatus.color = FlatUI.Teal;

            y = Group(content, "LEGAL", y, 2, out var legal);
            ActionRow(legal, 0, "Privacy Policy", "→", () => Toast.Show("Opens in a web view"));
            ActionRow(legal, 1, "Terms of Service", "→", () => Toast.Show("Opens in a web view"));

            var footer = DesignUI.Label("Footer", content, "Chef Survivor v" + Application.version + " · UI shell",
                DesignUI.F(12), DesignUI.Muted, TextAnchor.MiddleCenter, Fonts.Body);
            footer.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(footer.rectTransform, 18f, y + 8f, 384f, y + 32f);

            DesignUI.SetContentHeight(content, y + 82f);
            Reflect();
        }

        /// <summary>Section label + its card. Returns the y where the next group starts.</summary>
        private float Group(Transform content, string title, float y, int rows, out Transform card)
        {
            DesignUI.Section(content, title, 18f, y);
            float top = y + 26f, h = rows * RowH + 12f;
            var face = DesignUI.Card(content, title + "Card", 18f);
            DesignUI.TopRect((RectTransform)face.transform.parent, 18f, top, 384f, top + h);
            card = face.transform;
            return top + h + 12f;
        }

        private const float RowH = 52f;

        /// <summary>Places a row inside a group card and draws the divider above it (never on the first).</summary>
        private RectTransform Row(Transform card, int index)
        {
            var go = new GameObject("Row" + index, typeof(RectTransform));
            go.transform.SetParent(card, false);
            var r = (RectTransform)go.transform;
            float pad = 6f * DesignUI.U, rowPx = RowH * DesignUI.U;
            r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(16f * DesignUI.U, -(pad + (index + 1) * rowPx));
            r.offsetMax = new Vector2(-16f * DesignUI.U, -(pad + index * rowPx));

            if (index > 0)
            {
                var div = DesignUI.Raw("Divider", card, null, FlatUI.CreamBorder);
                div.raycastTarget = false;
                var d = div.rectTransform;
                d.anchorMin = new Vector2(0f, 1f); d.anchorMax = new Vector2(1f, 1f); d.pivot = new Vector2(0.5f, 1f);
                d.offsetMin = new Vector2(16f * DesignUI.U, -(pad + index * rowPx) - 2f * DesignUI.U);
                d.offsetMax = new Vector2(-16f * DesignUI.U, -(pad + index * rowPx));
            }
            return r;
        }

        private Text RowLabel(Transform row, string text)
        {
            // Fixed 90px label column so every control in the group lines up (spec §AUDIO).
            var t = DesignUI.Label("Label", row, text, DesignUI.F(14), FlatUI.Ink, TextAnchor.MiddleLeft, Fonts.Body);
            t.fontStyle = FontStyle.Bold;
            DesignUI.Frac(t.rectTransform, 0f, 0f, 90f / 334f, 1f);
            return t;
        }

        private void SliderRow(Transform card, int index, string label, string key, float def, out Slider slider)
        {
            var row = Row(card, index);
            RowLabel(row, label);

            var holder = new GameObject("Slider", typeof(RectTransform));
            holder.transform.SetParent(row, false);
            DesignUI.Frac((RectTransform)holder.transform, 96f / 334f, 0.32f, 1f, 0.68f);

            var track = DesignUI.Raw("Track", holder.transform, DesignUI.Kit("bar_track_cream"), Color.white);
            track.type = Image.Type.Sliced; track.raycastTarget = true;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(holder.transform, false);
            DesignUI.Stretch((RectTransform)fillArea.transform, Vector2.zero, Vector2.one);
            var fill = DesignUI.Raw("Fill", fillArea.transform, DesignUI.Kit("bar_fill_red"), Color.white);
            fill.type = Image.Type.Sliced; fill.raycastTarget = false;

            slider = holder.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = true;
            slider.targetGraphic = track;
            slider.fillRect = fill.rectTransform;
            slider.value = PlayerPrefs.GetFloat(key, def);

            string k = key;
            slider.onValueChanged.AddListener(v => SetVolume(k, v));
        }

        private void ToggleRow(Transform card, int index, string label)
        {
            var row = Row(card, index);
            RowLabel(row, label);

            _vibTrack = DesignUI.Raw("Track", row, DesignUI.Kit("toggle_track_off"), Color.white);
            _vibTrack.type = Image.Type.Sliced;
            DesignUI.Frac(_vibTrack.rectTransform, 1f - 56f / 334f, 0.22f, 1f, 0.78f);

            var knob = DesignUI.Raw("Knob", _vibTrack.transform, DesignUI.Kit("toggle_knob"), Color.white);
            knob.preserveAspect = true; knob.raycastTarget = false;
            _vibKnob = knob.rectTransform;

            var b = _vibTrack.gameObject.AddComponent<Button>();
            b.targetGraphic = _vibTrack; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(ToggleVibration);
        }

        private void JoystickRow(Transform card, int index)
        {
            var row = Row(card, index);
            RowLabel(row, "Joystick side");

            var track = DesignUI.Raw("Segment", row, DesignUI.Kit("segment_track"), Color.white);
            track.type = Image.Type.Sliced; track.raycastTarget = false;
            DesignUI.Frac(track.rectTransform, 1f - 92f / 334f, 0.18f, 1f, 0.82f);

            _joyL = Segment(track.transform, "L", 0.04f, 0.49f, () => SetJoystickSide('L'), out _joyLText);
            _joyR = Segment(track.transform, "R", 0.51f, 0.96f, () => SetJoystickSide('R'), out _joyRText);
        }

        private Image Segment(Transform track, string text, float x0, float x1, UnityAction onClick, out Text label)
        {
            var holder = new GameObject("Seg" + text, typeof(RectTransform));
            holder.transform.SetParent(track, false);
            DesignUI.Frac((RectTransform)holder.transform, x0, 0.12f, x1, 0.88f);

            var active = DesignUI.Raw("Active", holder.transform, DesignUI.Kit("segment_active"), Color.white);
            active.type = Image.Type.Sliced; active.raycastTarget = false;

            label = DesignUI.Label("Label", holder.transform, text, DesignUI.F(14), FlatUI.Cream);
            var b = holder.AddComponent<Image>();
            b.color = new Color(0f, 0f, 0f, 0f);
            var btn = holder.AddComponent<Button>();
            btn.targetGraphic = b; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            return active;
        }

        private void LanguageRow(Transform card, int index)
        {
            var row = Row(card, index);
            RowLabel(row, "Language");

            var chip = DesignUI.Raw("LangChip", row, DesignUI.Kit("panel_cream"), FlatUI.CreamBorder);
            chip.type = Image.Type.Sliced; chip.pixelsPerUnitMultiplier = 60f / (12f * DesignUI.U);
            DesignUI.Frac(chip.rectTransform, 1f - 66f / 334f, 0.22f, 1f, 0.78f);
            _langLabel = DesignUI.Label("Label", chip.transform, "EN ▾", DesignUI.F(13), FlatUI.Ink, TextAnchor.MiddleCenter, Fonts.Body);
            _langLabel.fontStyle = FontStyle.Bold;

            var b = chip.gameObject.AddComponent<Button>();
            b.targetGraphic = chip; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(CycleLanguage);
        }

        /// <summary>A tappable row with an optional right-hand status/chevron. Returns that status text.</summary>
        private Text ActionRow(Transform card, int index, string label, string right, UnityAction onClick)
        {
            var row = Row(card, index);
            var l = DesignUI.Label("Label", row, label, DesignUI.F(14), FlatUI.Ink, TextAnchor.MiddleLeft, Fonts.Body);
            l.fontStyle = FontStyle.Bold;
            DesignUI.Frac(l.rectTransform, 0f, 0f, 0.7f, 1f);

            Text status = null;
            if (!string.IsNullOrEmpty(right))
            {
                status = DesignUI.Label("Status", row, right, DesignUI.F(12), DesignUI.Brown, TextAnchor.MiddleRight, Fonts.Body);
                status.fontStyle = FontStyle.Bold;
                DesignUI.Frac(status.rectTransform, 0.6f, 0f, 1f, 1f);
            }

            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var b = row.gameObject.AddComponent<Button>();
            b.targetGraphic = hit; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            return status;
        }
    }
}
