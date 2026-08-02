using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Whole-body procedural animation for the single assembled chef sprite (CookRig/ChefWhole,
    /// created by <see cref="ChefVisual"/>). Nothing can detach because it's one sprite:
    /// Idle   - gentle bob + subtle breathing squash.
    /// Walk   - bigger bob, squash-and-stretch, side lean, and flips to face the move direction.
    /// Attack - a quick upward pop + squash punch when the weapon fires.
    /// Hit    - a compress + recoil tilt when the player takes damage.
    /// </summary>
    public class CookAnimator : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;

        [Header("Idle")]
        [SerializeField] private float idleBob = 0.04f;
        [SerializeField] private float idleSpeed = 3f;
        [SerializeField] private float idleBreath = 0.02f;   // gentle breathing (oscillates around 1)

        [Header("Walk")]
        [SerializeField] private float walkBob = 0.12f;
        [SerializeField] private float walkSpeed = 11f;
        [SerializeField] private float walkLean = 4f;
        [SerializeField] private float walkSquash = 0.04f;   // subtle, oscillates around 1

        [Header("Attack")]
        [SerializeField] private float attackPop = 0.12f;
        [SerializeField] private float attackStretch = 0.1f; // pop UP and stretch taller (never flat)
        [SerializeField] private float attackDuration = 0.16f;
        [SerializeField] private float throwTilt = 16f;      // forward tilt when throwing the spatula

        [Header("Hit")]
        [SerializeField] private float hitFlinch = 0.06f;
        [SerializeField] private float hitDuration = 0.16f;

        [Header("Facing")]
        [SerializeField] private bool faceFlip = true;

        private Transform _t;
        private SpriteRenderer _sr;
        private Vector3 _baseScale = Vector3.one;
        private float _baseY;
        private float _phase, _attackT = -1f, _flinchT = -1f, _facing = 1f;
        private Vector2 _lastPos;
        private float _moveSpeed;
        private PlayerHealth _health;
        private float _lastHp;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            _lastPos = rb != null ? rb.position : (Vector2)transform.position;
        }

        private void Start()
        {
            _t = transform.Find("CookRig/ChefWhole");
            if (_t == null) _t = transform.Find("CookRig");
            if (_t != null)
            {
                _baseScale = _t.localScale;
                _baseY = _t.localPosition.y;
                _sr = _t.GetComponent<SpriteRenderer>();
            }
            if (TryGetComponent<KnifeWeapon>(out var w)) w.OnFired += OnAttack;
            if (TryGetComponent<PlayerHealth>(out _health)) { _health.OnChanged += OnHp; _lastHp = _health.Current; }
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnChanged -= OnHp;
            if (TryGetComponent<KnifeWeapon>(out var w)) w.OnFired -= OnAttack;   // balance the Start subscription
        }

        private void OnHp(float cur, float max)
        {
            if (cur < _lastHp - 0.01f) _flinchT = 0f;
            _lastHp = cur;
        }

        public void OnAttack() => _attackT = 0f;

        private void Update()
        {
            if (_t == null) return;
            float dt = Time.deltaTime;
            Vector2 cur = rb != null ? rb.position : (Vector2)transform.position;
            float dx = cur.x - _lastPos.x;
            float instant = dt > 0f ? (cur - _lastPos).magnitude / dt : 0f;
            _lastPos = cur;
            _moveSpeed = Mathf.Lerp(_moveSpeed, instant, 1f - Mathf.Exp(-dt * 12f));
            bool moving = _moveSpeed > 0.1f;
            _phase += dt * (moving ? walkSpeed : idleSpeed);

            if (faceFlip && Mathf.Abs(dx) > 0.0015f)
            {
                _facing = dx < 0f ? -1f : 1f;
                if (_sr != null) _sr.flipX = dx < 0f;
            }

            float swing = Mathf.Sin(_phase), bounce = Mathf.Abs(swing);
            float atk = Progress(ref _attackT, attackDuration, dt);
            float fln = Progress(ref _flinchT, hitDuration, dt);
            float atkP = atk >= 0f ? Mathf.Sin(atk * Mathf.PI) : 0f;
            float flnP = fln >= 0f ? Mathf.Sin(fln * Mathf.PI) : 0f;

            // position: bob + attack pop up + flinch dip
            float bob = bounce * (moving ? walkBob : idleBob);
            var lp = _t.localPosition;
            lp.y = _baseY + bob + atkP * attackPop - flnP * 0.05f;
            _t.localPosition = lp;

            // squash/stretch — attack STRETCHES taller (never flattens); walk/idle oscillate around 1
            float squash = moving ? swing * walkSquash : Mathf.Sin(_phase) * idleBreath;
            float sy = 1f + squash + atkP * attackStretch - flnP * hitFlinch;
            float sx = 1f - squash * 0.5f - atkP * attackStretch * 0.5f - flnP * hitFlinch * 0.5f;
            _t.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);

            // lean while walking + a forward tilt when throwing the spatula
            float lean = (moving ? swing * walkLean : 0f) - atkP * throwTilt * _facing;
            _t.localRotation = Quaternion.Euler(0f, 0f, lean);
        }

        private static float Progress(ref float t, float duration, float dt)
        {
            if (t < 0f) return -1f;
            t += dt;
            float u = t / duration;
            if (u >= 1f) { t = -1f; return 1f; }
            return u;
        }
    }
}
