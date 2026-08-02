using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Level-Up overlay (spec `10-level-up.md`): the core loop's decision moment — three cards, one
    /// tap, back to the action in under two seconds. There is no dismiss and no back button; a choice
    /// is mandatory.
    ///
    /// The draw rules and effects come from the scene's `UpgradeDefinition` pool (already real
    /// gameplay data); this class owns the presentation, the pick flow and the per-run reroll.
    /// Everything runs on unscaled time because the game is paused while it is open.
    /// </summary>
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private PlayerExperience xp;
        [SerializeField] private PlayerRoot player;
        [SerializeField] private UpgradeDefinition[] pool;
        [SerializeField] private int choices = 3;
        [SerializeField] private int rerollsPerRun = 1;

        private const float CardH = 80f, Gap = 12f, ResolveSeconds = 0.38f;

        private GameObject _root;
        private Card[] _cards;
        private GameObject _reroll;
        private Text _rerollLabel;
        private int _rerollsLeft;
        private int _pending;
        private bool _open, _locked;
        private float _resolveAt;
        private UpgradeDefinition _chosen;

        private WeaponManager _weapons;
        private PassiveInventory _passives;
        private PlayerHealth _health;

        private class Card
        {
            public GameObject go;
            public Image border, icon, badge;
            public Text title, desc, badgeText;
            public Button button;
            public CanvasGroup group;
            public RectTransform rt;
        }

        private void Start()
        {
            if (xp == null) xp = FindAnyObjectByType<PlayerExperience>();
            if (player == null) player = FindAnyObjectByType<PlayerRoot>();
            _weapons = player != null ? player.GetComponent<WeaponManager>() : FindAnyObjectByType<WeaponManager>();
            _passives = player != null ? player.GetComponent<PassiveInventory>() : FindAnyObjectByType<PassiveInventory>();
            _health = player != null ? player.GetComponent<PlayerHealth>() : null;
            _rerollsLeft = rerollsPerRun;
            Build();
            _root.SetActive(false);
            if (xp != null) xp.OnLevelUp += OnLevelUp;
            if (DesignUI.AutoOpen("levelup")) Open();
        }

        private void OnDestroy()
        {
            if (xp != null) xp.OnLevelUp -= OnLevelUp;
            if (_open) Time.timeScale = 1f;   // safety: never leave the game paused
        }

        private void OnLevelUp(int level)
        {
            // Death wins over a queued level-up — the gameover screen must not be covered.
            if (_health != null && _health.IsDead) return;
            _pending++;
            if (!_open) Open();
        }

        private void Open()
        {
            if (pool == null || pool.Length == 0) { _pending = 0; return; }
            _open = true;
            Time.timeScale = 0f;
            UiSfx.Play(UiSfx.LevelUpFanfare);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            NextChoice();
        }

        private void NextChoice()
        {
            _pending = Mathf.Max(0, _pending - 1);
            _locked = false;
            _chosen = null;
            Populate();
        }

        private void Update()
        {
            // Resolve on unscaled time: the pick highlight holds for 0.38s, then the effect lands.
            if (!_locked || Time.unscaledTime < _resolveAt) return;
            _locked = false;
            Apply(_chosen);
            if (_pending > 0) NextChoice();
            else Close();
        }

        /// <summary>Tap → every card locks at once (the double-tap guard), the chosen one takes the gold
        /// border and the rest fade back.</summary>
        private void Pick(UpgradeDefinition up, int index)
        {
            if (_locked) return;
            _locked = true;
            UiSfx.Play(UiSfx.CardPick);
            _chosen = up;
            _resolveAt = Time.unscaledTime + ResolveSeconds;
            for (int i = 0; i < _cards.Length; i++)
            {
                bool picked = i == index;
                _cards[i].group.alpha = picked ? 1f : 0.4f;
                _cards[i].border.color = picked ? FlatUI.Yellow : FlatUI.CreamBorder;
                _cards[i].rt.localScale = Vector3.one * (picked ? 1.04f : 1f);
            }
        }

        private void Apply(UpgradeDefinition up)
        {
            if (up == null) return;
            if (up.kind == UpgradeKind.Weapon)
            {
                var t = up.WeaponType;
                if (t != null && _weapons != null) _weapons.AddOrLevel(t);
            }
            else if (up.kind == UpgradeKind.Passive)
            {
                if (_passives != null) _passives.AddOrLevel(up.stat, up.amount, up.maxLevel);
            }
            else if (player != null) up.ApplyStat(player.Stats);
        }

        private void Close()
        {
            _open = false;
            _root.SetActive(false);
            Time.timeScale = 1f;
        }

        private void Reroll()
        {
            if (_rerollsLeft <= 0 || _locked) return;
            _rerollsLeft--;
            Populate();
        }

        // ---------- draw ----------

        private void Populate()
        {
            var picks = PickDistinct(_cards.Length);
            for (int i = 0; i < _cards.Length; i++)
            {
                var up = picks[i];
                var c = _cards[i];
                if (up == null) { c.go.SetActive(false); continue; }

                c.go.SetActive(true);
                c.group.alpha = 1f;
                c.border.color = FlatUI.CreamBorder;
                c.rt.localScale = Vector3.one;
                c.title.text = up.displayName;
                c.desc.text = up.description;
                if (up.icon != null) { c.icon.sprite = up.icon; c.icon.color = Color.white; }

                int stack = StackLevel(up);
                bool isNew = stack == 0;
                c.badge.gameObject.SetActive(isNew || stack > 0);
                c.badge.sprite = DesignUI.Kit(isNew ? "chip_red" : "chip_blue");
                c.badgeText.text = isNew ? "NEW" : "LV " + stack;

                int idx = i;
                var captured = up;
                c.button.onClick.RemoveAllListeners();
                c.button.onClick.AddListener(() => Pick(captured, idx));
            }

            _reroll.SetActive(_rerollsLeft > 0);
            _rerollLabel.text = "🔄 Reroll (x" + _rerollsLeft + ")";
        }

        /// <summary>How many times this upgrade is already held — 0 means it is a first-time pick.</summary>
        private int StackLevel(UpgradeDefinition up)
        {
            if (up.kind == UpgradeKind.Passive && _passives != null) return _passives.LevelOf(up.stat);
            if (up.kind == UpgradeKind.Weapon && _weapons != null)
            {
                var w = _weapons.Get(up.WeaponType);
                return w != null ? w.Level : 0;
            }
            return 0;
        }

        private UpgradeDefinition[] PickDistinct(int n)
        {
            // Skip anything already at its cap so a level-up never presents a dead pick; fall back to
            // the whole pool only if literally everything is maxed.
            var bag = new List<UpgradeDefinition>();
            foreach (var u in pool) if (u != null && !IsMaxed(u)) bag.Add(u);
            if (bag.Count == 0)
                foreach (var u in pool) if (u != null) bag.Add(u);

            var result = new UpgradeDefinition[n];
            for (int i = 0; i < n; i++)
            {
                if (bag.Count == 0) { result[i] = null; continue; }
                int idx = Random.Range(0, bag.Count);
                result[i] = bag[idx];
                bag.RemoveAt(idx);   // distinct cards; surplus slots stay null and are hidden
            }
            return result;
        }

        /// <summary>True if this upgrade is already at its cap and picking it would do nothing.</summary>
        private bool IsMaxed(UpgradeDefinition up)
        {
            if (up.kind == UpgradeKind.Passive)
                return _passives != null && _passives.LevelOf(up.stat) >= up.maxLevel;
            if (up.kind == UpgradeKind.Weapon)
            {
                var w = _weapons != null ? _weapons.Get(up.WeaponType) : null;
                return w != null && w.Level >= w.MaxLevel;
            }
            return false;   // stat upgrades are uncapped
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Overlay("LevelUpPanel", canvas.transform, 30000);

            int n = Mathf.Max(1, choices);
            float cardsH = n * CardH + (n - 1) * Gap;
            float total = 70f + Gap + cardsH + Gap + 34f;
            var col = DesignUI.Column(_root.transform, 354f, total);

            var title = DesignUI.Label("Title", col, "LEVEL UP!", DesignUI.F(40), FlatUI.Yellow);
            DesignUI.ColRect(title.rectTransform, 0f, 46f);
            var sub = DesignUI.Label("Sub", col, "Choose an upgrade", DesignUI.F(14),
                new Color(1f, 0.976f, 0.925f, 0.8f), TextAnchor.MiddleCenter, Fonts.Body);
            sub.fontStyle = FontStyle.Bold;
            DesignUI.ColRect(sub.rectTransform, 46f, 70f);

            _cards = new Card[n];
            for (int i = 0; i < n; i++)
                _cards[i] = BuildCard(col, 70f + Gap + i * (CardH + Gap));

            BuildReroll(col, total - 34f);
        }

        private Card BuildCard(Transform col, float yTop)
        {
            var holder = new GameObject("Card", typeof(RectTransform));
            holder.transform.SetParent(col, false);
            var rt = DesignUI.ColRect((RectTransform)holder.transform, yTop, yTop + CardH);
            var group = holder.AddComponent<CanvasGroup>();

            // Border + face: the 3px border is the card's selection state, so it is its own image.
            var border = DesignUI.Raw("Border", holder.transform, DesignUI.Kit("panel_cream_round_lg"), FlatUI.CreamBorder);
            border.type = Image.Type.Sliced; border.pixelsPerUnitMultiplier = 84f / (18f * DesignUI.U);
            var face = DesignUI.Raw("Face", holder.transform, DesignUI.Kit("panel_cream_round_lg"), FlatUI.Cream);
            face.type = Image.Type.Sliced; face.pixelsPerUnitMultiplier = 84f / (18f * DesignUI.U);
            DesignUI.Stretch(face.rectTransform, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(3f * DesignUI.U, 3f * DesignUI.U);
            face.rectTransform.offsetMax = new Vector2(-3f * DesignUI.U, -3f * DesignUI.U);
            face.raycastTarget = false;

            var icon = DesignUI.Raw("IconTile", holder.transform, DesignUI.Kit("panel_placeholder_dashed"), Color.white);
            icon.type = Image.Type.Sliced; icon.pixelsPerUnitMultiplier = 54f / (14f * DesignUI.U);
            icon.raycastTarget = false; icon.preserveAspect = false;
            DesignUI.Frac(icon.rectTransform, 16f / 354f, 0.16f, 68f / 354f, 0.84f);

            var title = DesignUI.Label("Name", holder.transform, "", DesignUI.F(18), FlatUI.Ink, TextAnchor.MiddleLeft);
            DesignUI.Frac(title.rectTransform, 82f / 354f, 0.50f, 0.72f, 0.86f);

            var badge = DesignUI.Raw("Badge", holder.transform, DesignUI.Kit("chip_red"), Color.white);
            badge.type = Image.Type.Sliced; badge.raycastTarget = false;
            DesignUI.Frac(badge.rectTransform, 0.74f, 0.52f, 0.95f, 0.84f);
            var badgeText = DesignUI.Label("Label", badge.transform, "NEW", DesignUI.F(11), FlatUI.Cream, TextAnchor.MiddleCenter, Fonts.Body);
            badgeText.fontStyle = FontStyle.Bold;

            var desc = DesignUI.Label("Desc", holder.transform, "", DesignUI.F(13), DesignUI.Brown, TextAnchor.MiddleLeft, Fonts.Body);
            desc.fontStyle = FontStyle.Bold;
            DesignUI.Frac(desc.rectTransform, 82f / 354f, 0.14f, 0.95f, 0.48f);

            // The whole card is the hit target, not just the title (spec §Acceptance).
            var button = holder.AddComponent<Button>();
            button.targetGraphic = border; button.transition = Selectable.Transition.None;

            return new Card
            {
                go = holder, border = border, icon = icon, badge = badge,
                title = title, desc = desc, badgeText = badgeText,
                button = button, group = group, rt = rt,
            };
        }

        private void BuildReroll(Transform col, float yTop)
        {
            var pill = DesignUI.Raw("Reroll", col, DesignUI.Kit("panel_scrim_card"), Color.white);
            pill.type = Image.Type.Sliced;
            var rt = DesignUI.ColRect(pill.rectTransform, yTop, yTop + 34f);
            // Self-centred pill, not full width.
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(150f * DesignUI.U, 34f * DesignUI.U);
            rt.anchoredPosition = new Vector2(0f, -yTop * DesignUI.U);

            _rerollLabel = DesignUI.Label("Label", pill.transform, "", DesignUI.F(13), FlatUI.Cream, TextAnchor.MiddleCenter, Fonts.Body);
            _rerollLabel.fontStyle = FontStyle.Bold;

            var b = pill.gameObject.AddComponent<Button>();
            b.targetGraphic = pill; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Reroll);
            _reroll = pill.gameObject;
        }
    }
}
