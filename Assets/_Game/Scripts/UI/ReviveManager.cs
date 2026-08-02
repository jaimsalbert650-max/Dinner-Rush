using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Revive overlay (spec `12-revive.md`): the rewarded-ad moment, offered at the peak of loss
    /// aversion, once per run, and easy to decline. Drives the death flow, so Game Over only appears
    /// once revive is declined, expired, or already spent.
    ///
    /// The 5s countdown runs on unscaled time and **pauses whenever the player cannot see it** (app
    /// unfocused, or another overlay on top) — an offer must never expire off-screen. The ad path is
    /// stubbed by <see cref="FakeAdService"/> until a real `IAdService` lands.
    /// </summary>
    public class ReviveManager : MonoBehaviour
    {
        [SerializeField] private int gemCost = 20;
        [SerializeField] private float window = 5f;

        private PlayerHealth _health;
        private GameOverPanel _gameOver;
        private GameObject _root;
        private Text _count, _gemLabel;
        private Image _gemFace, _gemShadow;
        private CanvasGroup _gemGroup;
        private float _t;
        private bool _open, _used, _focused = true;

        /// <summary>One revive per run, hard rule — Victory's star rating reads this.</summary>
        public bool ReviveUsed => _used;

        private void Start()
        {
            _health = FindAnyObjectByType<PlayerHealth>();
            _gameOver = FindAnyObjectByType<GameOverPanel>();
            Build();
            _root.SetActive(false);
            if (_health != null) _health.OnDied += OnDeath;
            if (DesignUI.AutoOpen("revive")) OnDeath();
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDied -= OnDeath;
            if (_open) Time.timeScale = 1f;
        }

        private void OnApplicationFocus(bool hasFocus) => _focused = hasFocus;

        private void OnDeath()
        {
            // Already spent, or nothing left to offer (no ad fill and not enough gems) -> skip the
            // overlay entirely rather than showing a dead one.
            if (_used || (!FakeAdService.HasFill && PlayerGems.Gems < gemCost)) { HandOff(); return; }
            _open = true;
            _t = window;
            Time.timeScale = 0f;
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (!_open) return;
            // Freeze while backgrounded or while something else is drawn on top of the offer.
            if (!_focused || !_root.activeInHierarchy) return;
            _t -= Time.unscaledDeltaTime;
            if (_count != null) _count.text = "⏱ offer closes in " + Mathf.CeilToInt(Mathf.Max(0f, _t)) + "s";
            if (_t <= 0f) Decline();
        }

        private void Refresh()
        {
            bool can = PlayerGems.Gems >= gemCost;
            string skin = can ? "purple" : "grey";
            if (_gemFace != null) _gemFace.sprite = DesignUI.Kit("btn_" + skin + "_face");
            if (_gemShadow != null) _gemShadow.sprite = DesignUI.Kit("btn_" + skin + "_shadow");
            if (_gemGroup != null) _gemGroup.alpha = can ? 1f : 0.7f;
            if (_gemLabel != null) _gemLabel.text = gemCost + " GEMS · REVIVE";
        }

        private void WatchAd()
        {
            FakeAdService.ShowRewarded(result =>
            {
                if (result == AdResult.Completed) { DoRevive(); Toast.Show("Revived! Back to the kitchen"); return; }
                if (result == AdResult.Skipped)
                {
                    Toast.Show("Ad not finished — no revive.");
                    _t = window;      // a skipped ad must not consume the offer (spec §Ad flow)
                    return;
                }
                Toast.Show("No ad available right now.");
            });
        }

        private void BuyWithGems()
        {
            // Deduct and revive atomically — never one without the other.
            if (PlayerGems.Gems < gemCost) { UiSfx.PlayDenied(); Toast.Show("Not enough gems!"); return; }
            if (!PlayerGems.Spend(gemCost)) { Refresh(); return; }
            DoRevive();
            Toast.Show("Revived! -" + gemCost + " gems");
        }

        private void DoRevive()
        {
            _used = true;
            _open = false;
            UiSfx.Play(UiSfx.Revive);
            _root.SetActive(false);
            if (_health != null) _health.Revive(1f, 2f);   // full HP + 2s invulnerability
            Time.timeScale = 1f;
        }

        private void Decline()
        {
            _open = false;
            _root.SetActive(false);
            HandOff();
        }

        private void HandOff()
        {
            if (_gameOver != null) _gameOver.Show();
            else Time.timeScale = 0f;   // nothing to fall back to; at least keep the run stopped
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Overlay("RevivePanel", canvas.transform, 31500, 0.78f);

            // Dialog: title 36 / sub 24 / art 110 / ad 56 / gem 52 / decline 30 / countdown 20 + gaps.
            var col = DesignUI.Column(_root.transform, 350f, 396f);
            var dialog = DesignUI.Raw("Dialog", col, DesignUI.Kit("panel_cream_round_lg"), Color.white);
            dialog.type = Image.Type.Sliced; dialog.pixelsPerUnitMultiplier = 84f / (26f * DesignUI.U);
            dialog.raycastTarget = false;

            var title = DesignUI.Label("Title", col, "YOU WENT DOWN!", DesignUI.F(28), FlatUI.Red);
            DesignUI.ColRect(title.rectTransform, 24f, 60f, 20f);

            var sub = DesignUI.Label("Sub", col, "Revive and keep cooking?", DesignUI.F(14), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            sub.fontStyle = FontStyle.Bold;
            DesignUI.ColRect(sub.rectTransform, 62f, 84f, 20f);

            // Knocked-down chef art slot — dashed placeholder until `chef_downed` exists.
            var art = DesignUI.Raw("Art", col, DesignUI.Kit("panel_placeholder_dashed"), Color.white);
            art.type = Image.Type.Sliced; art.pixelsPerUnitMultiplier = 54f / (20f * DesignUI.U);
            art.raycastTarget = false;
            var art_rt = DesignUI.ColRect(art.rectTransform, 94f, 204f);
            art_rt.anchorMin = new Vector2(0.5f, 1f); art_rt.anchorMax = new Vector2(0.5f, 1f);
            art_rt.pivot = new Vector2(0.5f, 1f);
            art_rt.sizeDelta = new Vector2(130f * DesignUI.U, 110f * DesignUI.U);
            art_rt.anchoredPosition = new Vector2(0f, -94f * DesignUI.U);

            var ad = Chunky.Button("WatchAd", col, "teal", Vector2.zero, Vector2.one, WatchAd, out var adFace,
                5f * DesignUI.U, 60f / (16f * DesignUI.U));
            DesignUI.ColRect((RectTransform)ad.transform, 216f, 272f, 24f);
            DesignUI.Label("Label", adFace, "▶ WATCH AD · REVIVE", DesignUI.F(19), FlatUI.Cream);
            // The ad CTA is hidden, never disabled, when there is no fill.
            ad.gameObject.SetActive(FakeAdService.HasFill);

            var gem = Chunky.Button("Gems", col, "purple", Vector2.zero, Vector2.one, BuyWithGems, out var gemFace,
                5f * DesignUI.U, 60f / (16f * DesignUI.U));
            DesignUI.ColRect((RectTransform)gem.transform, 284f, 336f, 24f);
            _gemGroup = gem.gameObject.AddComponent<CanvasGroup>();
            _gemFace = gemFace.GetComponent<Image>();
            _gemShadow = gem.transform.Find("Shadow").GetComponent<Image>();
            _gemLabel = DesignUI.Label("Label", gemFace, "", DesignUI.F(18), FlatUI.Cream);

            var decline = DesignUI.Label("Decline", col, "✖ No thanks", DesignUI.F(14), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            decline.fontStyle = FontStyle.Bold;
            decline.raycastTarget = true;
            DesignUI.ColRect(decline.rectTransform, 344f, 372f, 24f);
            var db = decline.gameObject.AddComponent<Button>();
            db.targetGraphic = decline; db.transition = Selectable.Transition.None;
            db.onClick.AddListener(Decline);

            _count = DesignUI.Label("Countdown", col, "", DesignUI.F(11), DesignUI.Muted, TextAnchor.MiddleCenter, Fonts.Body);
            _count.fontStyle = FontStyle.Bold;
            DesignUI.ColRect(_count.rectTransform, 372f, 394f, 24f);
        }
    }

    public enum AdResult { Completed, Skipped, Failed }

    /// <summary>
    /// Stand-in for rewarded video while there is no ad SDK: reports fill and completes immediately,
    /// like the clickable prototype. Replace with `IAdService` — `12-revive.md` §Ad flow lists the
    /// skipped / no-fill / app-killed cases the real implementation has to handle.
    /// </summary>
    internal static class FakeAdService
    {
        public static bool HasFill => true;

        public static void ShowRewarded(System.Action<AdResult> done)
        {
            Debug.Log("[FakeAdService] rewarded ad completed — no ad SDK integrated yet.");
            done(AdResult.Completed);
        }
    }
}
