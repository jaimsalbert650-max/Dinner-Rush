using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Builds the kit's chunky two-part button (a dark shadow sprite offset down + a bevelled face
    /// sprite on top) with a press animation: on press the face + label drop and the shadow collapses,
    /// on release they spring back. Colour picks the kit sprite pair (red/teal/blue/yellow/cream/grey).
    /// </summary>
    public static class Chunky
    {
        /// <summary>Build a chunky button under `parent` filling the given anchors. `face` is the child
        /// the caller parents its label/icon to. <paramref name="shadowPx"/> is the bevel depth in canvas
        /// px — the design specifies it per button role (PLAY 6 proto-px, nav row 4, circles 3).</summary>
        public static Button Button(string name, Transform parent, string color, Vector2 aMin, Vector2 aMax,
            UnityAction onClick, out Transform face, float shadowPx = 8f, float ppuMultiplier = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var shadow = Piece("Shadow", go.transform, "kit/btn_" + color + "_shadow", ppuMultiplier);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -shadowPx);
            var faceImg = Piece("Face", go.transform, "kit/btn_" + color + "_face", ppuMultiplier);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = faceImg;
            btn.transition = Selectable.Transition.None;   // the press component handles the feel
            if (onClick != null) btn.onClick.AddListener(onClick);

            go.AddComponent<ChunkyPress>().Init(faceImg.rectTransform, shadow.rectTransform, shadowPx);

            face = faceImg.transform;
            return btn;
        }

        private static Image Piece(string name, Transform parent, string res, float ppuMultiplier = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var s = Resources.Load<Sprite>(res);
            img.sprite = s != null ? s : UIBuilder.White;
            img.color = Color.white;
            img.type = Image.Type.Sliced;
            // The kit sprites carry a 60px corner; the design specifies a per-role radius, so scale the
            // 9-slice border down instead of authoring another sprite (>1 = tighter corners).
            img.pixelsPerUnitMultiplier = ppuMultiplier;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }
    }

    /// <summary>Press feel for a chunky button: face drops + shadow collapses on press, springs on release.</summary>
    public class ChunkyPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _face, _shadow;
        private float _drop = 8f;

        public void Init(RectTransform face, RectTransform shadow, float drop = 8f)
        {
            _face = face; _shadow = shadow; _drop = drop;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_face != null) _face.anchoredPosition = new Vector2(0f, -_drop);
            if (_shadow != null) _shadow.anchoredPosition = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (_face != null) _face.anchoredPosition = Vector2.zero;
            if (_shadow != null) _shadow.anchoredPosition = new Vector2(0f, -_drop);
        }
    }
}
