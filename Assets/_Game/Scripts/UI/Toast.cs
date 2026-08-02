using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// The kit's global toast (see `00-README.md`): a dark pill 96 proto-px above the bottom edge,
    /// Nunito 900 cream, ~1.9s, one at a time — a new message replaces the current one, and it never
    /// blocks input. Every "locked / unaffordable" tap in the design fires one of these instead of
    /// doing nothing, so any screen can call <see cref="Show"/> without owning a copy.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private const float DW = 402f, DH = 874f;

        private static Toast _instance;

        private GameObject _pill;
        private Text _label;
        private float _until;

        /// <summary>Show a message. Creates the toast on first use; safe to call from any screen.</summary>
        public static void Show(string message)
        {
            if (_instance == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null) return;
                var go = new GameObject("Toast", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                _instance = go.AddComponent<Toast>();
                _instance.Build(canvas);
            }
            _instance.ShowInternal(message);
        }

        private void Build(Canvas canvas)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Above every panel (lobby 20000, overlays 31000) so a toast is never buried.
            var c = gameObject.AddComponent<Canvas>();
            c.overrideSorting = true; c.sortingOrder = 32000;

            float w = ((RectTransform)canvas.transform).rect.width;
            float u = (w > 1f ? w : 1080f) / DW;

            var img = new GameObject("Pill", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(transform, false);
            img.sprite = Resources.Load<Sprite>("kit/panel_dark_pill");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;                       // never blocks input
            var prt = img.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f); prt.anchorMax = new Vector2(1f, 0f); prt.pivot = new Vector2(0.5f, 0f);
            prt.offsetMin = new Vector2(40f * u, 96f * u);
            prt.offsetMax = new Vector2(-40f * u, (96f + 40f) * u);

            _label = FlatUI.Label("Label", img.transform, "", Mathf.RoundToInt(13f * u), FlatUI.Cream,
                TextAnchor.MiddleCenter, Fonts.Body);
            _label.fontStyle = FontStyle.Bold;

            _pill = img.gameObject;
            _pill.SetActive(false);
        }

        private void ShowInternal(string message)
        {
            if (_label == null) return;
            _label.text = message;
            _pill.SetActive(true);
            transform.SetAsLastSibling();
            _until = Time.unscaledTime + 1.9f;               // unscaled: menus run on timeScale 0
        }

        private void Update()
        {
            if (_pill != null && _pill.activeSelf && Time.unscaledTime >= _until) _pill.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
