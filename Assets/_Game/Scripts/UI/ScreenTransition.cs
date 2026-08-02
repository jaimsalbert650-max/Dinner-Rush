using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// The kit's shared entry animations (`00-README.md` §Transitions), so no screen has to hand-roll
    /// them: a **screen** fades in while sliding up 40 proto-px over 0.22s OutCubic, and an **overlay**
    /// fades its scrim over 0.18s while its panel pops 0.7 → 1.05 → 1 over 0.30s OutBack.
    ///
    /// Everything runs on unscaled time because the menus and every overlay sit on timeScale 0.
    /// </summary>
    public class ScreenTransition : MonoBehaviour
    {
        private const float ScreenSeconds = 0.22f, ScrimSeconds = 0.18f, PopSeconds = 0.30f;
        private const float SlidePx = 40f;

        private static readonly List<ScreenTransition> Live = new();

        private bool _overlay;
        private CanvasGroup _group;
        private RectTransform _rt;
        private Vector2 _home;
        private readonly List<RectTransform> _panels = new();
        private float _t;

        /// <summary>Screen style: fade + rise.</summary>
        public static ScreenTransition AddScreen(GameObject go) => Add(go, false);

        /// <summary>Overlay style: scrim fade + panel pop.</summary>
        public static ScreenTransition AddOverlay(GameObject go) => Add(go, true);

        private static ScreenTransition Add(GameObject go, bool overlay)
        {
            var t = go.GetComponent<ScreenTransition>();
            if (t == null) t = go.AddComponent<ScreenTransition>();
            t._overlay = overlay;
            // AddComponent fires Awake/OnEnable immediately — i.e. *before* the line above — so the
            // first arm ran in screen mode. Re-arm now that the mode is actually known, otherwise an
            // overlay keeps the screen-mode slide and never puts it back.
            t.Arm();
            return t;
        }

        /// <summary>Snap every running transition to its finished state — used when a caller needs the
        /// UI settled immediately (editor captures, tests) rather than mid-animation.</summary>
        public static void CompleteAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Live[i].Complete();
        }

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            // Capture the resting position ONCE, before the first frame of animation displaces it.
            // Re-reading it in OnEnable would latch the mid-slide offset the second time a screen is
            // shown, and the screen would settle 40px low from then on.
            _home = _rt.anchoredPosition;
        }

        private void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
            // Opening a screen is also where its buttons pick up their tap sound — one hook point
            // instead of every button site having to remember to ask for one.
            UiSfx.Bind(gameObject);
            Arm();
        }

        /// <summary>A screen that is built and left active never re-enables, and the OnEnable above ran
        /// before its buttons existed — so bind once more on the first frame, when they do.</summary>
        private void Start() => UiSfx.Bind(gameObject);

        private void Arm()
        {
            if (_rt == null) return;                 // not awake yet; OnEnable will arm it
            _t = 0f;

            // Overlays pop their direct children (the column / dialog); a screen moves as one piece.
            _panels.Clear();
            if (_overlay)
                foreach (RectTransform child in _rt)
                    _panels.Add(child);

            Apply(0f);
        }

        private void OnDisable() => Live.Remove(this);

        private void Update()
        {
            if (_t >= 1f) return;
            float longest = _overlay ? PopSeconds : ScreenSeconds;
            _t = Mathf.Clamp01(_t + Time.unscaledDeltaTime / longest);
            Apply(_t);
        }

        public void Complete()
        {
            _t = 1f;
            Apply(1f);
        }

        private void Apply(float t)
        {
            if (_overlay)
            {
                _rt.anchoredPosition = _home;        // an overlay never slides, only its panel pops
                float scrim = Mathf.Clamp01(t * PopSeconds / ScrimSeconds);
                _group.alpha = Mathf.Lerp(0f, 1f, scrim);
                float pop = OutBack(t);
                for (int i = 0; i < _panels.Count; i++)
                    if (_panels[i] != null) _panels[i].localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, pop);
                return;
            }

            float e = OutCubic(t);
            _group.alpha = e;
            _rt.anchoredPosition = _home + new Vector2(0f, Mathf.Lerp(-SlidePx * DesignUI.U, 0f, e));
        }

        private static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>Overshoots to ~1.05 before settling, which is the 0.7 → 1.05 → 1 the spec draws.</summary>
        private static float OutBack(float t)
        {
            const float c1 = 0.7f, c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
