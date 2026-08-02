using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Victory (spec `14-victory.md`): the same information architecture as Game Over at the opposite
    /// emotional temperature, and the CTA points forward. Note the deliberate hierarchy inversion —
    /// yellow NEXT is the primary here and RETRY is secondary, so retry is never red on this screen.
    ///
    /// The 2x rewarded ad is deliberately absent: victory is rare and already generous, and a prompt
    /// here reads as greedy.
    /// </summary>
    public class WinPanel : MonoBehaviour
    {
        private const string StarsKeyPrefix = "stage_stars_";

        private GameObject _root;
        private Text[] _statValues;
        private Text _payout;
        private Image[] _stars;

        private static readonly string[] StatLabels = { "Survived", "Level reached", "Customers fed", "Best combo" };

        private void Start()
        {
            Build();
            _root.SetActive(false);
            WaveManager.OnAllWavesCleared += Show;
            if (DesignUI.AutoOpen("victory")) Show();
        }

        private void OnDestroy() => WaveManager.OnAllWavesCleared -= Show;

        private void Show()
        {
            Time.timeScale = 0f;
            UiSfx.Play(UiSfx.VictorySting);
            var gs = GameStats.Instance;
            int kills = gs != null ? gs.Kills : 0;
            float rewardMult = StageConfig.Current.rewardMult;
            var pr = FindAnyObjectByType<PlayerRoot>();
            float coinMult = pr != null ? pr.Stats.CoinMult : 1f;
            int coins = Mathf.RoundToInt((gs != null ? gs.Coins : 0) * rewardMult * coinMult);
            MetaProgress.AddCoins(coins);
            PlayerPrefs.SetInt("total_kills", PlayerPrefs.GetInt("total_kills", 0) + kills);

            float t = gs != null ? gs.Elapsed : 0f;
            int m = (int)(t / 60f), s = (int)(t % 60f);
            int gems = Mathf.RoundToInt((1 + m) * rewardMult) + 10;   // +10 win bonus
            PlayerGems.Add(gems);

            float best = PlayerPrefs.GetFloat("meta_best_time", 0f);
            if (t > best) { PlayerPrefs.SetFloat("meta_best_time", t); PlayerPrefs.Save(); }
            PlayerProfile.AddXp(kills * 2 + m * 4 + 50);   // +50 win bonus

            var xp = FindAnyObjectByType<PlayerExperience>();
            var combo = FindAnyObjectByType<ComboHud>();
            _statValues[0].text = m.ToString("00") + ":" + s.ToString("00");
            _statValues[1].text = (xp != null ? xp.Level : 1).ToString();
            _statValues[2].text = DesignUI.Num(kills);
            _statValues[3].text = "x" + (combo != null ? combo.BestCombo : 0);
            _payout.text = "+" + DesignUI.Num(coins);

            ShowStars(Rate(pr));

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        /// <summary>1 star for clearing, 2 if HP is still above 30%, 3 if no revive was used. The kill
        /// half of the spec's third criterion needs a per-stage target the stage data does not carry
        /// yet, so it is left out rather than faked.</summary>
        private int Rate(PlayerRoot pr)
        {
            int stars = 1;
            var hp = pr != null ? pr.GetComponent<PlayerHealth>() : FindAnyObjectByType<PlayerHealth>();
            if (hp != null && hp.Current > hp.Max * 0.3f) stars++;
            var revive = FindAnyObjectByType<ReviveManager>();
            if (stars == 2 && (revive == null || !revive.ReviveUsed)) stars++;

            // Stored stars are a max and must never regress.
            string key = StarsKeyPrefix + StageConfig.Current.name;
            PlayerPrefs.SetInt(key, Mathf.Max(PlayerPrefs.GetInt(key, 0), stars));
            PlayerPrefs.Save();
            return stars;
        }

        private void ShowStars(int earned)
        {
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].color = i < earned ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }

        private void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Menu() => Retry();   // v1: the reloaded scene lands on the lobby

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("WinPanel", canvas.transform, 30000);
            _root.GetComponent<Image>().color = new Color(0.059f, 0.357f, 0.294f);   // #0F5B4B
            var bg = DesignUI.Raw("Bg", _root.transform, DesignUI.Kit("bg_victory"), Color.white);
            bg.raycastTarget = false;

            var title = DesignUI.Label("Title", _root.transform, "STAGE CLEARED!", DesignUI.F(38), FlatUI.Yellow);
            DesignUI.TopRect(title.rectTransform, 24f, 64f, 378f, 112f);

            _stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (DesignUI.DW - (3f * 44f + 2f * 8f)) * 0.5f + i * 52f;
                _stars[i] = DesignUI.Raw("Star" + i, _root.transform,
                    DesignUI.StarIcon != null ? DesignUI.StarIcon : DesignUI.Kit("pip_full"), Color.white);
                _stars[i].preserveAspect = true; _stars[i].raycastTarget = false;
                DesignUI.TopRect(_stars[i].rectTransform, x, 124f, x + 44f, 168f);
            }

            BuildStats(184f);

            _payout = DesignUI.Label("Payout", _root.transform, "+0", DesignUI.F(28), FlatUI.Yellow);
            DesignUI.TopRect(_payout.rectTransform, 24f, 356f, 378f, 398f);

            // Three equal buttons: NEXT is the primary, retry and menu are glass.
            float w = (354f - 2f * 10f) / 3f;
            Secondary("Retry", 24f, w, "RETRY", Retry);

            var next = Chunky.Button("Next", _root.transform, "yellow", Vector2.zero, Vector2.one,
                () => Toast.Show("Next stage comes in v2!"), out var nextFace, 5f * DesignUI.U, 60f / (16f * DesignUI.U));
            DesignUI.BotRect((RectTransform)next.transform, 24f + w + 10f, DesignUI.DH - 50f - 56f, 24f + 2f * w + 10f, DesignUI.DH - 50f);
            DesignUI.Label("Label", nextFace, "NEXT ▶", DesignUI.F(17), FlatUI.Ink);

            Secondary("Menu", 24f + 2f * (w + 10f), w, "MENU", Menu);
        }

        private void Secondary(string name, float x0, float w, string label, UnityEngine.Events.UnityAction onClick)
        {
            var img = DesignUI.Raw(name, _root.transform, DesignUI.Kit("panel_scrim_card"), Color.white);
            img.type = Image.Type.Sliced;
            DesignUI.BotRect(img.rectTransform, x0, DesignUI.DH - 50f - 56f, x0 + w, DesignUI.DH - 50f);
            DesignUI.Label("Label", img.transform, label, DesignUI.F(17), FlatUI.Cream);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
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
