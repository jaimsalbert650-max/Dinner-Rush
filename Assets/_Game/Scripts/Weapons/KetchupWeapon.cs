using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// The ketchup bottle — the one tool the cook does not throw. It stays in his hand, turns to face
    /// the nearest customer and fires a squirt of sauce at them like a bolt from a pistol
    /// (<see cref="KetchupShot"/>), which marks whoever it hits with a fading red splat.
    ///
    /// The art is cut so the bottle and the squirt both pivot on the **nozzle**, so one rotating holder
    /// aims the bottle and every shot leaves exactly from its tip, at any angle.
    /// </summary>
    public class KetchupWeapon : Weapon
    {
        [SerializeField] private float targetRange = 8f;
        [SerializeField] private float baseDamage = 7f;
        [SerializeField] private float shotSpeed = 15f;
        /// <summary>How big the flying squirt is drawn (1 = the sprite's own size). Its hit radius and
        /// its trail scale with it, so this one number changes the whole read of the shot.</summary>
        [SerializeField] private float shotScale = 1.7f;
        /// <summary>Seconds the bottle takes to settle on a new angle. Without this the hand snapped
        /// every time the nearest customer changed, which is the twitch the player sees.</summary>
        [SerializeField] private float aimSmoothing = 0.09f;
        /// <summary>A new customer has to be this much closer than the one being aimed at to steal the
        /// aim. Two of them at nearly equal distance would otherwise trade the target every frame.</summary>
        [SerializeField] private float switchAdvantage = 0.75f;
        /// <summary>Where the bottle sits relative to the cook's centre — his hand height.</summary>
        [SerializeField] private Vector2 handOffset = new Vector2(0.04f, -0.10f);
        /// <summary>How far in front of that the nozzle ends up, so the body covers the hand.</summary>
        [SerializeField] private float bottleForward = 0.34f;

        private Sprite _shotArt;
        private Transform _holder;
        private SpriteRenderer _bottle;
        private Transform _target;
        private float _aimDeg, _aimVel;
        private bool _mirrored;

        private float Damage => baseDamage + (Level - 1) * 3f;
        /// <summary>Level pushes the squirt further as well as harder, so the tool grows in reach.</summary>
        private float Range => targetRange + (Level - 1) * 0.8f;

        private void Awake()
        {
            baseInterval = 0.55f;
            _shotArt = Resources.Load<Sprite>("fx/ketchup_shot");

            var holder = new GameObject("KetchupHand");
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = handOffset;
            _holder = holder.transform;

            // Over the cook, who sorts at 20 — a bottle hidden behind his coat is the whole point missed.
            _bottle = Piece("Bottle", Resources.Load<Sprite>("fx/ketchup_bottle"), 22);
            _bottle.transform.localPosition = new Vector3(bottleForward, 0f, 0f);
        }

        private void OnEnable()
        {
            _aimVel = 0f;              // a stale smoothing velocity would overshoot on the first frame
            if (_holder != null) _holder.gameObject.SetActive(true);
        }

        /// <summary>The Loadout can switch this tool off, and a disabled component still leaves its
        /// renderers on screen — so the bottle has to go with it.</summary>
        private void OnDisable() { if (_holder != null) _holder.gameObject.SetActive(false); }

        protected override void Update()
        {
            Aim();          // before the cooldown, so a shot leaves along this frame's angle
            // The cooldown does not run down while there is nobody to shoot: otherwise it burns away in
            // an empty room and the first customer to walk in gets up to a full interval for free.
            if (_target == null) return;
            base.Update();
        }

        /// <summary>Shots leave along the bottle as it is actually pointing, not at the target: while
        /// the hand is still swinging the sauce flies where the bottle looks, which is what sells it as
        /// aiming rather than tracking.</summary>
        protected override void Fire()
        {
            if (_target == null) return;                  // no shot into an empty room
            KetchupShot.Spawn(_bottle.transform.position, _holder.right, shotSpeed,
                Stats.RollDamage(Damage), Range, _shotArt, shotScale + (Level - 1) * 0.1f);
        }

        /// <summary>Keeps the bottle on one customer and eases toward them, instead of snapping to
        /// whoever happens to be nearest this frame. Holds the last angle when the room empties, so it
        /// never flicks back to a default pose.</summary>
        private void Aim()
        {
            PickTarget();

            if (_target != null)
            {
                Vector2 d = (Vector2)_target.position - (Vector2)_holder.position;
                if (d.sqrMagnitude > 0.0001f)
                {
                    float want = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    _aimDeg = Mathf.SmoothDampAngle(_aimDeg, want, ref _aimVel, aimSmoothing);
                }
            }

            _holder.localRotation = Quaternion.Euler(0f, 0f, _aimDeg);

            // Mirror rather than rotate past vertical, or the label ends up upside down. The 85/95
            // deadband stops it flickering while the aim sits right on the boundary.
            float fromRight = Mathf.Abs(Mathf.DeltaAngle(_aimDeg, 0f));
            if (!_mirrored && fromRight > 95f) _mirrored = true;
            else if (_mirrored && fromRight < 85f) _mirrored = false;
            _holder.localScale = new Vector3(1f, _mirrored ? -1f : 1f, 1f);
        }

        /// <summary>Sticky targeting: the current customer keeps the aim until they are gone or out of
        /// reach, and a rival has to be clearly closer (<see cref="switchAdvantage"/>) to take over —
        /// two of them at similar distance would otherwise swap every frame and shake the bottle.</summary>
        private void PickTarget()
        {
            float range = Range;
            bool keep = _target != null && _target.gameObject.activeInHierarchy
                        && ((Vector2)_target.position - (Vector2)transform.position).sqrMagnitude <= range * range;

            var nearest = NearestEnemy(range);
            if (nearest == null) { _target = keep ? _target : null; return; }
            if (!keep) { _target = nearest; return; }
            if (nearest == _target) return;

            float dCur = ((Vector2)_target.position - (Vector2)transform.position).magnitude;
            float dNew = ((Vector2)nearest.position - (Vector2)transform.position).magnitude;
            if (dNew < dCur * switchAdvantage) _target = nearest;
        }

        private SpriteRenderer Piece(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_holder, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
