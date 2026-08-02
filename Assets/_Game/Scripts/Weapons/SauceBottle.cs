using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// One thrown bottle of hot sauce, from the arc to the last ember: it tumbles in on frames 1–2,
    /// bursts (3), the puddle catches (4–7) and then burns (8) until it fades.
    ///
    /// The art carries the whole effect, so this only decides where it lands and who it hurts. Frames
    /// that have landed pivot on their own ground line, which is why the puddle sits exactly where the
    /// bottle hit without a single offset.
    /// </summary>
    public class SauceBottle : MonoBehaviour
    {
        private const float FlightSeconds = 0.55f, ArcHeight = 2.2f;
        private const float SplashSeconds = 0.10f, IgniteFrameTime = 0.07f;
        private const float FadeSeconds = 0.45f;
        private const float DamageInterval = 0.35f;

        // Static, so it outlives a scene load while its GameObjects do not — pops skip the corpses.
        private static readonly Stack<SauceBottle> Pool = new Stack<SauceBottle>();
        private static Sprite[] _frames;

        private enum Phase { Flight, Splash, Ignite, Burn, Fade }

        private SpriteRenderer _sr;
        private Phase _phase;
        private Vector2 _from, _to;
        private float _t, _damageT, _radius, _dps, _burnSeconds;

        public static void Throw(Vector2 from, Vector2 to, float radius, float dps, float burnSeconds)
        {
            SauceBottle b = null;
            while (b == null && Pool.Count > 0) b = Pool.Pop();
            if (b == null)
            {
                var go = new GameObject("SauceBottle");
                b = go.AddComponent<SauceBottle>();
                b._sr = go.AddComponent<SpriteRenderer>();
            }
            EnsureArt();

            b._from = from; b._to = to;
            b._radius = radius; b._dps = dps; b._burnSeconds = burnSeconds;
            b._phase = Phase.Flight;
            b._t = 0f; b._damageT = 0f;
            b._sr.color = Color.white;
            b.transform.position = from;
            b.gameObject.SetActive(true);
            b.Draw(0);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Flight:
                {
                    float u = Mathf.Clamp01(_t / FlightSeconds);
                    Vector2 flat = Vector2.Lerp(_from, _to, u);
                    // A thrown arc, so it reads as going over the crowd rather than through it.
                    float lift = ArcHeight * 4f * u * (1f - u);
                    Vector2 next = flat + Vector2.up * lift;

                    Vector2 step = next - (Vector2)transform.position;
                    if (step.sqrMagnitude > 0.000001f)
                        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg - 90f);
                    transform.position = next;

                    Draw(Mathf.FloorToInt(_t * 14f) % 2);          // tumble between the two flight frames
                    if (u >= 1f)
                    {
                        _phase = Phase.Splash; _t = 0f;
                        transform.position = _to;
                        transform.localRotation = Quaternion.identity;   // everything from here lies flat
                        Draw(2);
                        Damage();                                        // the burst itself hurts
                    }
                    break;
                }
                case Phase.Splash:
                    if (_t >= SplashSeconds) { _phase = Phase.Ignite; _t = 0f; }
                    break;

                case Phase.Ignite:
                {
                    int f = 3 + Mathf.FloorToInt(_t / IgniteFrameTime);
                    if (f >= 7) { _phase = Phase.Burn; _t = 0f; Draw(7); break; }
                    Draw(f);
                    break;
                }
                case Phase.Burn:
                {
                    // Actually burn: cycle the three lit frames instead of holding the last one. Held
                    // with a 2% breath it read as a painted decal, not as fire.
                    Draw(5 + Mathf.FloorToInt(_t * 10f) % 3);
                    _damageT -= Time.deltaTime;
                    if (_damageT <= 0f) { _damageT = DamageInterval; Damage(); }
                    if (_t >= _burnSeconds) { _phase = Phase.Fade; _t = 0f; }
                    break;
                }
                default:
                {
                    float k = Mathf.Clamp01(_t / FadeSeconds);
                    _sr.color = new Color(1f, 1f, 1f, 1f - k);
                    if (k >= 1f)
                    {
                        gameObject.SetActive(false);
                        Pool.Push(this);
                    }
                    break;
                }
            }
        }

        /// <summary>Damages everything standing in the puddle. Backwards, because a kill takes the enemy
        /// out of the registry mid-loop.</summary>
        private void Damage()
        {
            float r2 = _radius * _radius;
            float amount = _dps * DamageInterval;
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                var e = EnemyMovement.At(i);
                if (e == null || e.Health == null) continue;
                if (((Vector2)e.transform.position - _to).sqrMagnitude > r2) continue;
                e.Health.TakeDamage(amount);
            }
        }

        /// <summary>Draws a frame. The landed frames are scaled so the full pool covers exactly the
        /// radius that burns — what you see on the floor is what hurts.</summary>
        private void Draw(int index, float extra = 1f)
        {
            if (_frames == null) return;
            var s = _frames[Mathf.Clamp(index, 0, _frames.Length - 1)];
            if (s == null) return;
            _sr.sprite = s;
            // In the air it flies over everything; once it has landed it is FLOOR — under the crowd
            // (2) and under the cook (20), so customers stand in the puddle instead of behind it.
            _sr.sortingOrder = index < 2 ? 25 : 1;

            if (index < 2) { transform.localScale = Vector3.one; return; }

            var full = _frames[7];
            float k = full != null && full.bounds.size.x > 0.01f ? _radius * 2f / full.bounds.size.x : 1f;
            transform.localScale = new Vector3(k * extra, k * extra, 1f);
        }

        private static void EnsureArt()
        {
            if (_frames != null) return;
            _frames = new Sprite[8];
            for (int i = 0; i < 8; i++) _frames[i] = Resources.Load<Sprite>("fx/molotov_" + (i + 1));
        }
    }
}
