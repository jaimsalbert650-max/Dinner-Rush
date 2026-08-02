using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Skins the player with the fully-assembled chef sprite (the reference figure baked into
    /// chef2_cut.png), as a SINGLE sprite so it always looks exactly like the reference — no
    /// part misalignment or detaching. The old multi-part rig renderers are hidden; CookAnimator
    /// animates this one sprite as a whole (bob / squash / lean / hop / flinch).
    /// </summary>
    public class ChefVisual : MonoBehaviour
    {
        [SerializeField] private Texture2D sheet;     // visitirs_cut.png
        [SerializeField] private float ppu = 340f;
        [SerializeField] private float rigScale = 1f;
        [SerializeField] private Vector2 stretch = new Vector2(0.92f, 1.12f); // taller/narrower so the squat sprite isn't flattened

        // The goggle-chef within the visitirs sheet (bottom row, 2nd).
        private static readonly Rect RWhole = new Rect(513, 132, 313, 294);

        public SpriteRenderer Body { get; private set; }

        private void Awake()
        {
            if (sheet == null) return;

            var rig = transform.Find("CookRig");
            if (rig == null)
            {
                rig = new GameObject("CookRig").transform;
                rig.SetParent(transform, false);
            }
            else
            {
                // hide the old part-based rig renderers.
                foreach (var sr in rig.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.enabled = false;
            }
            rig.localScale = Vector3.one * rigScale;

            var go = new GameObject("ChefWhole");
            go.transform.SetParent(rig, false);
            go.transform.localScale = new Vector3(stretch.x, stretch.y, 1f);   // un-flatten the squat sprite
            Body = go.AddComponent<SpriteRenderer>();
            Body.sprite = Sprite.Create(sheet, RWhole, new Vector2(0.5f, 0.42f), ppu);
            // Above the crowd (enemy bodies sit at 2). Tied orders are resolved by an unspecified
            // order, so with both at 2 a surrounding mob drew over the cook and he vanished inside it —
            // the one thing that must always be readable on screen.
            Body.sortingOrder = 20;
        }
    }
}
