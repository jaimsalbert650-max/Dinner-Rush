using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Boot splash (spec `01-splash.md`): one readable brand beat covering the real boot cost, and the
    /// only screen the user cannot interact with — no back, no tap-to-skip (tap-to-skip invites a
    /// double-tap straight into the menu's PLAY).
    ///
    /// It leaves when boot work is done **and** at least 1.2s has passed, with a hard 8s cap. There is
    /// no async boot pipeline yet, so <see cref="Progress"/> is a stand-in that a real loader should
    /// drive (save 20% / addressables 50% / store + remote 30%); the visual is eased so the bar never
    /// jumps, never regresses, and never sits full. Runs on unscaled time — the lobby holds the game
    /// at timeScale 0 underneath.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        private const float MinSeconds = 1.2f, MaxSeconds = 8f, FadeSeconds = 0.25f;

        private static readonly string[] Status =
        {
            "Preheating the kitchen…", "Sharpening the knives…", "Plating up…",
        };

        private GameObject _root;
        private CanvasGroup _group;
        private Image _fill;
        private RectTransform _logo;
        private Text _status;
        private float _t, _shown, _fadeT = -1f;
        private bool _done;

        /// <summary>0..1 boot progress. Replace with the real loader's weighted total.</summary>
        public float Progress { get; set; }

        private void Start() => Build();

        private void Update()
        {
            if (_done || _root == null) return;
            _t += Time.unscaledDeltaTime;

            // No real async boot yet: approximate it so the bar behaves, and let a loader override.
            float target = Mathf.Max(Progress, Mathf.Clamp01(_t / MinSeconds));
            _shown = Mathf.Max(_shown, Mathf.Lerp(_shown, target, Time.unscaledDeltaTime / 0.2f));
            if (_fill != null) _fill.fillAmount = Mathf.Max(0.05f, _shown);

            if (_logo != null)
                _logo.anchoredPosition = new Vector2(0f, Mathf.Sin(_t * (Mathf.PI * 2f / 2.6f)) * 8f * DesignUI.U);

            if (_status != null)
            {
                // The status line only rotates when boot drags past 3s (spec §Copy).
                int i = _t > 3f ? Mathf.Min(Status.Length - 1, (int)((_t - 3f) / 1.4f) + 1) : 0;
                _status.text = Status[i];
                float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(_t * (Mathf.PI * 2f / 1.4f)));
                _status.color = new Color(1f, 0.976f, 0.925f, 0.75f * pulse);
            }

            bool ready = (_t >= MinSeconds && _shown >= 0.999f) || _t >= MaxSeconds;
            if (ready && _fadeT < 0f) _fadeT = 0f;

            if (_fadeT >= 0f)
            {
                _fadeT += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(_fadeT / FadeSeconds);
                if (_fadeT >= FadeSeconds) { _done = true; _root.SetActive(false); }
            }
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            DesignUI.RecalcIfStale();

            // Top of the stack, with a raycaster so no tap leaks through to the lobby underneath.
            _root = DesignUI.Page("Splash", canvas.transform, 32000);
            _root.GetComponent<Image>().color = new Color(0.541f, 0.141f, 0.09f);   // #8A2417

            // CanvasGroup is [DisallowMultipleComponent] and the page already has one (ScreenTransition
            // adds it), so AddComponent here would return null and the fade-out below would throw every
            // frame — which is exactly how the splash got stuck on screen. Reuse the existing group.
            _group = _root.GetComponent<CanvasGroup>();
            if (_group == null) _group = _root.AddComponent<CanvasGroup>();
            var bg = DesignUI.Raw("Bg", _root.transform, DesignUI.Kit("bg_splash"), Color.white);
            bg.raycastTarget = false;

            // Centred column: logo 150 / wordmark 2 x 46 / bar 16 / status, 22px gaps.
            var col = DesignUI.Column(_root.transform, 322f, 340f);

            // The mascot is the logo: the cook the player actually controls, cut from the same sheet
            // the in-game character is built from, so the splash promises exactly what starts.
            var logo = DesignUI.Raw("Logo", col, ChefArt.Figure, Color.white);
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            _logo = logo.rectTransform;
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 1f);
            _logo.pivot = new Vector2(0.5f, 1f);
            _logo.sizeDelta = new Vector2(150f * DesignUI.U, 150f * DesignUI.U);
            _logo.anchoredPosition = Vector2.zero;
            ChefArt.Straighten(logo);   // the sheet art is squat; match the in-game proportions

            var l1 = DesignUI.Label("Chef", col, "CHEF", DesignUI.F(46), FlatUI.Yellow);
            DesignUI.ColRect(l1.rectTransform, 172f, 220f);
            var l2 = DesignUI.Label("Survivor", col, "SURVIVOR", DesignUI.F(46), FlatUI.Cream);
            DesignUI.ColRect(l2.rectTransform, 212f, 260f);   // line 2 pulled up 8px

            var track = DesignUI.Raw("LoadTrack", col, DesignUI.Kit("bar_track_dark"), Color.white);
            track.type = Image.Type.Sliced; track.raycastTarget = false;
            var trt = DesignUI.ColRect(track.rectTransform, 286f, 302f);
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(250f * DesignUI.U, 16f * DesignUI.U);
            trt.anchoredPosition = new Vector2(0f, -286f * DesignUI.U);

            _fill = DesignUI.Raw("LoadFill", track.transform, DesignUI.Kit("bar_fill_yellow"), Color.white);
            DesignUI.Stretch(_fill.rectTransform, new Vector2(0.01f, 0.15f), new Vector2(0.99f, 0.85f));
            _fill.type = Image.Type.Filled; _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left; _fill.fillAmount = 0.05f;
            _fill.raycastTarget = false;

            _status = DesignUI.Label("Status", col, Status[0], DesignUI.F(13),
                new Color(1f, 0.976f, 0.925f, 0.75f), TextAnchor.MiddleCenter, Fonts.Body);
            _status.fontStyle = FontStyle.Bold;
            DesignUI.ColRect(_status.rectTransform, 316f, 340f);
        }
    }
}
