using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Pre-Run Loadout (spec `04-loadout.md`): two choices, five seconds — starting tool and stage.
    /// It is the last gate before gameplay, so it must never feel like a form: the slot border and
    /// fill carry the selection, and a locked tap explains itself instead of doing nothing.
    ///
    /// Both picks persist the moment they change, not on START, so an app kill keeps them.
    /// </summary>
    public class LoadoutPanel : MonoBehaviour
    {
        /// <summary>The design's second stage, kept as the grid's fourth card so the row pairs up and the
        /// screen says out loud that more kitchens are coming (`04-loadout.md` §Content).</summary>
        private const string TeaserName = "Café Crunch";

        /// <summary>The kit's lock art is a cool grey; the design's locks are amber, so they read as
        /// "later" rather than "broken". Tint, not a second sprite.</summary>
        private static readonly Color LockAmber = new Color(1f, 0.78f, 0.32f);

        private GameObject _root;
        private Image[] _toolSlots, _stageSlots, _stageIcons;
        private CanvasGroup[] _toolGroups, _stageGroups;
        private Text[] _stageSubs;
        private Text _selection;

        private void Start()
        {
            Build();
            _root.SetActive(false);
            if (DesignUI.AutoOpen("loadout")) Open();
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Close() => _root.SetActive(false);

        private void PickTool(int i)
        {
            if (PlayerLoadout.Tools[i].locked) { UiSfx.PlayDenied(); Toast.Show("Unlock by playing more runs!"); return; }
            PlayerLoadout.SelectedTool = i;
            Refresh();
        }

        private void PickStage(int i)
        {
            if (i >= StageConfig.Tiers.Length)
            {
                UiSfx.PlayDenied();
                Toast.Show(TeaserName + " unlocks in v2!");
                return;
            }
            if (!StageConfig.Unlocked(i))
            {
                int mins = Mathf.CeilToInt(StageConfig.Tiers[i].unlockTime / 60f);
                UiSfx.PlayDenied();
                Toast.Show("Survive " + mins + " min to unlock this kitchen!");
                return;
            }
            StageConfig.Selected = i;
            Refresh();
        }

        /// <summary>The run really begins here, so this is where the energy is spent.</summary>
        private void StartRun()
        {
            if (!PlayerEnergy.Spend(LobbyScreen.EnergyPerRun)) { UiSfx.PlayDenied(); Toast.Show("Not enough energy!"); return; }
            Time.timeScale = 1f;
            LobbyScreen.BeginRun();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Refresh()
        {
            for (int i = 0; i < _toolSlots.Length; i++)
            {
                bool locked = PlayerLoadout.Tools[i].locked;
                bool on = PlayerLoadout.SelectedTool == i && !locked;
                _toolSlots[i].sprite = DesignUI.Kit(on ? "panel_slot_selected" : "panel_slot_empty");
                // Locked slots keep the normal border so the grid stays visually even (spec).
                _toolGroups[i].alpha = locked ? 0.55f : 1f;
            }
            _selection.text = "Selected: “" + PlayerLoadout.CurrentTool.name + "”";

            for (int i = 0; i < _stageSlots.Length; i++)
            {
                bool teaser = i >= StageConfig.Tiers.Length;
                bool unlocked = !teaser && StageConfig.Unlocked(i);
                bool on = !teaser && StageConfig.Selected == i && unlocked;
                _stageSlots[i].sprite = DesignUI.Kit(on ? "panel_slot_selected" : "panel_slot_empty");
                _stageGroups[i].alpha = unlocked ? 1f : 0.55f;
                _stageSubs[i].text = teaser ? "Coming soon"
                    : !unlocked ? "Locked" : on ? "✓ selected" : "tap to select";
                // A kitchen you cannot enter shows the lock instead of the diner (spec §Content).
                _stageIcons[i].sprite = unlocked ? DesignUI.Kit("stage_diner")
                    : DesignUI.LockIcon != null ? DesignUI.LockIcon : DesignUI.Kit("stage_diner");
                _stageIcons[i].color = unlocked ? Color.white : LockAmber;
            }
        }

        // ---------- build ----------

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            DesignUI.RecalcIfStale();

            _root = DesignUI.Page("LoadoutPanel", canvas.transform);
            DesignUI.Header(_root.transform, "GET READY", Close);

            DesignUI.Section(_root.transform, "STARTING TOOL", 18f, 122f);

            int tools = PlayerLoadout.Tools.Length;
            float tw = (366f - 3f * 10f) / 4f;
            _toolSlots = new Image[tools];
            _toolGroups = new CanvasGroup[tools];
            for (int i = 0; i < tools; i++)
                BuildToolSlot(i, 18f + i * (tw + 10f), 152f, tw);

            _selection = DesignUI.Label("Selection", _root.transform, "", DesignUI.F(14), FlatUI.Ink,
                TextAnchor.MiddleCenter, Fonts.Body);
            _selection.fontStyle = FontStyle.Bold;
            DesignUI.TopRect(_selection.rectTransform, 18f, 152f + tw + 10f, 384f, 152f + tw + 34f);

            float stageTop = 152f + tw + 48f;
            DesignUI.Section(_root.transform, "STAGE", 18f, stageTop);

            // The design's stage grid: cards, two per row, not full-width rows. Our three difficulty
            // tiers fill the first three cells and the design's own "Café Crunch — Coming soon" teaser
            // takes the fourth, so the grid is square and nothing is invented to fill it.
            int cards = StageConfig.Tiers.Length + 1;
            const float cardH = 110f, gap = 10f;
            float cardW = (366f - gap) / 2f;
            _stageSlots = new Image[cards];
            _stageGroups = new CanvasGroup[cards];
            _stageSubs = new Text[cards];
            _stageIcons = new Image[cards];
            for (int i = 0; i < cards; i++)
                BuildStageSlot(i, 18f + (i % 2) * (cardW + gap), stageTop + 30f + (i / 2) * (cardH + gap),
                    cardW, cardH);

            var start = Chunky.Button("Start", _root.transform, "red", Vector2.zero, Vector2.one, StartRun, out var face,
                6f * DesignUI.U, 60f / (20f * DesignUI.U));
            DesignUI.BotRect((RectTransform)start.transform, 18f, DesignUI.DH - 46f - 60f, 384f, DesignUI.DH - 46f);
            DesignUI.Label("Label", face, "▶ START", DesignUI.F(26), FlatUI.Cream);
        }

        private void BuildToolSlot(int i, float x0, float yTop, float size)
        {
            var slot = DesignUI.Raw("Tool" + i, _root.transform, DesignUI.Kit("panel_slot_empty"), Color.white);
            slot.type = Image.Type.Sliced; slot.pixelsPerUnitMultiplier = 54f / (16f * DesignUI.U);
            DesignUI.TopRect(slot.rectTransform, x0, yTop, x0 + size, yTop + size);
            _toolSlots[i] = slot;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(slot.transform, false);
            DesignUI.Stretch((RectTransform)content.transform, Vector2.zero, Vector2.one);
            _toolGroups[i] = content.AddComponent<CanvasGroup>();

            var tool = PlayerLoadout.Tools[i];
            // Locked tools show the lock (spec §Content); an unlocked one shows its own tool art, and
            // falls back to the ink pip only while that art is still missing.
            var toolArt = tool.locked || string.IsNullOrEmpty(tool.icon) ? null : DesignUI.Kit(tool.icon);
            var icon = DesignUI.Raw("Icon", content.transform,
                tool.locked && DesignUI.LockIcon != null ? DesignUI.LockIcon
                    : toolArt != null ? toolArt : DesignUI.Kit("pip_full"),
                tool.locked ? LockAmber : toolArt != null ? Color.white : FlatUI.Ink);
            icon.preserveAspect = true; icon.raycastTarget = false;
            DesignUI.Frac(icon.rectTransform, 0.30f, 0.44f, 0.70f, 0.84f);

            // A locked slot names its state, not the tool — the design does not spoil what is behind it.
            var sub = DesignUI.Label("Sub", content.transform, tool.locked ? tool.sub : tool.name,
                DesignUI.F(10), DesignUI.Brown,
                TextAnchor.MiddleCenter, Fonts.Body);
            sub.fontStyle = FontStyle.Bold;
            DesignUI.Frac(sub.rectTransform, 0.03f, 0.12f, 0.97f, 0.40f);

            int idx = i;
            var b = slot.gameObject.AddComponent<Button>();
            b.targetGraphic = slot; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => PickTool(idx));
        }

        /// <summary>One stage card: 34px icon on top, name, sub-label — all centred, like the design's
        /// grid. The last card is the teaser, which has no tier behind it.</summary>
        private void BuildStageSlot(int i, float x0, float yTop, float w, float h)
        {
            var slot = DesignUI.Raw("Stage" + i, _root.transform, DesignUI.Kit("panel_slot_empty"), Color.white);
            slot.type = Image.Type.Sliced; slot.pixelsPerUnitMultiplier = 54f / (16f * DesignUI.U);
            DesignUI.TopRect(slot.rectTransform, x0, yTop, x0 + w, yTop + h);
            _stageSlots[i] = slot;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(slot.transform, false);
            DesignUI.Stretch((RectTransform)content.transform, Vector2.zero, Vector2.one);
            _stageGroups[i] = content.AddComponent<CanvasGroup>();

            bool teaser = i >= StageConfig.Tiers.Length;

            // 34px icon, centred: the diner glyph for a kitchen you can enter, the lock for one you
            // cannot (spec §Content). Refresh swaps it, since a tier unlocks mid-session.
            var icon = DesignUI.Raw("Icon", content.transform, DesignUI.Kit("stage_diner"), Color.white);
            icon.preserveAspect = true; icon.raycastTarget = false;
            float half = 17f / w;
            DesignUI.Frac(icon.rectTransform, 0.5f - half, (h - 48f) / h, 0.5f + half, (h - 14f) / h);
            _stageIcons[i] = icon;

            string title = teaser ? TeaserName : StageConfig.Tiers[i].name;
            var name = DesignUI.Label("Name", content.transform, title, DesignUI.F(13), FlatUI.Ink,
                TextAnchor.MiddleCenter, Fonts.Body);
            name.fontStyle = FontStyle.Bold;
            DesignUI.Frac(name.rectTransform, 0.04f, 0.26f, 0.96f, 0.55f);

            _stageSubs[i] = DesignUI.Label("Sub", content.transform, "", DesignUI.F(11), DesignUI.Brown,
                TextAnchor.MiddleCenter, Fonts.Body);
            _stageSubs[i].fontStyle = FontStyle.Bold;
            DesignUI.Frac(_stageSubs[i].rectTransform, 0.04f, 0.07f, 0.96f, 0.26f);

            int idx = i;
            var b = slot.gameObject.AddComponent<Button>();
            b.targetGraphic = slot; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => PickStage(idx));
        }
    }
}
