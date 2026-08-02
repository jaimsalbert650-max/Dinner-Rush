using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Flashes a "WAVE n" banner in the centre-top of the screen for ~1.6s whenever a new wave
    /// starts. Pulses and fades. Built at runtime; subscribes to WaveManager.OnWaveChanged.
    /// </summary>
    public class WaveBanner : MonoBehaviour
    {
        private const float Duration = 1.6f;
        private static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

        private Text _text;
        private Image _ribbon;
        private float _t = -1f;

        private void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

            var ribSprite = Resources.Load<Sprite>("wave_ribbon");
            if (ribSprite != null)
            {
                _ribbon = UIBuilder.Image("WaveRibbon", canvas.transform, ribSprite, Color.white,
                    new Vector2(0.14f, 0.685f), new Vector2(0.86f, 0.80f), false);
                _ribbon.raycastTarget = false;
                _ribbon.enabled = false;
            }

            // Text on top of the ribbon (built after it → later sibling → drawn over it).
            _text = UIBuilder.Text("WaveBanner", canvas.transform, "WAVE 1", 48,
                new Color(0.35f, 0.22f, 0.05f), new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.78f));
            _text.fontStyle = FontStyle.Bold;
            _text.enabled = false;
            WaveManager.OnWaveChanged += Trigger;
        }

        private void OnDestroy() => WaveManager.OnWaveChanged -= Trigger;

        private void Trigger(int wave)
        {
            if (_text != null) _text.text = "WAVE " + wave;
            _t = 0f;
        }

        private void Update()
        {
            if (_t < 0f) { if (_text != null && _text.enabled) _text.enabled = false; if (_ribbon != null) _ribbon.enabled = false; return; }

            _t += Time.deltaTime;
            float u = _t / Duration;
            if (u >= 1f) { _t = -1f; if (_text != null) _text.enabled = false; if (_ribbon != null) _ribbon.enabled = false; return; }

            float scale = 1f + Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)) * 0.18f;
            float a = Mathf.Clamp01(u < 0.15f ? u / 0.15f : (u > 0.7f ? (1f - u) / 0.3f : 1f));

            _text.enabled = true;
            _text.transform.localScale = Vector3.one * scale;

            if (_ribbon != null)
            {
                _ribbon.enabled = true;
                _ribbon.transform.localScale = Vector3.one * scale;
                _ribbon.color = new Color(1f, 1f, 1f, a);
                _text.color = new Color(0.35f, 0.22f, 0.05f, a);   // dark text on the gold ribbon
            }
            else
            {
                _text.color = new Color(Gold.r, Gold.g, Gold.b, a);
            }
        }
    }
}
