using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Game Over (spec `13-game-over.md`): show the run honestly, report the payout, and get the
    /// player into the next one — RETRY is the loudest thing on screen.
    ///
    /// Coins are banked continuously **during** the run, not here, so a crash never costs the player
    /// their coins; this screen only reports the total. Both buttons are dead for 0.4s on entry so a
    /// panic tap during death cannot skip past it.
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        private const float InputLockSeconds = 0.4f;

        private PlayerHealth _health;
        private UpgradesPanel _upgrades;   // the coin sink after a run (spec 07)
        private GameObject _root, _doubleCta, _doubledRow;
        private Text[] _statValues;
        private Text _payout;
        private CanvasGroup _buttons;
        private float _unlockAt;
        private int _coinsEarned;
        private bool _doubled;

        private static readonly string[] StatLabels = { "Survived", "Level reached", "Customers fed", "Best combo" };

        private void Start()
        {
            _health = FindAnyObjectByType<PlayerHealth>();
            _upgrades = FindAnyObjectByType<UpgradesPanel>();
            Build();
            _root.SetActive(false);
            // Death is routed through ReviveManager (revive first, then game over). If no ReviveManager
            // is present in the scene, fall back to subscribing directly so death still ends the run.
            if (_health != null && FindAnyObjectByType<ReviveManager>() == null) _health.OnDied += Show;
            if (DesignUI.AutoOpen("gameover")) Show();
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDied -= Show;
        }

        /// <summary>Show the screen and bank the run. Called by ReviveManager when revive is declined
        /// or unavailable, or directly off death when no ReviveManager exists.</summary>
        public void Show()
        {
            Time.timeScale = 0f;
            var gs = GameStats.Instance;
            int kills = gs != null ? gs.Kills : 0;
            float rewardMult = StageConfig.Current.rewardMult;               // harder tiers pay more (#5)
            var pr = FindAnyObjectByType<PlayerRoot>();
            float coinMult = pr != null ? pr.Stats.CoinMult : 1f;            // Tip Jar + Lucky Tips
            _coinsEarned = Mathf.RoundToInt((gs != null ? gs.Coins : 0) * rewardMult * coinMult);
            MetaProgress.AddCoins(_coinsEarned);
            PlayerPrefs.SetInt("total_kills", PlayerPrefs.GetInt("total_kills", 0) + kills);   // for quests

            float t = gs != null ? gs.Elapsed : 0f;
            int m = (int)(t / 60f), s = (int)(t % 60f);
            int gems = Mathf.RoundToInt((1 + m) * rewardMult);   // premium gems: 1/run + 1/minute, tier-scaled
            PlayerGems.Add(gems);

            float best = PlayerPrefs.GetFloat("meta_best_time", 0f);
            if (t > best) { PlayerPrefs.SetFloat("meta_best_time", t); PlayerPrefs.Save(); }

            PlayerProfile.AddXp(kills * 2 + m * 4);

            var xp = FindAnyObjectByType<PlayerExperience>();
            var combo = FindAnyObjectByType<ComboHud>();
            _statValues[0].text = m.ToString("00") + ":" + s.ToString("00");
            _statValues[1].text = (xp != null ? xp.Level : 1).ToString();
            _statValues[2].text = DesignUI.Num(kills);
            _statValues[3].text = "x" + (combo != null ? combo.BestCombo : 0);
            _payout.text = "+" + DesignUI.Num(_coinsEarned);

            _doubled = false;
            // The bundle already pays for itself, so it removes this one rewarded placement.
            _doubleCta.SetActive(!ShopPanel.AdsRemoved);
            _doubledRow.SetActive(false);

            _unlockAt = Time.unscaledTime + InputLockSeconds;
            _buttons.interactable = false;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf || _buttons.interactable) return;
            if (Time.unscaledTime >= _unlockAt) _buttons.interactable = true;
        }

        private void DoubleCoins()
        {
            if (_doubled) return;
            _doubled = true;
            MetaProgress.AddCoins(_coinsEarned);
            _payout.text = "+" + DesignUI.Num(_coinsEarned * 2);
            _doubleCta.SetActive(false);
            _doubledRow.SetActive(true);       // replaced in place, never left as a disabled button
            Toast.Show("+" + DesignUI.Num(_coinsEarned) + " coins doubled!");
        }

        private void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Menu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("GameOverPanel", canvas.transform, 30000);
            var bg = DesignUI.Raw("Bg", _root.transform, DesignUI.Kit("bg_gameover"), Color.white);
            bg.raycastTarget = false;
            _root.GetComponent<Image>().color = new Color(0.227f, 0.047f, 0.024f);   // #3A0C06 behind the art

            var title = DesignUI.Label("Title", _root.transform, "GAME OVER", DesignUI.F(44), new Color(0.949f, 0.333f, 0.22f));
            DesignUI.TopRect(title.rectTransform, 24f, 64f, 378f, 118f);

            BuildStats(146f);

            _payout = DesignUI.Label("Payout", _root.transform, "+0", DesignUI.F(28), FlatUI.Yellow);
            DesignUI.TopRect(_payout.rectTransform, 24f, 330f, 378f, 372f);

            var buttons = new GameObject("Buttons", typeof(RectTransform));
            buttons.transform.SetParent(_root.transform, false);
            DesignUI.Stretch((RectTransform)buttons.transform, Vector2.zero, Vector2.one);
            _buttons = buttons.AddComponent<CanvasGroup>();

            var dbl = Chunky.Button("Double", buttons.transform, "teal", Vector2.zero, Vector2.one, DoubleCoins, out var dblFace,
                5f * DesignUI.U, 60f / (16f * DesignUI.U));
            DesignUI.BotRect((RectTransform)dbl.transform, 24f, DesignUI.DH - 50f - 122f, 378f, DesignUI.DH - 50f - 66f);
            DesignUI.Label("Label", dblFace, "▶ 2× COINS (watch ad)", DesignUI.F(19), FlatUI.Cream);
            _doubleCta = dbl.gameObject;

            _doubledRow = DesignUI.Raw("Doubled", buttons.transform, DesignUI.Kit("panel_scrim_card"), Color.white).gameObject;
            var drt = (RectTransform)_doubledRow.transform;
            ((Image)_doubledRow.GetComponent<Image>()).type = Image.Type.Sliced;
            DesignUI.BotRect(drt, 24f, DesignUI.DH - 50f - 118f, 378f, DesignUI.DH - 50f - 66f);
            DesignUI.Label("Label", _doubledRow.transform, "✓ COINS DOUBLED", DesignUI.F(16),
                new Color(1f, 0.976f, 0.925f, 0.6f), TextAnchor.MiddleCenter, Fonts.Body);
            _doubledRow.SetActive(false);

            // RETRY loud on the left, MENU quiet on the right.
            var retry = Chunky.Button("Retry", buttons.transform, "red", Vector2.zero, Vector2.one, Retry, out var retryFace,
                5f * DesignUI.U, 60f / (16f * DesignUI.U));
            DesignUI.BotRect((RectTransform)retry.transform, 24f, DesignUI.DH - 50f - 56f, 195f, DesignUI.DH - 50f);
            DesignUI.Label("Label", retryFace, "RETRY", DesignUI.F(19), FlatUI.Cream);

            var menu = DesignUI.Raw("Menu", buttons.transform, DesignUI.Kit("panel_scrim_card"), Color.white);
            menu.type = Image.Type.Sliced;
            DesignUI.BotRect(menu.rectTransform, 207f, DesignUI.DH - 50f - 56f, 378f, DesignUI.DH - 50f);
            DesignUI.Label("Label", menu.transform, "MENU", DesignUI.F(19), FlatUI.Cream);
            var mb = menu.gameObject.AddComponent<Button>();
            mb.targetGraphic = menu; mb.transition = Selectable.Transition.None;
            mb.onClick.AddListener(Menu);
        }

        private void BuildStats(float yTop)
        {
            var box = DesignUI.Raw("StatsBox", _root.transform, DesignUI.Kit("panel_stat_frame"), Color.white);
            box.type = Image.Type.Sliced; box.pixelsPerUnitMultiplier = 66f / (20f * DesignUI.U);
            box.raycastTarget = false;
            DesignUI.TopRect(box.rectTransform, 24f, yTop, 378f, yTop + 156f);

            _statValues = new Text[StatLabels.Length];
            for (int i = 0; i < StatLabels.Length; i++)
            {
                float y0 = 16f + i * 31f;
                var row = new GameObject("Row" + i, typeof(RectTransform));
                row.transform.SetParent(box.transform, false);
                DesignUI.ColRect((RectTransform)row.transform, y0, y0 + 26f, 20f);

                var lbl = DesignUI.Label("Label", row.transform, StatLabels[i], DesignUI.F(15),
                    new Color(1f, 0.976f, 0.925f, 0.65f), TextAnchor.MiddleLeft, Fonts.Body);
                lbl.fontStyle = FontStyle.Bold;
                DesignUI.Frac(lbl.rectTransform, 0f, 0f, 0.65f, 1f);

                _statValues[i] = DesignUI.Label("Value", row.transform, "", DesignUI.F(15), FlatUI.Cream, TextAnchor.MiddleRight, Fonts.Body);
                _statValues[i].fontStyle = FontStyle.Bold;
                DesignUI.Frac(_statValues[i].rectTransform, 0.5f, 0f, 1f, 1f);
            }
        }
    }
}
