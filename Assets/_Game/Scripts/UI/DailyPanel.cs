using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Daily Reward (spec `05-daily.md`): a 7-day return loop whose whole job is to make day 7 feel
    /// close — a 3x2 grid of days plus the jackpot card sitting right under it, then one CLAIM.
    /// Backed by <see cref="DailyService"/>; claiming never leaves the screen.
    /// </summary>
    public class DailyPanel : MonoBehaviour
    {
        private const float CellH = 92f, Gap = 10f;

        private GameObject _root;
        private readonly Image[] _cells = new Image[6];
        private readonly Image[] _cellIcons = new Image[6];
        private readonly Text[] _cellSubs = new Text[6];
        private readonly CanvasGroup[] _cellGroups = new CanvasGroup[6];
        private Image _claimFace, _claimShadow;
        private CanvasGroup _claimGroup;
        private Text _claimLabel;
        private Button _claimBtn;
        private float _nextCountdownRefresh;
        private bool _lastCanClaim;

        private void Start()
        {
            Build();
            _root.SetActive(false);
            if (DesignUI.AutoOpen("daily")) Open();
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        private void Update()
        {
            // The countdown only needs a minute's resolution — never rebuild it per frame (spec).
            if (_root == null || !_root.activeSelf) return;
            if (Time.unscaledTime < _nextCountdownRefresh) return;
            _nextCountdownRefresh = Time.unscaledTime + 60f;

            // Midnight rollover while the screen is open has to move the grid too, not just the CTA
            // (spec §Edge cases) — otherwise the cells keep yesterday's state until you leave and
            // come back, and the two disagree on screen.
            bool can = DailyService.CanClaim;
            if (can != _lastCanClaim) Refresh();
            else RefreshCta();
        }

        private void ClaimPressed()
        {
            if (!DailyService.CanClaim) return;          // also the double-tap guard
            int day = DailyService.Streak;
            int amount = DailyService.Reward(day);
            if (!DailyService.Claim()) return;
            UiSfx.Play(UiSfx.DailyClaim);
            Toast.Show("+" + DesignUI.Num(amount) + " coins · See you tomorrow!");
            Refresh();
        }

        private void Refresh()
        {
            int streak = DailyService.Streak;
            bool can = DailyService.CanClaim;
            _lastCanClaim = can;

            for (int i = 0; i < 6; i++)
            {
                bool claimed = i < streak || (i == streak && !can);
                bool today = i == streak && can;

                _cells[i].sprite = DesignUI.Kit(today ? "panel_slot_selected" : "panel_slot_empty");
                _cellGroups[i].alpha = claimed ? 0.55f : 1f;
                _cellSubs[i].text = claimed ? "Claimed" : DesignUI.Num(DailyService.Reward(i));

                // Claimed = a filled pip standing in for the check icon, today = the coin, later = a
                // lock. Real `ic_check` / `ic_lock` art drops straight into these three slots.
                var coin = DesignUI.CoinIcon != null ? DesignUI.CoinIcon : DesignUI.Kit("pip_full");
                var padlock = DesignUI.LockIcon != null ? DesignUI.LockIcon : DesignUI.Kit("pip_empty");
                _cellIcons[i].sprite = claimed ? DesignUI.Kit("pip_full") : today ? coin : padlock;
                _cellIcons[i].color = claimed ? DesignUI.Brown : Color.white;
            }

            RefreshCta();
        }

        private void RefreshCta()
        {
            bool can = DailyService.CanClaim;
            _claimLabel.text = can
                ? "CLAIM " + DesignUI.Num(DailyService.Reward(DailyService.Streak))
                : "✓ CLAIMED · BACK IN " + DailyService.HoursUntilNext + "H";
            _claimFace.sprite = DesignUI.Kit(can ? "btn_red_face" : "btn_grey_face");
            _claimShadow.sprite = DesignUI.Kit(can ? "btn_red_shadow" : "btn_grey_shadow");
            _claimGroup.alpha = can ? 1f : 0.7f;
            _claimBtn.interactable = can;
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("DailyPanel", canvas.transform);
            DesignUI.Header(_root.transform, "DAILY REWARD", Close);

            var sub = DesignUI.Label("Subtitle", _root.transform, "Come back every day for tastier rewards!",
                DesignUI.F(14), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            sub.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(sub.rectTransform, 18f, 122f, 384f, 148f);

            // 3 x 2 day grid — day 7 lives in the jackpot card below, not the grid.
            float w = (366f - 2f * Gap) / 3f;
            for (int i = 0; i < 6; i++)
            {
                float x = 18f + (i % 3) * (w + Gap);
                float y = 166f + (i / 3) * (CellH + Gap);
                BuildCell(i, x, y, w);
            }

            BuildJackpot(166f + 2f * (CellH + Gap) + 6f);
            BuildClaim();
        }

        private void BuildCell(int i, float x, float yTop, float w)
        {
            var cell = DesignUI.Raw("Day" + (i + 1), _root.transform, DesignUI.Kit("panel_slot_empty"), Color.white);
            cell.type = Image.Type.Sliced; cell.pixelsPerUnitMultiplier = 54f / (16f * DesignUI.U);
            cell.raycastTarget = false;
            DesignUI.TopRect(cell.rectTransform, x, yTop, x + w, yTop + CellH);
            _cells[i] = cell;
            _cellGroups[i] = cell.gameObject.AddComponent<CanvasGroup>();

            var day = DesignUI.Label("Day", cell.transform, "DAY " + (i + 1), DesignUI.F(11), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            day.fontStyle = FontStyle.Bold;
            DesignUI.Frac(day.rectTransform, 0.05f, 0.66f, 0.95f, 0.90f);

            _cellIcons[i] = DesignUI.Raw("Icon", cell.transform, DesignUI.Kit("pip_full"), Color.white);
            _cellIcons[i].preserveAspect = true; _cellIcons[i].raycastTarget = false;
            DesignUI.Frac(_cellIcons[i].rectTransform, 0.5f - 11f / w, 0.36f, 0.5f + 11f / w, 0.64f);

            _cellSubs[i] = DesignUI.Label("Sub", cell.transform, "", DesignUI.F(12), FlatUI.Ink, TextAnchor.MiddleCenter, Fonts.Body);
            _cellSubs[i].fontStyle = FontStyle.Bold;
            DesignUI.Frac(_cellSubs[i].rectTransform, 0.05f, 0.10f, 0.95f, 0.34f);
        }

        private void BuildJackpot(float yTop)
        {
            var btn = Chunky.Button("Jackpot", _root.transform, "yellow", Vector2.zero, Vector2.one, null, out var face,
                5f * DesignUI.U, 60f / (20f * DesignUI.U));
            DesignUI.TopRect((RectTransform)btn.transform, (DesignUI.DW - 200f) * 0.5f, yTop, (DesignUI.DW + 200f) * 0.5f, yTop + 110f);

            var title = DesignUI.Label("Title", face, "DAY 7 · JACKPOT", DesignUI.F(12), new Color(0.541f, 0.353f, 0.063f), TextAnchor.MiddleCenter, Fonts.Body);
            title.fontStyle = FontStyle.Bold;
            DesignUI.Frac(title.rectTransform, 0.05f, 0.72f, 0.95f, 0.94f);

            var gift = DesignUI.Raw("Gift", face, DesignUI.StarIcon != null ? DesignUI.StarIcon : DesignUI.Kit("pip_full"), Color.white);
            gift.preserveAspect = true; gift.raycastTarget = false;
            DesignUI.Frac(gift.rectTransform, 0.42f, 0.36f, 0.58f, 0.70f);

            var amount = DesignUI.Label("Amount", face,
                DesignUI.Num(DailyService.Reward(6)) + " coins + " + DailyService.JackpotGems + " gems",
                DesignUI.F(18), FlatUI.Ink);
            DesignUI.Frac(amount.rectTransform, 0.04f, 0.08f, 0.96f, 0.34f);
        }

        private void BuildClaim()
        {
            _claimBtn = Chunky.Button("Claim", _root.transform, "red", Vector2.zero, Vector2.one, ClaimPressed, out var face,
                6f * DesignUI.U, 60f / (20f * DesignUI.U));
            DesignUI.BotRect((RectTransform)_claimBtn.transform, 18f, DesignUI.DH - 46f - 58f, 384f, DesignUI.DH - 46f);
            _claimGroup = _claimBtn.gameObject.AddComponent<CanvasGroup>();
            _claimFace = face.GetComponent<Image>();
            _claimShadow = _claimBtn.transform.Find("Shadow").GetComponent<Image>();
            _claimLabel = DesignUI.Label("Label", face, "CLAIM", DesignUI.F(20), FlatUI.Cream);
        }
    }
}
