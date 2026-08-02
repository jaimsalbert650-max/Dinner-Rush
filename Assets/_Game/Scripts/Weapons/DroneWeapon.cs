using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Summons drones that orbit the player and auto-fire homing missiles at the nearest enemy.
    /// Each level adds another drone (the reference's "Type-A Drone... fires many missiles").
    /// </summary>
    public class DroneWeapon : Weapon
    {
        [SerializeField] private float orbitRadius = 1.15f;
        [SerializeField] private float orbitSpeed = 110f;
        [SerializeField] private float targetRange = 8f;
        [SerializeField] private float missileDamage = 6f;

        private readonly List<Transform> _drones = new();
        private readonly Stack<Missile> _pool = new();
        private float _angle;

        private void Awake()
        {
            baseInterval = 1.1f;
            EnsureDrones();
        }

        public override void LevelUp()
        {
            base.LevelUp();
            EnsureDrones();
        }

        protected override void Update()
        {
            base.Update();
            _angle += orbitSpeed * Time.deltaTime;
            int n = _drones.Count;
            for (int i = 0; i < n; i++)
            {
                float a = (_angle + i * (360f / n)) * Mathf.Deg2Rad;
                _drones[i].localPosition = new Vector3(Mathf.Cos(a) * orbitRadius, Mathf.Sin(a) * orbitRadius, 0f);
            }
        }

        protected override void Fire()
        {
            Transform target = NearestEnemy(targetRange);
            if (target == null) return;
            float dmg = missileDamage * (1f + (Level - 1) * 0.25f);
            foreach (var drone in _drones)
            {
                Missile m = _pool.Count > 0 ? _pool.Pop() : Create();
                m.Launch(drone.position, target, Stats.RollDamage(dmg), x => _pool.Push(x));   // Sharp Knife rolls crit per missile
            }
        }

        private void EnsureDrones()
        {
            while (_drones.Count < Level)
                _drones.Add(BuildDrone());
        }

        private Transform BuildDrone()
        {
            var go = new GameObject("Drone");
            go.transform.SetParent(transform, false);
            AddSprite(go.transform, RoundedRect(24, 18, 6), new Color(0.24f, 0.26f, 0.30f), new Vector3(0.42f, 0.42f, 1f), Vector3.zero, 15);
            AddSprite(go.transform, Circle(12), new Color(0.95f, 0.25f, 0.22f), new Vector3(0.14f, 0.14f, 1f), new Vector3(0f, 0.02f, 0f), 16);
            return go.transform;
        }

        private Missile Create()
        {
            var go = new GameObject("Missile");
            return go.AddComponent<Missile>();
        }

        private void AddSprite(Transform parent, Sprite s, Color c, Vector3 scale, Vector3 lp, int order)
        {
            var go = new GameObject("Piece");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
        }

        private static Sprite Circle(int d)
        {
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            var cols = new Color32[d * d];
            float r = d * 0.5f - 0.5f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - d * 0.5f + 0.5f, dy = y - d * 0.5f + 0.5f;
                    cols[y * d + x] = new Color32(255, 255, 255, (byte)(dx * dx + dy * dy <= r * r ? 255 : 0));
                }
            tex.SetPixels32(cols); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), d);
        }

        private static Sprite RoundedRect(int w, int h, int radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cols = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, radius, w - 1 - radius), cy = Mathf.Clamp(y, radius, h - 1 - radius);
                    float dx = x - cx, dy = y - cy;
                    cols[y * w + x] = new Color32(255, 255, 255, (byte)(dx * dx + dy * dy <= radius * radius ? 255 : 0));
                }
            tex.SetPixels32(cols); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
