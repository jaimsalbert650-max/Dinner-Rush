using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Walk animation for an enemy, applied to its "Body" child (created by EnemyVisual) so it never
    /// fights the physics root: a hop (bob), a side-to-side waddle, squash-and-stretch, and a flip to
    /// face the movement direction. A base vertical stretch un-flattens the squat visitor sprites.
    /// Motion is read from position change (EnemyMovement uses MovePosition). Pool-safe.
    /// </summary>
    public class EnemyAnimator : MonoBehaviour
    {
        [SerializeField] private Transform visual;     // resolved to the "Body" child
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float wobbleSpeed = 10f;
        [SerializeField] private float squash = 0.08f;
        [SerializeField] private float bob = 0.07f;
        [SerializeField] private float waddle = 6f;
        [SerializeField] private Vector2 stretch = new Vector2(0.92f, 1.12f);
        [SerializeField] private bool faceFlip = true;

        private Vector3 _baseScale = Vector3.one;
        private float _baseY;
        private SpriteRenderer _sr;
        private Vector2 _lastPos;
        private float _moveSpeed, _phase, _scaleMult = 1f, _facing = 1f;
        private bool _init;

        /// <summary>Multiplier applied on top of the base scale — used by the spawner for size variants.</summary>
        public void SetBaseScaleMult(float m) { _scaleMult = Mathf.Max(0.1f, m); ResetPose(); }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            _lastPos = rb != null ? rb.position : (Vector2)transform.position;
        }

        private void OnEnable()
        {
            _lastPos = rb != null ? rb.position : (Vector2)transform.position;
            Resolve();
            ResetPose();
        }

        private void Resolve()
        {
            if (_init) return;
            if (visual == null || visual == transform) visual = transform.Find("Body");
            if (visual == null) return;
            _baseScale = new Vector3(stretch.x, stretch.y, 1f);
            _baseY = visual.localPosition.y;
            _sr = visual.GetComponent<SpriteRenderer>();
            _init = true;
        }

        private void ResetPose()
        {
            if (visual == null) return;
            visual.localScale = new Vector3(_baseScale.x * _scaleMult, _baseScale.y * _scaleMult, 1f);
            var lp = visual.localPosition; lp.y = _baseY; visual.localPosition = lp;
            visual.localRotation = Quaternion.identity;
        }

        /// <summary>Driven by <see cref="EnemyMovement.TickAll"/>, not by the engine — see the note there.</summary>
        public void Tick(float dt)
        {
            Resolve();
            if (visual == null) return;

            Vector2 cur = rb != null ? rb.position : (Vector2)transform.position;
            float dx = cur.x - _lastPos.x;
            float instant = dt > 0f ? (cur - _lastPos).magnitude / dt : 0f;
            _lastPos = cur;
            _moveSpeed = Mathf.Lerp(_moveSpeed, instant, 1f - Mathf.Exp(-dt * 12f));
            bool moving = _moveSpeed > 0.1f;
            _phase += dt * (moving ? wobbleSpeed : 0f);

            if (faceFlip && Mathf.Abs(dx) > 0.001f)
            {
                _facing = dx < 0f ? -1f : 1f;
                if (_sr != null) _sr.flipX = dx < 0f;
            }

            float s = moving ? Mathf.Sin(_phase) : 0f;    // -1..1
            float bounce = Mathf.Abs(s);                  // 0..1, one hop per step

            float bx = _baseScale.x * _scaleMult, by = _baseScale.y * _scaleMult;
            visual.localScale = new Vector3(bx * (1f - s * squash), by * (1f + s * squash), 1f);

            var lp = visual.localPosition;
            lp.y = _baseY + bounce * bob * _scaleMult;
            visual.localPosition = lp;

            visual.localRotation = Quaternion.Euler(0f, 0f, moving ? s * waddle : 0f);
        }
    }
}
