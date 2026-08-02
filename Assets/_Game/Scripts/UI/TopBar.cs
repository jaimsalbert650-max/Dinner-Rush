using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Solid dark bar pinned to the very top of the screen, covering the phone's front-camera / notch
    /// & status-bar strip. Built at runtime on its own high-sorting canvas so it stays on top of the
    /// lobby and the in-game HUD. Its height follows the top safe-area inset (the notch/status region),
    /// with a small minimum so it's still visible on notchless screens and in the editor Game view.
    /// </summary>
    public class TopBar : MonoBehaviour
    {
        [SerializeField] private Color barColor = new Color(0.16f, 0.17f, 0.22f, 1f);

        private void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("TopBar", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            // Own canvas so it draws above the HUD (0), lobby (20000), title (25000) and pause (30000).
            var c = go.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 32000;

            var img = go.AddComponent<Image>();
            img.color = barColor;
            img.raycastTarget = false;

            // Full width, pinned to the top; height = the unsafe top inset (notch/status), clamped so
            // it's a visible strip without a notch but never swallows the HUD.
            float sh = Mathf.Max(1f, Screen.height);
            float topInset = sh - Screen.safeArea.yMax;
            float frac = Mathf.Clamp(topInset / sh, 0.045f, 0.09f);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f - frac);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
