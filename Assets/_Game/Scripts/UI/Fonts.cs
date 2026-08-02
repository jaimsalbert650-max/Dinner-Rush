using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Central font access for the kit UI. The spec calls for Lilita One (display / headings, numbers,
    /// buttons) and Nunito (body / labels). Drop the TTFs into <c>Assets/_Game/Resources/fonts/</c> named
    /// <c>LilitaOne.ttf</c> and <c>Nunito.ttf</c> and they light up everywhere automatically; until then we
    /// fall back to a dynamic OS font so nothing renders blank.
    /// </summary>
    public static class Fonts
    {
        private static Font _display, _body, _fallback;

        /// <summary>Lilita One — headings, big numbers, button labels.</summary>
        public static Font Display => _display != null ? _display : (_display = Load("LilitaOne") ?? Fallback);

        /// <summary>Nunito — body copy, small labels, stat rows.</summary>
        public static Font Body => _body != null ? _body : (_body = Load("Nunito") ?? Fallback);

        private static Font Fallback => _fallback != null ? _fallback
            : (_fallback = Font.CreateDynamicFontFromOSFont(
                new[] { "Arial", "Liberation Sans", "Helvetica", "Verdana" }, 40));

        private static Font Load(string name)
        {
            // Try a couple of common file-name spellings so the drop-in is forgiving.
            var f = Resources.Load<Font>("fonts/" + name);
            if (f == null) f = Resources.Load<Font>("fonts/" + name.ToLowerInvariant());
            return f;
        }
    }
}
