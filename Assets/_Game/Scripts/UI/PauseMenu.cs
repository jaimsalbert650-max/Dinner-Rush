using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Pause overlay (spec `11-pause.md`): resume fast, restart easily, and never let anyone quit a
    /// good run by accident — QUIT always goes through a confirm, and the destructive button sits on
    /// the right and is never the default.
    ///
    /// The three quick toggles here are on/off only (volume sliders live in Settings) and write the
    /// same keys <see cref="SettingsPanel"/> does, so the two screens can never disagree: muting here
    /// stores the previous non-zero volume and restores it on unmute.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private const string PrevSoundKey = "opt_sfx_vol_prev", PrevMusicKey = "opt_music_vol_prev";

        private GameObject _overlay, _confirm;
        private bool _paused;
        private float _lastToggle = -1f;

        private readonly Image[] _toggleTracks = new Image[3];
        private readonly RectTransform[] _toggleKnobs = new RectTransform[3];

        private void Start()
        {
            Build();
            _overlay.SetActive(false);
            if (DesignUI.AutoOpen("pause")) SetPaused(true);
        }

        private void OnDestroy()
        {
            if (_paused) Time.timeScale = 1f;   // safety
        }

#if !UNITY_EDITOR
        // Auto-pause when the app is backgrounded (incoming call, notification, task switch) so the
        // player doesn't take damage while away. Editor-excluded: focus flips constantly in dev.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !_paused && Time.timeScale != 0f) SetPaused(true);
        }
