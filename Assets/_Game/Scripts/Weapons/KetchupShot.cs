using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// One squirt of ketchup in flight — a short dash that travels straight from the bottle's nozzle
    /// like a bolt, marks the first customer it reaches with a <see cref="KetchupSplat"/> and vanishes.
    ///
    /// Distance-checked rather than collider-driven, like the aura: the enemy hurtboxes are triggers on
    /// kinematic bodies and this needs no physics wiring.
    /// </summary>
    public class KetchupShot : MonoBehaviour
    {
        private const float HitRadius = 0.34f;      // per unit of scale
        private const float TrailSeconds = 0.13f;

        // Static, so it outlives a scene load while its GameObjects do not — every pop skips the
        // corpses that leaves behind.
        private static readonly Stack<KetchupShot> Pool = new Stack<KetchupShot>();
        private static Material _trailMaterial;

        private SpriteRenderer _sr;
        private TrailRenderer _trail;
        private Vector2 _dir;
        private float _speed, _damage, _travelLeft, _length, _radius;
        private bool _alive;
        /// <summary>Seconds left of "the dash is gone but its trail is still fading". Deactivating on
        /// impact would cut the streak off in mid-air.</summary>
        private float _fadeLeft;

        public static void Spawn(Vector2 from, Vector2 dir, float speed, float damage, float range,
            Sprite art, float scale)
        {
            KetchupShot s = null;
            while (s == null && Pool.Count > 0) s = Pool.Pop();
            if (s == null)
            {
                var go = new GameObject("KetchupShot");
                s = go.AddComponent<KetchupShot>();
                s._sr = go.AddComponent<SpriteRenderer>();
                s._sr.sortingOrder = 21;      // over the crowd and the cook, like the bottle
                s._trail = BuildTrail(go);
            }

            s._sr.sprite = art;
            // The art pivots on its tail (the nozzle), so the head is one scaled length ahead.
            s._length = (art != null ? art.bounds.size.x : 0.5f) * scale;
            s._radius = HitRadius * scale;
            s._dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            s._speed = speed;
            s._damage = damage;
            s._travelLeft = range;
            s._alive = true;
            s._fadeLeft = 0f;
            s._sr.enabled = true;

            var t = s.transform;
            t.position = from;
            float deg = Mathf.Atan2(s._dir.y, s._dir.x) * Mathf.Rad2Deg;
            t.localRotation = Quaternion.Euler(0f, 0f, deg);
            // Mirror rather than let the dash hang upside down when firing left.
            bool mirror = Mathf.Abs(Mathf.DeltaAngle(deg, 0f)) > 90f;
            t.localScale = new Vector3(scale, mirror ? -scale : scale, 1f);

            s.gameObject.SetActive(true);
            if (s._trail != null)
            {
                // Just under the dash's own thickness, so the streak reads as its wake and the drawn
                // squirt stays the thing you look at.
                s._trail.widthMultiplier = 0.16f * scale;
                // A pooled trail still remembers where the last shot flew, and would draw a streak
                // across the arena from there. Clear AFTER the move, or it re-records the old point.
                s._trail.Clear();
            }
        }

        /// <summary>The streak of sauce behind the squirt: red fading to nothing over a tenth of a
        /// second, tapering to a point. Sprite-shader material, shared by every shot.</summary>
        private static TrailRenderer BuildTrail(GameObject go)
        {
            var child = new GameObject("Trail");
            child.transform.SetParent(go.transform, false);
            var tr = child.AddComponent<TrailRenderer>();

            if (_trailMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null) _trailMaterial = new Material(shader);
            }
            if (_trailMaterial != null) tr.material = _trailMaterial;

            tr.time = TrailSeconds;
            tr.minVertexDistance = 0.04f;
            tr.autodestruct = false;
            tr.numCapVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.sortingOrder = 20;             // under the dash itself, over the crowd
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.78f, 0.14f, 0.10f), 0f),
                        new GradientColorKey(new Color(0.55f, 0.08f, 0.06f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = grad;
            return tr;
        }

        private void Update()
        {
            if (!_alive)
            {
                // Spent: stand still while the trail runs out, then go back to the pool.
                _fadeLeft -= Time.deltaTime;
                if (_fadeLeft <= 0f) Recycle();
                return;
            }

            float step = _speed * Time.deltaTime;
            Vector2 wasTail = transform.position;
            transform.position += (Vector3)(_dir * step);
            _travelLeft -= step;

            // The whole squirt, swept: from where its tail was to where its head is now. Testing only
            // the head — which starts a body-length ahead of the nozzle — meant a customer standing
            // closer than that was inside the drawn sauce and never touched by it, which is exactly how
            // it flew through anyone the cook walked up to. Motion is along the body, so this one
            // segment covers both the length and the step.
            Vector2 head = (Vector2)transform.position + _dir * _length;
            float r2 = _radius * _radius;

            // Backwards: the kill this causes removes an enemy from the registry mid-loop.
            for (int i = EnemyMovement.ActiveCount - 1; i >= 0; i--)
            {
                EnemyMovement e = EnemyMovement.At(i);
                if (e == null || e.Health == null) continue;
                if (SqrDistToSegment(e.transform.position, wasTail, head) > r2) continue;

                KetchupSplat.Spawn(e.transform.position);
                e.Health.TakeDamage(_damage);
                Despawn();
                return;
            }

            if (_travelLeft <= 0f) Despawn();
        }

        private static float SqrDistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.000001f) return (p - a).sqrMagnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return (p - (a + ab * t)).sqrMagnitude;
        }

        /// <summary>Stop the dash but leave the object alive for one trail length, so the streak fades
        /// where the sauce landed instead of blinking out with it.</summary>
        private void Despawn()
        {
            if (!_alive) return;              // guards a second despawn in the same frame
            _alive = false;
            _fadeLeft = TrailSeconds;
            _sr.enabled = false;
        }

        private void Recycle()
        {
            gameObject.SetActive(false);
            Pool.Push(this);
        }
    }
}
