using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// The chef mascot, cut from the assembled sheet the game already ships
    /// (`Resources/art/chef2_cut.png` — the same art the in-game cook is built from, so every screen
    /// shows the character the player actually controls rather than a stand-in).
    ///
    /// The rects are the sheet's opaque islands, measured from the texture rather than eyeballed.
    /// The sheet lives under Resources so any screen can reach it: the splash builds before anything
    /// else exists, so it cannot borrow a reference from a scene object.
    /// </summary>
    public static class ChefArt
    {
        private const string SheetPath = "art/chef2_cut";
        private const float Ppu = 100f;

        private static Texture2D _sheet;
        private static bool _looked;
        private static Sprite _figure, _hat, _head, _mustache;

        private static Texture2D Sheet
        {
            get
            {
                if (_looked) return _sheet;
                _looked = true;
                return _sheet = Resources.Load<Texture2D>(SheetPath);
            }
        }

        /// <summary>
        /// The sheet draws the cook squat — wider than he should read. The in-game character has
        /// always corrected this (`ChefVisual` scales its body by exactly this), but the UI drew the
        /// raw sprite with `preserveAspect`, so the same chef looked flattened on the menus and
        /// normal in play. Apply this as a local scale wherever the figure is shown.
        /// </summary>
        public static readonly Vector3 Unflatten = new Vector3(0.92f, 1.12f, 1f);

        /// <summary>Applies <see cref="Unflatten"/> to an Image showing the figure.</summary>
        public static void Straighten(Component image)
        {
            if (image != null) image.transform.localScale = Unflatten;
        }

        private static Sprite Cut(ref Sprite cache, Rect r)
        {
            if (cache != null) return cache;
            Texture2D t = Sheet;
            return t == null ? null : cache = Sprite.Create(t, r, new Vector2(0.5f, 0.5f), Ppu);
        }

        /// <summary>The whole cook, hat to boots — the mascot.</summary>
        public static Sprite Figure => Cut(ref _figure, new Rect(66, 363, 507, 484));

        /// <summary>Just the toque.</summary>
        public static Sprite Hat => Cut(ref _hat, new Rect(1111, 598, 469, 242));

        /// <summary>Goggled face, no hat or moustache.</summary>
        public static Sprite Head => Cut(ref _head, new Rect(714, 612, 329, 203));

        /// <summary>The moustache on its own.</summary>
        public static Sprite Mustache => Cut(ref _mustache, new Rect(1603, 602, 352, 115));
    }
}