#endif

        /// <summary>Opened by the HUD's pause button — the design puts it in the top bar, so the HUD
        /// owns the button and this owns the overlay.</summary>
        public void Toggle() => TogglePause();

        private void TogglePause()
        {
            if (Time.unscaledTime - _lastToggle < 0.2f) return;      // spam debounce (spec §Edge cases)
            _lastToggle = Time.unscaledTime;
            if (!_paused && Time.timeScale == 0f) return;            // another screen already owns the pause
            SetPaused(!_paused);
        }

        private void SetPaused(bool p)
        {
            _paused = p;
            Time.timeScale = p ? 0f : 1f;
            _overlay.SetActive(p);
            if (_confirm != null) _confirm.SetActive(false);
            if (p)
            {
                Reflect();
                _overlay.transform.SetAsLastSibling();
            }
        }

        private void Resume() => SetPaused(false);

        /// <summary>Restart and quit both reload the scene. Run coins are banked as they are picked
        /// up, not at run end, so the confirm's "Coins are kept!" is literally true either way.</summary>
        private void Reload()
        {
            _paused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ---------- quick toggles ----------

        private static float Volume(int i)
        {
            if (i == 0) return PlayerPrefs.GetFloat(SettingsPanel.SoundKey, 80f);
            if (i == 1) return PlayerPrefs.GetFloat(SettingsPanel.MusicKey, 55f);
            return PlayerPrefs.GetInt(SettingsPanel.VibKey, 0) * 100f;
        }

        private static bool On(int i) => Volume(i) > 0.001f;

        private void Toggle(int i)
        {
            if (i == 2)
            {
                PlayerPrefs.SetInt(SettingsPanel.VibKey, On(2) ? 0 : 1);
            }
            else
            {
                string key = i == 0 ? SettingsPanel.SoundKey : SettingsPanel.MusicKey;
                string prevKey = i == 0 ? PrevSoundKey : PrevMusicKey;
                float def = i == 0 ? 80f : 55f;
                if (On(i))
                {
                    PlayerPrefs.SetFloat(prevKey, Volume(i));   // remember the level so unmute restores it
                    PlayerPrefs.SetFloat(key, 0f);
                }
                else PlayerPrefs.SetFloat(key, PlayerPrefs.GetFloat(prevKey, def));
                AudioListener.volume = Mathf.Max(Volume(0), Volume(1)) / 100f;
            }
            PlayerPrefs.Save();
            Reflect();
        }

        private void Reflect()
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = On(i);
                if (_toggleTracks[i] != null) _toggleTracks[i].sprite = DesignUI.Kit(on ? "toggle_track_on" : "toggle_track_off");
                if (_toggleKnobs[i] != null) DesignUI.Frac(_toggleKnobs[i], on ? 0.52f : 0.06f, 0.10f, on ? 0.94f : 0.48f, 0.90f);
            }
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            // The pause *button* lives in the HUD's top row (spec 09), not here.
            _overlay = DesignUI.Overlay("PauseOverlay", canvas.transform, 30000);

            // Column: title 48 / RESUME 56 / RESTART 52 / toggles 156 / QUIT 52, 12px gaps.
            var col = DesignUI.Column(_overlay.transform, 346f, 412f);

            var title = DesignUI.Label("Title", col, "PAUSED", DesignUI.F(38), FlatUI.Cream);
            DesignUI.ColRect(title.rectTransform, 0f, 48f);

            Cta(col, "Resume", "red", "▶ RESUME", FlatUI.Cream, 21f, 60f, 116f, Resume);
            Cta(col, "Restart", "yellow", "RESTART RUN", FlatUI.Ink, 18f, 128f, 180f, Reload);

            BuildToggles(col, 192f);

            // QUIT uses the outline face — it must not read as a primary action.
            var quit = DesignUI.Raw("Quit", col, DesignUI.Kit("btn_cream_outline_face"), Color.white);
            quit.type = Image.Type.Sliced; quit.pixelsPerUnitMultiplier = 60f / (16f * DesignUI.U);
            DesignUI.ColRect(quit.rectTransform, 360f, 412f);
            DesignUI.Label("Label", quit.transform, "✖ QUIT TO MENU", DesignUI.F(17), FlatUI.Red);
            var qb = quit.gameObject.AddComponent<Button>();
            qb.targetGraphic = quit; qb.transition = Selectable.Transition.None;
            qb.onClick.AddListener(() => { _confirm.SetActive(true); _confirm.transform.SetAsLastSibling(); });

            BuildConfirm(_overlay.transform);
        }

        private void Cta(Transform col, string name, string color, string label, Color labelColor,
            float fontPx, float yTop, float yBot, UnityAction onClick)
        {
            var btn = Chunky.Button(name, col, color, Vector2.zero, Vector2.one, onClick, out var face,
                5f * DesignUI.U, 60f / (16f * DesignUI.U));
            DesignUI.ColRect((RectTransform)btn.transform, yTop, yBot);
            DesignUI.Label("Label", face, label, DesignUI.F(fontPx), labelColor);
        }

        private void BuildToggles(Transform col, float yTop)
        {
            string[] names = { "Sound", "Music", "Vibrate" };
            var face = DesignUI.Card(col, "Toggles", 18f);
            DesignUI.ColRect((RectTransform)face.transform.parent, yTop, yTop + 156f);

            for (int i = 0; i < 3; i++)
            {
                float y0 = 6f + i * 48f;
                var row = new GameObject("Row" + i, typeof(RectTransform));
                row.transform.SetParent(face.transform, false);
                DesignUI.ColRect((RectTransform)row.transform, y0, y0 + 48f, 16f);

                var lbl = DesignUI.Label("Label", row.transform, names[i], DesignUI.F(14), FlatUI.Ink, TextAnchor.MiddleLeft, Fonts.Body);
                lbl.fontStyle = FontStyle.Bold;
                DesignUI.Frac(lbl.rectTransform, 0f, 0f, 0.6f, 1f);

                var track = DesignUI.Raw("Track", row.transform, DesignUI.Kit("toggle_track_off"), Color.white);
                track.type = Image.Type.Sliced;
                DesignUI.Frac(track.rectTransform, 1f - 56f / 314f, 0.22f, 1f, 0.78f);
                _toggleTracks[i] = track;

                var knob = DesignUI.Raw("Knob", track.transform, DesignUI.Kit("toggle_knob"), Color.white);
                knob.preserveAspect = true; knob.raycastTarget = false;
                _toggleKnobs[i] = knob.rectTransform;

                int idx = i;
                var b = track.gameObject.AddComponent<Button>();
                b.targetGraphic = track; b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Toggle(idx));

                if (i < 2)
                {
                    var div = DesignUI.Raw("Divider", face.transform, null, FlatUI.CreamBorder);
                    div.raycastTarget = false;
                    DesignUI.ColRect(div.rectTransform, y0 + 48f, y0 + 50f, 16f);
                }
            }
        }

        private void BuildConfirm(Transform overlay)
        {
            _confirm = DesignUI.Overlay("QuitConfirm", overlay, 30500);
            _confirm.GetComponent<Image>().color = new Color(24f / 255f, 10f / 255f, 4f / 255f, 0.6f);

            var col = DesignUI.Column(_confirm.transform, 330f, 168f);

            var dialog = DesignUI.Raw("Dialog", col, DesignUI.Kit("panel_cream_round_lg"), Color.white);
            dialog.type = Image.Type.Sliced; dialog.pixelsPerUnitMultiplier = 84f / (22f * DesignUI.U);

            var title = DesignUI.Label("Title", col, "Quit this run?", DesignUI.F(22), FlatUI.Ink);
            DesignUI.ColRect(title.rectTransform, 24f, 56f, 24f);

            var body = DesignUI.Label("Body", col, "Run progress will be lost. Coins are kept!",
                DesignUI.F(13), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            body.fontStyle = FontStyle.Bold;
            DesignUI.ColRect(body.rectTransform, 60f, 88f, 24f);

            // CANCEL left, QUIT right — destructive on the right, never the default (spec).
            var cancel = DesignUI.Raw("Cancel", col, DesignUI.Kit("panel_cream"), FlatUI.CreamBorder);
            cancel.type = Image.Type.Sliced; cancel.pixelsPerUnitMultiplier = 60f / (14f * DesignUI.U);
            Half(cancel.rectTransform, true);
            DesignUI.Label("Label", cancel.transform, "CANCEL", DesignUI.F(15), FlatUI.Ink);
            var cb = cancel.gameObject.AddComponent<Button>();
            cb.targetGraphic = cancel; cb.transition = Selectable.Transition.None;
            cb.onClick.AddListener(() => _confirm.SetActive(false));

            var quit = Chunky.Button("Quit", col, "red", Vector2.zero, Vector2.one, Reload, out var qf,
                4f * DesignUI.U, 60f / (14f * DesignUI.U));
            Half((RectTransform)quit.transform, false);
            DesignUI.Label("Label", qf, "QUIT", DesignUI.F(15), FlatUI.Cream);

            _confirm.SetActive(false);
        }

        /// <summary>The confirm's button row: left or right half, 24px outer inset, 10px between.</summary>
        private static void Half(RectTransform rt, bool left)
        {
            rt.anchorMin = new Vector2(left ? 0f : 0.5f, 1f);
            rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2((left ? 24f : 5f) * DesignUI.U, -144f * DesignUI.U);
            rt.offsetMax = new Vector2((left ? -5f : -24f) * DesignUI.U, -96f * DesignUI.U);
        }
    }
}
