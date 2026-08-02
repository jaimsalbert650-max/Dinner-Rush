using UnityEngine;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Kill-combo meter. Each kill bumps the combo; it resets if no kill lands within a short window.
    /// Shows a punchy "COMBO xN" on the HUD (fades as the window runs out) and drops a small coin
    /// bonus at every milestone. Built at runtime on the shared canvas.
    /// </summary>
    public class ComboHud : MonoBehaviour
    {
        [SerializeField] private float window = 2.5f;
        [SerializeField] private int showAt = 3;
        [SerializeField] private int milestone = 15;

        /// <summary>Highest combo reached this run — reported on the Game Over / Victory stat rows.</summary>
        public int BestCombo { get; private set; }

        private Text _text;
        private int _combo;
        private float _timer;
        private float _punch;

        private void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            _text = UIBuilder.Text("ComboText", canvas.transform, "", 44, new Color(1f, 0.85f, 0.3f),
                new Vector2(0.15f, 0.80f), new Vector2(0.85f, 0.875f));
            _text.fontStyle = FontStyle.Bold;
            _text.enabled = false;
            EnemyHealth.AnyKilled += OnKill;
        }

        private void OnDestroy() => EnemyHealth.AnyKilled -= OnKill;

        private void OnKill()
        {
            _combo++;
            if (_combo > BestCombo) BestCombo = _combo;
            _timer = window;
            _punch = 1f;
            if (milestone > 0 && _combo % milestone == 0)
            {
                var player = FindAnyObjectByType<PlayerRoot>();   // name-independent, matches the rest of the codebase
                if (player != null)
                    for (int i = 0; i < 3; i++)
                        CoinSpawner.Spawn(player.transform.position + (Vector3)(Random.insideUnitCircle * 0.7f));
            }
        }

        private void Update()
        {
            if (_combo > 0)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f) _combo = 0;
            }

            bool show = _combo >= showAt;
            if (_text != null) _text.enabled = show;
            if (!show) return;

            _text.text = "COMBO x" + _combo;
            _punch = Mathf.MoveTowards(_punch, 0f, Time.deltaTime * 4f);
            _text.transform.localScale = Vector3.one * (1f + _punch * 0.45f);

            // gold that shifts toward hot orange on big combos; fades as the window empties.
            float heat = Mathf.Clamp01(_combo / 40f);
            float a = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(_timer / window * 2f));
            _text.color = new Color(1f, Mathf.Lerp(0.85f, 0.45f, heat), Mathf.Lerp(0.3f, 0.1f, heat), a);
        }
    }
}
