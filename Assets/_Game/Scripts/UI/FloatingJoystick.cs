using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace DinnerRush
{
    /// <summary>
    /// The HUD's joystick in the mode `09-hud.md` asks for: a touch anywhere in the bottom 40% of the
    /// screen re-centres the base under the finger, so the player never has to look down to find it.
    /// Lifting the finger returns the base to its resting corner.
    ///
    /// Feeds `&lt;Gamepad&gt;/leftStick` through <see cref="OnScreenControl"/>, which is the path
    /// <see cref="PlayerMovement"/> already binds — so this replaces the stock `OnScreenStick` without
    /// touching movement code. Drag radius and dead zone come straight from the spec.
    /// </summary>
    public class FloatingJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] private string controlPath = "<Gamepad>/leftStick";

        protected override string controlPathInternal
        {
            get => controlPath;
            set => controlPath = value;
        }

        private RectTransform _area;      // the touch region (bottom 40%)
        private RectTransform _base;      // the ring that moves under the finger
        private RectTransform _knob;
        private CanvasGroup _group;
        private Camera _camera;
        private Vector2 _home;            // resting position, from the side setting
        private float _radius = 55f;
        private const float DeadZone = 0.08f;

        public void Init(RectTransform area, RectTransform baseRt, RectTransform knob, float radiusPx)
        {
            _area = area; _base = baseRt; _knob = knob; _radius = radiusPx;
            _group = _base.GetComponent<CanvasGroup>();
            if (_group == null) _group = _base.gameObject.AddComponent<CanvasGroup>();

            // Resolve the camera from the canvas rather than trusting eventData.pressEventCamera —
            // this canvas is Screen Space - Camera, and a null camera maps the touch to nonsense
            // coordinates thousands of units off screen.
            var canvas = area.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                _camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            }

            _home = _base.anchoredPosition;
            Rest();
        }

        /// <summary>Called when the side setting changes — the corner the base returns to moves too.</summary>
        public void SetHome(Vector2 home)
        {
            _home = home;
            if (_knob != null && _knob.anchoredPosition == Vector2.zero) Rest();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_base == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, _camera, out var local))
                return;
            _base.anchoredPosition = local;      // re-centre under the finger
            _group.alpha = 1f;
            Move(Vector2.zero);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_base == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, _camera, out var local))
                return;
            Move(local - _base.anchoredPosition);
        }

        public void OnPointerUp(PointerEventData e)
        {
            Move(Vector2.zero);
            Rest();
        }

        private void Move(Vector2 offset)
        {
            var clamped = Vector2.ClampMagnitude(offset, _radius);
            if (_knob != null) _knob.anchoredPosition = clamped;

            var value = clamped / _radius;
            if (value.magnitude < DeadZone) value = Vector2.zero;
            SendValueToControl(value);
        }

        private void Rest()
        {
            if (_base != null) { _base.anchoredPosition = _home; _group.alpha = 0.75f; }
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }
    }
}
