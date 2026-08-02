using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Skins the on-screen joystick with the art in Resources: the base ring on this object's own
    /// Image, and the thumb knob on its (single) child Image. Runs at Start so it needs no scene
    /// sprite wiring. Falls back gracefully if the sprites or images are missing.
    /// </summary>
    public class JoystickSkin : MonoBehaviour
    {
        private void Start()
        {
            var baseSprite = Resources.Load<Sprite>("kit/joystick_base");
            var knobSprite = Resources.Load<Sprite>("kit/joystick_knob");

            if (TryGetComponent<Image>(out var baseImg) && baseSprite != null)
            {
                baseImg.sprite = baseSprite;
                baseImg.color = Color.white;
                baseImg.preserveAspect = false;
            }

            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject == gameObject) continue;   // skip the base
                if (knobSprite != null)
                {
                    img.sprite = knobSprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                break;   // only the first child image (the knob)
            }
        }
    }
}
