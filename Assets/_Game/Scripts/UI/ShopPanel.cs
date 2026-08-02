using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Shop (spec `06-shop.md`): starter bundle first, gems second, coins third — real money above the
    /// fold, soft currency below. Cream page, scrolling content, opened by SHOP on the Main Menu and by
    /// the gem pill's `+`.
    ///
    /// The gem packs and the bundle are IAP in the shipping game; there is no store integration yet, so
    /// they run through <see cref="FakeStore"/> — an editor-only stand-in that grants immediately, the
    /// same thing the clickable prototype does. Swap it for `IStoreService` (Unity IAP) before ship;
    /// the prices in <see cref="GemPacks"/> are fallback copy, the store is the source of truth.
    /// The coins-for-gems exchange below is real and works offline.
    ///
    /// The permanent-upgrade rows this class used to host moved to <see cref="UpgradesPanel"/>, which
    /// is what the design calls UPGRADES (spec 07).
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        private const string AdsRemovedKey = "opt_ads_removed";
        private const int BundleGems = 500;

        // Fallback copy only — real prices come from the store. icon size scales with the pack (spec).
        private static readonly int[] GemAmounts = { 80, 500, 1200, 2500 };
        private static readonly string[] GemPrices = { "$0.99", "$4.99", "$9.99", "$19.99" };
        private static readonly float[] GemIconPx = { 18f, 22f, 26f, 30f };

        private static readonly int[] CoinAmounts = { 1000, 5500, 15000 };
        private static readonly int[] CoinCosts = { 20, 90, 220 };

        private GameObject _root, _bundle, _adsRow;
        private Text _coins, _gems;
        private readonly Image[] _coinCards = new Image[3];
        private readonly Text[] _coinLabels = new Text[3];
        private readonly Image[] _coinChips = new Image[3];

        public static bool AdsRemoved => PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;

        private void Start()
        {
            Build();
            _root.SetActive(false);
            if (DesignUI.AutoOpen("shop")) Open();
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        // ---------- purchases ----------

        private void BuyBundle()
        {
            if (AdsRemoved) return;
            FakeStore.Purchase("cs.bundle.starter", ok =>
            {
                if (!ok) { Toast.Show("Purchase didn't go through."); return; }
                PlayerPrefs.SetInt(AdsRemovedKey, 1);
                PlayerPrefs.Save();
                PlayerGems.Add(BundleGems);
                UiSfx.Play(UiSfx.Purchase);
                Toast.Show("Bundle purchased! +" + BundleGems + " gems");
                Refresh();
            });
        }

        private void BuyGems(int i)
        {
            FakeStore.Purchase("cs.gems." + GemAmounts[i], ok =>
            {
                if (!ok) { Toast.Show("Purchase didn't go through."); return; }
                PlayerGems.Add(GemAmounts[i]);
                UiSfx.Play(UiSfx.Purchase);
                Toast.Show("Purchase successful! +" + DesignUI.Num(GemAmounts[i]) + " gems");
                Refresh();
            });
        }

        private void BuyCoins(int i)
        {
            if (!PlayerGems.Spend(CoinCosts[i])) { UiSfx.PlayDenied(); Toast.Show("Not enough gems!"); return; }
            MetaProgress.AddCoins(CoinAmounts[i]);
            UiSfx.Play(UiSfx.Purchase);
            Toast.Show("+" + DesignUI.Num(CoinAmounts[i]) + " coins");
            Refresh();
        }

        private void Refresh()
        {
            if (_coins != null) _coins.text = DesignUI.Num(MetaProgress.Coins);
            if (_gems != null) _gems.text = DesignUI.Num(PlayerGems.Gems);
            if (_bundle != null) _bundle.SetActive(!AdsRemoved);
            if (_adsRow != null) _adsRow.SetActive(AdsRemoved);

            // Unaffordable coin packs dim but stay tappable — the tap explains why (spec: never dead).
            for (int i = 0; i < CoinAmounts.Length; i++)
            {
                float a = PlayerGems.Gems >= CoinCosts[i] ? 1f : 0.55f;
                if (_coinCards[i] != null) SetAlpha(_coinCards[i].transform.parent, a);
            }
        }

        private static void SetAlpha(Transform root, float a)
        {
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = a;
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("ShopPanel", canvas.transform);
            DesignUI.Header(_root.transform, "SHOP", Close);

            // Currency row, centred (spec 06 §2). Two 94px pills with an 8px gap = 196 wide.
            float x = (DesignUI.DW - 196f) * 0.5f;
            _coins = DesignUI.Pill(_root.transform, "CoinPill", x, 118f, 94f, DesignUI.CoinIcon, new Color(1f, 0.78f, 0.2f), "0");
            _gems = DesignUI.Pill(_root.transform, "GemPill", x + 102f, 118f, 94f, DesignUI.GemIcon, FlatUI.Teal, "0");

            DesignUI.Scroll(_root.transform, 166f, out var content);

            BuildBundle(content);
            BuildAdsRemovedRow(content);

            DesignUI.Section(content, "GEMS", 18f, 88f);
            float gemW = (366f - 3f * 8f) / 4f;
            for (int i = 0; i < 4; i++)
                BuildGemCard(content, i, 18f + i * (gemW + 8f), 116f, gemW);

            var coinsLabel = DesignUI.Section(content, "COINS", 18f, 234f);
            var spend = DesignUI.Label("Sub", content, "(spend gems)", DesignUI.F(12), DesignUI.Muted, TextAnchor.MiddleLeft, Fonts.Body);
            spend.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(spend.rectTransform, 78f, 234f, 260f, 256f);

            float coinW = (366f - 2f * 8f) / 3f;
            for (int i = 0; i < 3; i++)
                BuildCoinCard(content, i, 18f + i * (coinW + 8f), 262f, coinW);

            BuildRestore(content, 382f);

            DesignUI.SetContentHeight(content, 480f);
        }

        /// <summary>Starter bundle banner — purple, bevelled, with the rotated BEST VALUE ribbon.</summary>
        private void BuildBundle(Transform content)
        {
            var btn = Chunky.Button("Bundle", content, "purple", Vector2.zero, Vector2.one, BuyBundle, out var face,
                5f * DesignUI.U, 60f / (20f * DesignUI.U));
            DesignUI.TopRect((RectTransform)btn.transform, 18f, 0f, 384f, 76f);
            _bundle = btn.gameObject;

            var star = DesignUI.Raw("Star", face, DesignUI.StarIcon != null ? DesignUI.StarIcon : DesignUI.Kit("pip_full"),
                DesignUI.StarIcon != null ? Color.white : FlatUI.Yellow);
            DesignUI.Frac(star.rectTransform, 0.045f, 0.26f, 0.145f, 0.74f);
            star.preserveAspect = true; star.raycastTarget = false;

            var title = DesignUI.Label("Title", face, "NO ADS + " + BundleGems + " GEMS", DesignUI.F(19), FlatUI.Cream, TextAnchor.MiddleLeft);
            DesignUI.Frac(title.rectTransform, 0.18f, 0.50f, 0.74f, 0.86f);
            var sub = DesignUI.Label("Sub", face, "Starter bundle · one-time offer", DesignUI.F(12), new Color(1f, 0.976f, 0.925f, 0.75f), TextAnchor.MiddleLeft, Fonts.Body);
            sub.fontStyle = FontStyle.Bold;
            DesignUI.Frac(sub.rectTransform, 0.18f, 0.18f, 0.74f, 0.46f);

            var price = DesignUI.Raw("Price", face, DesignUI.Kit("panel_cream_plain"), FlatUI.Cream);
            price.type = Image.Type.Sliced; price.pixelsPerUnitMultiplier = 60f / (10f * DesignUI.U);
            price.raycastTarget = false;
            DesignUI.Frac(price.rectTransform, 0.755f, 0.12f, 0.955f, 0.60f);   // sits under the ribbon
            DesignUI.Label("Label", price.transform, GemPrices[1], DesignUI.F(16), DesignUI.PurpleDeep);

            var ribbon = DesignUI.Raw("Ribbon", face, DesignUI.Kit("ribbon_yellow_strip"), Color.white);
            ribbon.preserveAspect = true;   // it was stretching ~31% wider than the strip is drawn
            // Fully inside the banner's top-right corner: overhanging the edge clipped the label to
            // "…T VALUE", and a ribbon whose own text is unreadable is worse than no ribbon.
            DesignUI.Frac(ribbon.rectTransform, 0.685f, 0.64f, 0.995f, 1.0f);
            ribbon.raycastTarget = false;
            ribbon.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            var rl = DesignUI.Label("Label", ribbon.transform, "BEST VALUE", DesignUI.F(9), FlatUI.Ink, TextAnchor.MiddleCenter, Fonts.Body);
            rl.fontStyle = FontStyle.Bold;
            DesignUI.Frac(rl.rectTransform, 0.02f, 0f, 0.98f, 1f);
        }

        /// <summary>What the banner turns into once the bundle is owned (spec 06 §3a).</summary>
        private void BuildAdsRemovedRow(Transform content)
        {
            var img = DesignUI.Raw("AdsRemoved", content, DesignUI.Kit("panel_cream"), FlatUI.CreamBorder);
            img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 60f / (20f * DesignUI.U);
            img.raycastTarget = false;
            DesignUI.TopRect(img.rectTransform, 18f, 14f, 384f, 62f);
            var t = DesignUI.Label("Label", img.transform, "✓ Ads removed — thanks, chef!", DesignUI.F(14), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            t.fontStyle = FontStyle.Bold;
            _adsRow = img.gameObject;
        }

        private void BuildGemCard(Transform content, int i, float x0, float yTop, float w)
        {
            var face = DesignUI.Card(content, "GemCard" + i);
            DesignUI.TopRect((RectTransform)face.transform.parent, x0, yTop, x0 + w, yTop + 104f);

            float s = GemIconPx[i], half = s / w * 0.5f;
            var ic = DesignUI.Raw("Icon", face.transform, DesignUI.GemIcon != null ? DesignUI.GemIcon : FlatUI.Circle,
                DesignUI.GemIcon != null ? Color.white : FlatUI.Teal);
            DesignUI.Frac(ic.rectTransform, 0.5f - half, 1f - (10f + s) / 104f, 0.5f + half, 1f - 10f / 104f);
            ic.preserveAspect = DesignUI.GemIcon != null; ic.raycastTarget = false;

            var amt = DesignUI.Label("Amount", face.transform, DesignUI.Num(GemAmounts[i]), DesignUI.F(15), FlatUI.Ink);
            DesignUI.Frac(amt.rectTransform, 0.02f, 1f - 70f / 104f, 0.98f, 1f - 48f / 104f);

            var chip = DesignUI.Chip(face.transform, "chip_teal", GemPrices[i], 11, FlatUI.Cream, Fonts.Body);
            DesignUI.Frac(chip.rectTransform, 10f / w, 1f - 96f / 104f, 1f - 10f / w, 1f - 74f / 104f);

            int idx = i;
            var btn = face.gameObject.AddComponent<Button>();
            btn.targetGraphic = face; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => BuyGems(idx));
        }

        private void BuildCoinCard(Transform content, int i, float x0, float yTop, float w)
        {
            var face = DesignUI.Card(content, "CoinCard" + i);
            DesignUI.TopRect((RectTransform)face.transform.parent, x0, yTop, x0 + w, yTop + 104f);
            _coinCards[i] = face;

            float s = 26f, half = s / w * 0.5f;
            var ic = DesignUI.Raw("Icon", face.transform, DesignUI.CoinIcon != null ? DesignUI.CoinIcon : FlatUI.Circle,
                DesignUI.CoinIcon != null ? Color.white : new Color(1f, 0.78f, 0.2f));
            DesignUI.Frac(ic.rectTransform, 0.5f - half, 1f - 38f / 104f, 0.5f + half, 1f - 12f / 104f);
            ic.preserveAspect = DesignUI.CoinIcon != null; ic.raycastTarget = false;

            var amt = DesignUI.Label("Amount", face.transform, DesignUI.Num(CoinAmounts[i]), DesignUI.F(15), FlatUI.Ink);
            DesignUI.Frac(amt.rectTransform, 0.02f, 1f - 70f / 104f, 0.98f, 1f - 48f / 104f);

            // Cost chip: gem icon + the price, so it reads as "spend gems" without a label.
            var chip = DesignUI.Raw("Chip", face.transform, DesignUI.Kit("chip_purple"), Color.white);
            chip.type = Image.Type.Sliced; chip.raycastTarget = false;
            DesignUI.Frac(chip.rectTransform, 18f / w, 1f - 96f / 104f, 1f - 18f / w, 1f - 74f / 104f);
            _coinChips[i] = chip;
            var gic = DesignUI.Raw("Gem", chip.transform, DesignUI.GemIcon != null ? DesignUI.GemIcon : FlatUI.Circle,
                DesignUI.GemIcon != null ? Color.white : FlatUI.Teal);
            DesignUI.Frac(gic.rectTransform, 0.10f, 0.18f, 0.36f, 0.82f);
            gic.preserveAspect = DesignUI.GemIcon != null; gic.raycastTarget = false;
            _coinLabels[i] = DesignUI.Label("Label", chip.transform, CoinCosts[i].ToString(), DesignUI.F(11), FlatUI.Cream, TextAnchor.MiddleLeft, Fonts.Body);
            _coinLabels[i].fontStyle = FontStyle.Bold;
            DesignUI.Frac(_coinLabels[i].rectTransform, 0.40f, 0.05f, 0.94f, 0.95f);

            int idx = i;
            var btn = face.gameObject.AddComponent<Button>();
            btn.targetGraphic = face; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => BuyCoins(idx));
        }

        private void BuildRestore(Transform content, float yTop)
        {
            var img = DesignUI.Raw("Restore", content, DesignUI.Kit("panel_cream"), Color.white);
            img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 60f / (14f * DesignUI.U);
            DesignUI.TopRect(img.rectTransform, 18f, yTop, 384f, yTop + 48f);
            var t = DesignUI.Label("Label", img.transform, "↻ Restore Purchases", DesignUI.F(14), DesignUI.Brown, TextAnchor.MiddleCenter, Fonts.Body);
            t.fontStyle = FontStyle.Bold;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => Toast.Show("Purchases restored ✓"));
        }
    }

    /// <summary>
    /// Stand-in for the store while there is no IAP integration: succeeds immediately, exactly like the
    /// clickable prototype. Replace with `IStoreService` (Unity IAP) — see `06-shop.md` §Store
    /// integration for the real purchase / failure / deferred flows this deliberately does not model.
    /// </summary>
    internal static class FakeStore
    {
        public static void Purchase(string productId, System.Action<bool> done)
        {
            Debug.Log("[FakeStore] granting '" + productId + "' — no IAP integration yet.");
            done(true);
        }
    }
}
