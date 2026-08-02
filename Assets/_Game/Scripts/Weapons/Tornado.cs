using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A spinning funnel of spaghetti that circles the cook, drags customers into its throat, chews them
    /// while it spins, and then flings everything it caught outward before it unwinds.
    ///
    /// It moves the crowd by writing straight to their bodies rather than asking them to walk: they are
    /// kinematic and script-driven, so the pull simply has to be stronger than whatever they were doing.
    /// </summary>
    public class Tornado : MonoBehaviour
    {
        private const float FrameTime = 0.06f;          // 8 frames, a little over half a second a turn
        private const float DamageInterval = 0.3f;
        private const float ThrowSeconds = 0.4f;
        private const float FadeSeconds = 0.35f;
        /// <summary>How fast it drags a customer toward its throat, and how close counts as inside.</summary>
        private const float PullSpeed = 5.5f, CoreRadius = 0.55f;
        private const float ThrowSpeed = 14f;
        /// <summary>It circles the cook rather than sitting on him — a moving hazard reads as alive, and
        /// it sweeps different parts of the crowd as it goes.</summary>
        private const float OrbitRadius = 2.6f, OrbitSpeed = 95f;

        private static readonly Stack<Tornado> Pool = new Stack<Tornado>();
        private static Sprite[] _frames;

        private enum Phase { Spin, Throw, Fade }

        private SpriteRenderer _sr;
        private Transform _owner;
        private Phase _phase;
        private float _t, _damageT, _radius, _dps, _throwDamage, _liveSeconds, _angle;
        private readonly List<Transform> _caught = new List<Transform>();
        private readonly List<Vector2> _flingDirs = new List<Vector2>();

        public static void Summon(Transform owner, float radius, float dps, float throwDamage, float liveSeconds)
        {
            Tornado t = null;
            while (t == null && Pool.Count > 0) t = Pool.Pop();
            if (t == null)
            {
                var go = new GameObject("Tornado");
                t = go.AddComponent<Tornado>();
                t._sr = go.AddComponent<SpriteRenderer>();
                // Over the crowd it swallows, under the cook — the one thing that must stay readable.
                t._sr.sortingOrder = 15;
            }
            EnsureArt();

            t._owner = owner;
            t._radius = radius;
            t._dps = dps;
            t._throwDamage = throwDamage;
            t._liveSeconds = liveSeconds;
            t._phase = Phase.Spin;
            t._t = 0f;
            t._damageT = 0f;
            t._angle = Random.Range(0f, 360f);
            t._caught.Clear();
            t._flingDirs.Clear();
            t._sr.color = Color.white;
            t.transform.localScale = Vector3.one;
            t.Place();
            t.gameObject.SetActive(true);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_frames != null && _frames.Length > 0)
                _sr.sprite = _frames[Mathf.FloorToInt(_t / FrameTime) % _frames.Length];

            switch (_phase)
            {
                case Phase.Spin:
                    _angle += OrbitSpeed * Time.deltaTime;
                    Place();
                    Pull();
                    _damageT -= Time.deltaTime;
                    if (_damageT <= 0f) { _damageT = DamageInterval; Damage(_dps * DamageInterval); }
                    if (_t >= _liveSeconds) { _phase = Phase.Throw; _t = 0f; Catch(); }
                    break;

                case Phase.Throw:
                    Fling();
                    if (_t >= ThrowSeconds) { _phase = Phase.Fade; _t = 0f; }
                    break;

                default:
                {
                    float k = Mathf.Clamp01(_t / FadeSeconds);
                    _sr.color = new Color(1f, 1f, 1f, 1f - k);
                    transform.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.7f, k), 1f);   // unwinds
                    if (k >= 1f)
                    {
                        gameObject.SetActive(false);
                        Pool.Push(this);
                    }
                    break;
                }
            }
        }

        /// <summary>Circles the cook. Follows him, so walking away does not leave it behind.</summary>
        private void Place()
        {
            if (_owner == null) return;
            float r = _angle * Mathf.Deg2Rad;
            transform.position = (Vector2)_owner.position + new Vector2(Mathf.Cos(r), Mathf.Sin(r) * 0.6f) * OrbitRadius;
        }

        /// <summary>Drags everything in reach toward the throat. Writes the body position directly —
        /// these are kinematic and drive themselves, so the pull has to overrule them, not ask.</summary>
        private void Pull()
        {
            Vector2 c = transform.position;
            float r2 = _radius * _radius;
            float step = PullSpeed * Time.deltaTime;

            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null) continue;
                Vector2 p = e.transform.position;
                Vector2 d = c - p;
                if (d.sqrMagnitude > r2) continue;

                float dist = d.magnitude;
                Vector2 next = dist <= CoreRadius
                    ? c + (dist > 0.001f ? d / dist : Vector2.up) * -CoreRadius   // held in the throat
                    : p + d / Mathf.Max(0.001f, dist) * Mathf.Min(step, dist - CoreRadius);

                e.transform.position = next;
                var rb = e.GetComponent<Rigidbody2D>();
                if (rb != null) rb.position = next;
            }
        }

        private void Damage(float amount)
        {
            Vector2 c = transform.position;
            float r2 = _radius * _radius;
            // Backwards: a kill takes the enemy out of the registry mid-loop.
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null || e.Health == null) continue;
                if (((Vector2)e.transform.position - c).sqrMagnitude > r2) continue;
                e.Health.TakeDamage(amount);
            }
        }

        /// <summary>Snapshot of who is in the funnel when it lets go, with the direction each one leaves
        /// in — taken once, so the throw stays straight even as they fly apart.</summary>
        private void Catch()
        {
            _caught.Clear();
            _flingDirs.Clear();
            Vector2 c = transform.position;
            float r2 = _radius * _radius;

            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null) continue;
                Vector2 p = e.transform.position;
                if ((p - c).sqrMagnitude > r2) continue;

                Vector2 away = p - c;
                if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle.normalized;
                _caught.Add(e.transform);
                _flingDirs.Add(away.normalized);
            }
            Damage(_throwDamage);      // the release itself hurts
        }

        private void Fling()
        {
            float step = ThrowSpeed * Time.deltaTime;
            for (int i = 0; i < _caught.Count; i++)
            {
                var t = _caught[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                Vector2 next = (Vector2)t.position + _flingDirs[i] * step;
                t.position = next;
                var rb = t.GetComponent<Rigidbody2D>();
                if (rb != null) rb.position = next;
            }
        }

        private static void EnsureArt()
        {
            if (_frames != null) return;
            _frames = new Sprite[8];
            for (int i = 0; i < 8; i++) _frames[i] = Resources.Load<Sprite>("fx/tornado_" + (i + 1));
        }
    }
}
