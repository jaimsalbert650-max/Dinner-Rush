# Dinner Rush — Bullet Heaven Milestone 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the playable core loop of a 2D top-down auto-attack roguelike: a cook moves with WASD, auto-throws knives at swarming visitors, collects XP, levels up to pick upgrades, and dies when HP hits 0.

**Architecture:** Data-driven, event-decoupled Unity 2D. Pure C# logic classes (`PlayerStats`, `LevelProgression`, `ObjectPool<T>`) are unit-tested in EditMode; MonoBehaviours wrap them and are verified in Play mode. Systems communicate through C# events and a shared `PlayerStats` instance. First-party code lives under `Assets/_Game/`; third-party art stays in `Assets/External Assets/`.

**Tech Stack:** Unity 2D, new Input System (`Assets/InputSystem_Actions`), Rigidbody2D physics, ScriptableObjects, Unity Test Framework (NUnit EditMode).

**Conventions for every task:**
- Scripts are created with the `script-update-or-create` skill (or written to disk then `assets-refresh`).
- After adding/changing `.cs` files, run `assets-refresh` and confirm **no compile errors** in the Console (`console-get-logs`) before continuing.
- EditMode tests run with the `tests-run` skill (`testMode: EditMode`).
- Namespace for all gameplay scripts: `DinnerRush`. Tests: `DinnerRush.Tests`.
- Commit at the end of every task. Append the trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` to each commit.

---

## Task 0: Project scaffolding (folders + assemblies + layers)

**Files:**
- Create folders under `Assets/_Game/` (see spec structure).
- Create: `Assets/_Game/Scripts/DinnerRush.asmdef`
- Create: `Assets/_Game/Tests/EditMode/DinnerRush.Tests.asmdef`
- Modify: `ProjectSettings/TagManager.asset` (layers) — done via Editor, verified in code.

- [ ] **Step 1: Create the folder tree**

Use `assets-create-folder` to create, in order (parents first):
```
Assets/_Game
Assets/_Game/Scenes
Assets/_Game/Scripts
Assets/_Game/Scripts/Core
Assets/_Game/Scripts/Player
Assets/_Game/Scripts/Weapons
Assets/_Game/Scripts/Enemies
Assets/_Game/Scripts/Pickups
Assets/_Game/Scripts/Progression
Assets/_Game/Scripts/UI
Assets/_Game/Prefabs
Assets/_Game/ScriptableObjects
Assets/_Game/ScriptableObjects/Upgrades
Assets/_Game/Art
Assets/_Game/Tests
Assets/_Game/Tests/EditMode
```

- [ ] **Step 2: Create the runtime assembly definition**

Create `Assets/_Game/Scripts/DinnerRush.asmdef`:
```json
{
    "name": "DinnerRush",
    "rootNamespace": "DinnerRush",
    "references": [
        "GUID:75469ad4d38634e559750d17036d5f7c"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```
> The reference GUID above is Unity's Input System assembly (`Unity.InputSystem`). If `assets-refresh` reports it can't resolve the reference, open the asmdef in the Inspector and add `Unity.InputSystem` from the reference picker instead.

- [ ] **Step 3: Create the test assembly definition**

Create `Assets/_Game/Tests/EditMode/DinnerRush.Tests.asmdef`:
```json
{
    "name": "DinnerRush.Tests",
    "rootNamespace": "DinnerRush.Tests",
    "references": [
        "DinnerRush",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Add physics layers**

Add these User Layers via the Editor (Project Settings → Tags and Layers), or via `script-execute` writing to `TagManager`: `Player`, `Enemy`, `Projectile`, `Pickup`.
Then set the Physics2D collision matrix (Project Settings → Physics 2D):
- `Projectile` × `Enemy`: ON
- `Enemy` × `Player`: ON
- `Pickup` × `Player`: ON
- `Enemy` × `Enemy`: OFF
- `Projectile` × `Player`, `Projectile` × `Pickup`, `Projectile` × `Projectile`: OFF

- [ ] **Step 5: Refresh & verify no errors**

Run `assets-refresh`. Then `console-get-logs` (filter Error). Expected: no compile errors; both assemblies compile.

- [ ] **Step 6: Commit**
```bash
git add "Assets/_Game" "ProjectSettings/TagManager.asset" "ProjectSettings/Physics2DSettings.asset"
git commit -m "chore: scaffold _Game folders, assemblies, and physics layers"
```

---

## Task 1: PlayerStats (pure C#, TDD)

**Files:**
- Create: `Assets/_Game/Scripts/Core/PlayerStats.cs`
- Test: `Assets/_Game/Tests/EditMode/PlayerStatsTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/EditMode/PlayerStatsTests.cs`:
```csharp
using NUnit.Framework;
using DinnerRush;

namespace DinnerRush.Tests
{
    public class PlayerStatsTests
    {
        [Test]
        public void Defaults_AreSet()
        {
            var s = new PlayerStats();
            Assert.AreEqual(5f, s.MoveSpeed);
            Assert.AreEqual(100f, s.MaxHP);
            Assert.AreEqual(10f, s.Damage);
            Assert.AreEqual(1f, s.AttackRate);
            Assert.AreEqual(1.5f, s.PickupRadius);
        }

        [Test]
        public void AddModifier_IncreasesStat()
        {
            var s = new PlayerStats();
            s.AddModifier(StatType.Damage, 5f);
            Assert.AreEqual(15f, s.Damage);
        }

        [Test]
        public void AddModifier_Stacks()
        {
            var s = new PlayerStats();
            s.AddModifier(StatType.MoveSpeed, 1f);
            s.AddModifier(StatType.MoveSpeed, 2f);
            Assert.AreEqual(8f, s.MoveSpeed);
        }
    }
}
```

- [ ] **Step 2: Run test, verify it fails**

Run `tests-run` (EditMode, filter `PlayerStatsTests`).
Expected: FAIL — `PlayerStats` / `StatType` do not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

Create `Assets/_Game/Scripts/Core/PlayerStats.cs`:
```csharp
namespace DinnerRush
{
    public enum StatType { MoveSpeed, MaxHP, Damage, AttackRate, PickupRadius }

    /// <summary>
    /// Runtime, mutable stat block for the player. Systems read from it;
    /// upgrades write to it via AddModifier. Plain C# so it is unit-testable.
    /// </summary>
    public class PlayerStats
    {
        public float MoveSpeed = 5f;
        public float MaxHP = 100f;
        public float Damage = 10f;
        public float AttackRate = 1f;      // attacks per second
        public float PickupRadius = 1.5f;

        public void AddModifier(StatType type, float amount)
        {
            switch (type)
            {
                case StatType.MoveSpeed: MoveSpeed += amount; break;
                case StatType.MaxHP: MaxHP += amount; break;
                case StatType.Damage: Damage += amount; break;
                case StatType.AttackRate: AttackRate += amount; break;
                case StatType.PickupRadius: PickupRadius += amount; break;
            }
        }
    }
}
```

- [ ] **Step 4: Run test, verify it passes**

Run `tests-run` (EditMode, `PlayerStatsTests`). Expected: 3 PASS.

- [ ] **Step 5: Commit**
```bash
git add "Assets/_Game/Scripts/Core/PlayerStats.cs" "Assets/_Game/Tests/EditMode/PlayerStatsTests.cs"
git commit -m "feat: add PlayerStats runtime stat block with tests"
```

---

## Task 2: LevelProgression (pure C#, TDD)

**Files:**
- Create: `Assets/_Game/Scripts/Progression/LevelProgression.cs`
- Test: `Assets/_Game/Tests/EditMode/LevelProgressionTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/EditMode/LevelProgressionTests.cs`:
```csharp
using NUnit.Framework;
using DinnerRush;

namespace DinnerRush.Tests
{
    public class LevelProgressionTests
    {
        [Test]
        public void StartsAtLevelOne()
        {
            var p = new LevelProgression();
            Assert.AreEqual(1, p.Level);
            Assert.AreEqual(0, p.CurrentXp);
            Assert.AreEqual(5, p.XpForNextLevel);
        }

        [Test]
        public void AddXp_BelowThreshold_NoLevelUp()
        {
            var p = new LevelProgression();
            int gained = p.AddXp(3);
            Assert.AreEqual(0, gained);
            Assert.AreEqual(1, p.Level);
            Assert.AreEqual(3, p.CurrentXp);
        }

        [Test]
        public void AddXp_CrossThreshold_LevelsUpAndCarriesRemainder()
        {
            var p = new LevelProgression(); // needs 5 for L2
            int gained = p.AddXp(7);
            Assert.AreEqual(1, gained);
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(2, p.CurrentXp);      // 7 - 5 = 2
            Assert.AreEqual(15, p.XpForNextLevel); // 5 + (2-1)*10
        }

        [Test]
        public void AddXp_MultipleLevelsInOneGrant()
        {
            var p = new LevelProgression(); // L2 costs 5, L3 costs 15
            int gained = p.AddXp(20);        // 5 -> L2, 15 -> L3, 0 left
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, p.Level);
            Assert.AreEqual(0, p.CurrentXp);
        }
    }
}
```

- [ ] **Step 2: Run test, verify it fails**

Run `tests-run` (EditMode, `LevelProgressionTests`). Expected: FAIL — type does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `Assets/_Game/Scripts/Progression/LevelProgression.cs`:
```csharp
namespace DinnerRush
{
    /// <summary>
    /// Pure XP/level bookkeeping. XP required for the next level scales linearly:
    /// XpForNextLevel = 5 + (Level - 1) * 10.
    /// </summary>
    public class LevelProgression
    {
        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; } = 0;

        public int XpForNextLevel => 5 + (Level - 1) * 10;

        /// <summary>Adds XP and returns how many level-ups it triggered.</summary>
        public int AddXp(int amount)
        {
            CurrentXp += amount;
            int levelsGained = 0;
            while (CurrentXp >= XpForNextLevel)
            {
                CurrentXp -= XpForNextLevel;
                Level++;
                levelsGained++;
            }
            return levelsGained;
        }
    }
}
```

- [ ] **Step 4: Run test, verify it passes**

Run `tests-run` (EditMode, `LevelProgressionTests`). Expected: 4 PASS.

- [ ] **Step 5: Commit**
```bash
git add "Assets/_Game/Scripts/Progression/LevelProgression.cs" "Assets/_Game/Tests/EditMode/LevelProgressionTests.cs"
git commit -m "feat: add LevelProgression XP curve with tests"
```

---

## Task 3: ObjectPool<T> (pure C#, TDD)

**Files:**
- Create: `Assets/_Game/Scripts/Core/ObjectPool.cs`
- Test: `Assets/_Game/Tests/EditMode/ObjectPoolTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/_Game/Tests/EditMode/ObjectPoolTests.cs`:
```csharp
using NUnit.Framework;
using DinnerRush;

namespace DinnerRush.Tests
{
    public class ObjectPoolTests
    {
        private class Dummy { }

        [Test]
        public void Get_CreatesWhenEmpty()
        {
            int created = 0;
            var pool = new ObjectPool<Dummy>(() => { created++; return new Dummy(); });
            var a = pool.Get();
            Assert.IsNotNull(a);
            Assert.AreEqual(1, created);
        }

        [Test]
        public void ReturnThenGet_ReusesInstance()
        {
            int created = 0;
            var pool = new ObjectPool<Dummy>(() => { created++; return new Dummy(); });
            var a = pool.Get();
            pool.Return(a);
            var b = pool.Get();
            Assert.AreSame(a, b);
            Assert.AreEqual(1, created); // no new allocation
        }

        [Test]
        public void Callbacks_FireOnGetAndReturn()
        {
            int gets = 0, returns = 0;
            var pool = new ObjectPool<Dummy>(
                () => new Dummy(),
                onGet: _ => gets++,
                onReturn: _ => returns++);
            var a = pool.Get();
            pool.Return(a);
            Assert.AreEqual(1, gets);
            Assert.AreEqual(1, returns);
        }
    }
}
```

- [ ] **Step 2: Run test, verify it fails**

Run `tests-run` (EditMode, `ObjectPoolTests`). Expected: FAIL — type does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `Assets/_Game/Scripts/Core/ObjectPool.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace DinnerRush
{
    /// <summary>
    /// Generic reuse pool. Factory creates instances on demand; returned
    /// instances are reused. Optional onGet/onReturn hooks (e.g. enable/disable).
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onReturn = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onReturn = onReturn;
        }

        public int AvailableCount => _available.Count;

        public T Get()
        {
            T item = _available.Count > 0 ? _available.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Return(T item)
        {
            _onReturn?.Invoke(item);
            _available.Push(item);
        }
    }
}
```

- [ ] **Step 4: Run test, verify it passes**

Run `tests-run` (EditMode, `ObjectPoolTests`). Expected: 3 PASS.

- [ ] **Step 5: Commit**
```bash
git add "Assets/_Game/Scripts/Core/ObjectPool.cs" "Assets/_Game/Tests/EditMode/ObjectPoolTests.cs"
git commit -m "feat: add generic ObjectPool with tests"
```

---

## Task 4: Player scene, movement, and camera follow (Play-mode verify)

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerMovement.cs`
- Create: `Assets/_Game/Scripts/Core/CameraFollow.cs`
- Create scene: `Assets/_Game/Scenes/Game.unity`
- Create prefab: `Assets/_Game/Prefabs/Player.prefab`

- [ ] **Step 1: Write PlayerMovement**

Create `Assets/_Game/Scripts/Player/PlayerMovement.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinnerRush
{
    /// <summary>Moves the player Rigidbody2D from the Input System "Move" action.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;

        private Rigidbody2D _rb;
        private Vector2 _input;

        // Injected by the player root so movement reads live stats.
        public PlayerStats Stats { get; set; } = new PlayerStats();

        private void Awake() => _rb = GetComponent<Rigidbody2D>();
        private void OnEnable() => moveAction.action.Enable();
        private void OnDisable() => moveAction.action.Disable();

        private void Update() => _input = moveAction.action.ReadValue<Vector2>();

        private void FixedUpdate()
        {
            _rb.MovePosition(_rb.position + _input.normalized * Stats.MoveSpeed * Time.fixedDeltaTime);
        }
    }
}
```

- [ ] **Step 2: Write CameraFollow**

Create `Assets/_Game/Scripts/Core/CameraFollow.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Smoothly keeps the camera centered on a target (the player).</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 _velocity;

        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, smoothTime);
        }
    }
}
```

- [ ] **Step 3: Refresh & check compile**

Run `assets-refresh`, then `console-get-logs` (Error filter). Expected: no errors.

- [ ] **Step 4: Build the scene**

Using `scene-create` make `Assets/_Game/Scenes/Game.unity`, then:
- Ensure `Main Camera` is Orthographic, size ~5, and add `CameraFollow`.
- Create a `Player` GameObject: add `SpriteRenderer` (use a built-in white square sprite as placeholder), `Rigidbody2D` (GravityScale 0, Freeze Rotation Z, CollisionDetection Continuous), `CircleCollider2D`, and `PlayerMovement`. Set its layer to `Player`.
- On `PlayerMovement.moveAction`, assign the `Player/Move` action from `Assets/InputSystem_Actions`.
- Point `CameraFollow.target` at the Player.
- Save the scene (`scene-save`) and save the Player as a prefab at `Assets/_Game/Prefabs/Player.prefab` (`assets-prefab-create`).

- [ ] **Step 5: Play-mode verification**

Enter Play mode (`editor-application-set-state`). Press/hold WASD (or use `screenshot-game-view` before/after nudging the player via `gameobject-modify` position for an automated check). Expected: player moves smoothly in all directions; camera follows. Exit Play mode.

- [ ] **Step 6: Commit**
```bash
git add "Assets/_Game/Scripts/Player/PlayerMovement.cs" "Assets/_Game/Scripts/Core/CameraFollow.cs" "Assets/_Game/Scenes/Game.unity"* "Assets/_Game/Prefabs/Player.prefab"*
git commit -m "feat: player movement, camera follow, and Game scene"
```

---

## Task 5: Knife weapon + projectile + pool wrapper (Play-mode verify)

**Files:**
- Create: `Assets/_Game/Scripts/Weapons/Projectile.cs`
- Create: `Assets/_Game/Scripts/Weapons/KnifeWeapon.cs`
- Create: `Assets/_Game/Scripts/Enemies/IDamageable.cs`
- Create prefab: `Assets/_Game/Prefabs/Knife.prefab`

- [ ] **Step 1: Define the damage interface**

Create `Assets/_Game/Scripts/Enemies/IDamageable.cs`:
```csharp
namespace DinnerRush
{
    /// <summary>Anything a projectile or contact can damage.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
```

- [ ] **Step 2: Write Projectile**

Create `Assets/_Game/Scripts/Weapons/Projectile.cs`:
```csharp
using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Flies straight, damages the first IDamageable it hits, then despawns.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 3f;

        private float _damage;
        private float _age;
        private Action<Projectile> _despawn;

        public void Launch(Vector2 direction, float damage, Action<Projectile> despawn)
        {
            _damage = damage;
            _despawn = despawn;
            _age = 0f;
            transform.right = direction; // orient sprite along travel
            GetComponent<Rigidbody2D>().velocity = direction.normalized * speed;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifetime) Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var d))
            {
                d.TakeDamage(_damage);
                Despawn();
            }
        }

        private void Despawn()
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            _despawn?.Invoke(this);
        }
    }
}
```

- [ ] **Step 3: Write KnifeWeapon**

Create `Assets/_Game/Scripts/Weapons/KnifeWeapon.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Auto-fires knives at the nearest enemy on a cadence set by PlayerStats.AttackRate.
    /// Pools projectiles via ObjectPool.
    /// </summary>
    public class KnifeWeapon : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float targetRange = 8f;
        [SerializeField] private LayerMask enemyMask;

        public PlayerStats Stats { get; set; } = new PlayerStats();

        private ObjectPool<Projectile> _pool;
        private float _cooldown;

        private void Awake()
        {
            _pool = new ObjectPool<Projectile>(
                factory: () => Instantiate(projectilePrefab),
                onGet: p => p.gameObject.SetActive(true),
                onReturn: p => p.gameObject.SetActive(false));
        }

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            Transform target = FindNearestEnemy();
            if (target == null) return;

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Projectile p = _pool.Get();
            p.transform.position = transform.position;
            p.Launch(dir, Stats.Damage, proj => _pool.Return(proj));

            _cooldown = 1f / Mathf.Max(0.01f, Stats.AttackRate);
        }

        private Transform FindNearestEnemy()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, targetRange, enemyMask);
            Transform nearest = null;
            float best = float.MaxValue;
            foreach (var h in hits)
            {
                float d = ((Vector2)h.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = h.transform; }
            }
            return nearest;
        }
    }
}
```

- [ ] **Step 4: Refresh & check compile**

Run `assets-refresh`, then `console-get-logs` (Error). Expected: no errors.

- [ ] **Step 5: Build the Knife prefab & attach weapon**

- Create `Assets/_Game/Prefabs/Knife.prefab`: `SpriteRenderer` (thin rectangle placeholder), `Rigidbody2D` (GravityScale 0, IsKinematic OFF but no gravity), `BoxCollider2D` with **Is Trigger = ON**, `Projectile`. Layer = `Projectile`.
- Add `KnifeWeapon` to the Player prefab. Assign `projectilePrefab = Knife`, `enemyMask = Enemy`.

- [ ] **Step 6: Play-mode verification (temporary dummy target)**

Place a temporary GameObject on the `Enemy` layer with a `CircleCollider2D` near the player. Enter Play mode. Expected: knives spawn on a ~1/sec cadence, fly toward the dummy, and despawn on contact. Remove the dummy after verifying. Exit Play mode.

- [ ] **Step 7: Commit**
```bash
git add "Assets/_Game/Scripts/Weapons" "Assets/_Game/Scripts/Enemies/IDamageable.cs" "Assets/_Game/Prefabs/Knife.prefab"* "Assets/_Game/Prefabs/Player.prefab"*
git commit -m "feat: knife auto-attack weapon, pooled projectiles, IDamageable"
```

---

## Task 6: Enemies — spawner, chase, health (Play-mode verify)

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/EnemyHealth.cs`
- Create: `Assets/_Game/Scripts/Enemies/EnemyMovement.cs`
- Create: `Assets/_Game/Scripts/Enemies/EnemySpawner.cs`
- Create prefab: `Assets/_Game/Prefabs/Visitor.prefab`

- [ ] **Step 1: Write EnemyHealth**

Create `Assets/_Game/Scripts/Enemies/EnemyHealth.cs`:
```csharp
using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Enemy HP. Raises OnDied(position) when it dies so drops can spawn.</summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 20f;

        private float _health;

        /// <summary>Invoked with the death position. Spawner/drops subscribe.</summary>
        public event Action<Vector3> OnDied;

        private void OnEnable() => _health = maxHealth;

        public void TakeDamage(float amount)
        {
            _health -= amount;
            if (_health <= 0f)
            {
                OnDied?.Invoke(transform.position);
                gameObject.SetActive(false);
            }
        }
    }
}
```

- [ ] **Step 2: Write EnemyMovement**

Create `Assets/_Game/Scripts/Enemies/EnemyMovement.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Steers the enemy Rigidbody2D straight toward the player each physics step.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 2.2f;

        private Rigidbody2D _rb;
        private Transform _target;

        public void SetTarget(Transform t) => _target = t;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void FixedUpdate()
        {
            if (_target == null) return;
            Vector2 dir = ((Vector2)_target.position - _rb.position).normalized;
            _rb.MovePosition(_rb.position + dir * speed * Time.fixedDeltaTime);
        }
    }
}
```

- [ ] **Step 3: Write EnemySpawner**

Create `Assets/_Game/Scripts/Enemies/EnemySpawner.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Spawns visitors on a ring around the player. Spawn interval shrinks over time
    /// to ramp difficulty. Pools enemies via ObjectPool.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float startInterval = 1.5f;
        [SerializeField] private float minInterval = 0.35f;
        [SerializeField] private float rampSeconds = 120f;

        private ObjectPool<EnemyHealth> _pool;
        private float _timer;
        private float _elapsed;

        private void Awake()
        {
            _pool = new ObjectPool<EnemyHealth>(
                factory: CreateEnemy,
                onGet: e => e.gameObject.SetActive(true),
                onReturn: e => e.gameObject.SetActive(false));
        }

        private EnemyHealth CreateEnemy()
        {
            EnemyHealth e = Instantiate(enemyPrefab);
            e.OnDied += _ => _pool.Return(e);
            return e;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            Spawn();

            float t = Mathf.Clamp01(_elapsed / rampSeconds);
            _timer = Mathf.Lerp(startInterval, minInterval, t);
        }

        private void Spawn()
        {
            if (player == null) return;
            Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
            EnemyHealth e = _pool.Get();
            e.transform.position = (Vector2)player.position + offset;
            if (e.TryGetComponent<EnemyMovement>(out var mv)) mv.SetTarget(player);
        }
    }
}
```

- [ ] **Step 4: Refresh & check compile**

Run `assets-refresh`, then `console-get-logs` (Error). Expected: no errors.

- [ ] **Step 5: Build the Visitor prefab & spawner**

- Create `Assets/_Game/Prefabs/Visitor.prefab`: `SpriteRenderer` (colored circle placeholder), `Rigidbody2D` (GravityScale 0, Freeze Rotation Z), `CircleCollider2D` (solid, not trigger), `EnemyHealth`, `EnemyMovement`. Layer = `Enemy`.
- Add an `EnemySpawner` GameObject to the scene; assign `enemyPrefab = Visitor`, `player = Player`.

- [ ] **Step 6: Play-mode verification**

Enter Play mode. Expected: visitors spawn around the player, walk toward it, and die (deactivate) after a couple of knife hits; killed enemies are reused from the pool (enemy count stays bounded). Exit Play mode.

- [ ] **Step 7: Commit**
```bash
git add "Assets/_Game/Scripts/Enemies" "Assets/_Game/Prefabs/Visitor.prefab"* "Assets/_Game/Scenes/Game.unity"*
git commit -m "feat: enemy spawner, chase movement, and health"
```

---

## Task 7: Player health + contact damage + game over (Play-mode verify)

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerHealth.cs`
- Create: `Assets/_Game/Scripts/Enemies/ContactDamage.cs`
- Create: `Assets/_Game/Scripts/Core/GameManager.cs`
- Create: `Assets/_Game/Scripts/UI/GameOverPanel.cs`

- [ ] **Step 1: Write PlayerHealth**

Create `Assets/_Game/Scripts/Player/PlayerHealth.cs`:
```csharp
using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Player HP. Reads MaxHP from PlayerStats; raises OnDied and OnChanged.</summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public PlayerStats Stats { get; set; } = new PlayerStats();

        private float _current;
        private bool _dead;

        /// <summary>(current, max) whenever health changes.</summary>
        public event Action<float, float> OnChanged;
        public event Action OnDied;

        private void Start()
        {
            _current = Stats.MaxHP;
            OnChanged?.Invoke(_current, Stats.MaxHP);
        }

        public void TakeDamage(float amount)
        {
            if (_dead) return;
            _current = Mathf.Max(0f, _current - amount);
            OnChanged?.Invoke(_current, Stats.MaxHP);
            if (_current <= 0f)
            {
                _dead = true;
                OnDied?.Invoke();
            }
        }
    }
}
```

- [ ] **Step 2: Write ContactDamage**

Create `Assets/_Game/Scripts/Enemies/ContactDamage.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Damages any IDamageable it stays in contact with, on a cooldown.</summary>
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] private float damage = 8f;
        [SerializeField] private float interval = 1f;

        private float _cooldown;

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_cooldown > 0f) return;
            if (collision.collider.TryGetComponent<IDamageable>(out var d))
            {
                d.TakeDamage(damage);
                _cooldown = interval;
            }
        }
    }
}
```

- [ ] **Step 3: Write GameManager**

Create `Assets/_Game/Scripts/Core/GameManager.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    public enum GameState { Playing, LevelUp, GameOver }

    /// <summary>Owns game state and the survival timer. Central coordination point.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private GameOverPanel gameOverPanel;

        public GameState State { get; private set; } = GameState.Playing;
        public float SurvivalTime { get; private set; }

        private void Awake() => Instance = this;

        private void Start()
        {
            if (playerHealth != null) playerHealth.OnDied += HandlePlayerDied;
        }

        private void Update()
        {
            if (State == GameState.Playing) SurvivalTime += Time.deltaTime;
        }

        public void SetState(GameState state)
        {
            State = state;
            Time.timeScale = state == GameState.Playing ? 1f : 0f;
        }

        private void HandlePlayerDied()
        {
            SetState(GameState.GameOver);
            if (gameOverPanel != null) gameOverPanel.Show(SurvivalTime);
        }
    }
}
```

- [ ] **Step 4: Write GameOverPanel**

Create `Assets/_Game/Scripts/UI/GameOverPanel.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DinnerRush
{
    /// <summary>Shows the survival time and a restart button on death.</summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
        }

        public void Show(float survivalTime)
        {
            if (root != null) root.SetActive(true);
            if (timeLabel != null)
                timeLabel.text = $"Survived {Mathf.FloorToInt(survivalTime / 60f):00}:{Mathf.FloorToInt(survivalTime % 60f):00}";
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
```
> If `TMPro` is not yet imported, install TextMeshPro Essentials when Unity prompts, or swap `TMP_Text` for `UnityEngine.UI.Text`. Add `Unity.TextMeshPro` to the `DinnerRush.asmdef` references.

- [ ] **Step 5: Refresh & check compile**

Run `assets-refresh`, then `console-get-logs` (Error). Expected: no errors.

- [ ] **Step 6: Wire it up**

- Add `ContactDamage` to the Visitor prefab (`damage = 8`, `interval = 1`).
- Add `PlayerHealth` to the Player prefab.
- Add a `GameManager` GameObject; assign `playerHealth`.
- Build a Canvas + Game Over panel (hidden root, a time label, a Restart button) using the GUI Pro pack; add `GameOverPanel`, wire `root`/`timeLabel`/`restartButton`, and assign it on `GameManager`.

- [ ] **Step 7: Play-mode verification**

Enter Play mode; let visitors touch the player. Expected: HP drops on contact (once per second per enemy), and at 0 HP the game freezes and the Game Over panel shows the survival time; Restart reloads the run. Exit Play mode.

- [ ] **Step 8: Commit**
```bash
git add "Assets/_Game/Scripts/Player/PlayerHealth.cs" "Assets/_Game/Scripts/Enemies/ContactDamage.cs" "Assets/_Game/Scripts/Core/GameManager.cs" "Assets/_Game/Scripts/UI/GameOverPanel.cs" "Assets/_Game/Prefabs" "Assets/_Game/Scenes/Game.unity"*
git commit -m "feat: player health, contact damage, game over flow"
```

---

## Task 8: XP gems + PlayerExperience + XP HUD (Play-mode verify)

**Files:**
- Create: `Assets/_Game/Scripts/Pickups/XpGem.cs`
- Create: `Assets/_Game/Scripts/Player/PlayerExperience.cs`
- Create: `Assets/_Game/Scripts/UI/HUDController.cs`
- Create prefab: `Assets/_Game/Prefabs/XpGem.prefab`

- [ ] **Step 1: Write PlayerExperience**

Create `Assets/_Game/Scripts/Player/PlayerExperience.cs`:
```csharp
using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>MonoBehaviour wrapper around LevelProgression. Raises UI + level-up events.</summary>
    public class PlayerExperience : MonoBehaviour
    {
        private readonly LevelProgression _progression = new LevelProgression();

        /// <summary>(currentXp, xpForNextLevel, level) whenever XP changes.</summary>
        public event Action<int, int, int> OnXpChanged;
        /// <summary>Fires once per level gained, passing the new level.</summary>
        public event Action<int> OnLevelUp;

        public int Level => _progression.Level;

        private void Start() => RaiseChanged();

        public void AddXp(int amount)
        {
            int levels = _progression.AddXp(amount);
            RaiseChanged();
            for (int i = 0; i < levels; i++) OnLevelUp?.Invoke(_progression.Level);
        }

        private void RaiseChanged()
            => OnXpChanged?.Invoke(_progression.CurrentXp, _progression.XpForNextLevel, _progression.Level);
    }
}
```

- [ ] **Step 2: Write XpGem**

Create `Assets/_Game/Scripts/Pickups/XpGem.cs`:
```csharp
using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Idle until the player is within pickupRadius, then homes in. On contact,
    /// grants XP and despawns (via the despawn callback supplied by its spawner).
    /// </summary>
    public class XpGem : MonoBehaviour
    {
        [SerializeField] private int xpValue = 1;
        [SerializeField] private float homingSpeed = 9f;

        private Transform _player;
        private PlayerExperience _experience;
        private PlayerStats _stats;
        private Action<XpGem> _despawn;
        private bool _homing;

        public void Init(Transform player, PlayerExperience xp, PlayerStats stats, Action<XpGem> despawn)
        {
            _player = player;
            _experience = xp;
            _stats = stats;
            _despawn = despawn;
            _homing = false;
        }

        private void Update()
        {
            if (_player == null) return;
            float dist = Vector2.Distance(transform.position, _player.position);
            if (!_homing && dist <= _stats.PickupRadius) _homing = true;

            if (_homing)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, _player.position, homingSpeed * Time.deltaTime);
                if (dist <= 0.25f) Collect();
            }
        }

        private void Collect()
        {
            _experience.AddXp(xpValue);
            _despawn?.Invoke(this);
        }
    }
}
```

- [ ] **Step 3: Write HUDController**

Create `Assets/_Game/Scripts/UI/HUDController.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DinnerRush
{
    /// <summary>Binds player events to the on-screen bars/labels.</summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerExperience experience;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image xpFill;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text timerLabel;

        private void OnEnable()
        {
            if (health != null) health.OnChanged += UpdateHealth;
            if (experience != null) experience.OnXpChanged += UpdateXp;
        }

        private void OnDisable()
        {
            if (health != null) health.OnChanged -= UpdateHealth;
            if (experience != null) experience.OnXpChanged -= UpdateXp;
        }

        private void Update()
        {
            if (timerLabel != null && GameManager.Instance != null)
            {
                float t = GameManager.Instance.SurvivalTime;
                timerLabel.text = $"{Mathf.FloorToInt(t / 60f):00}:{Mathf.FloorToInt(t % 60f):00}";
            }
        }

        private void UpdateHealth(float current, float max)
        {
            if (healthFill != null) healthFill.fillAmount = max <= 0f ? 0f : current / max;
        }

        private void UpdateXp(int current, int forNext, int level)
        {
            if (xpFill != null) xpFill.fillAmount = forNext <= 0 ? 0f : (float)current / forNext;
            if (levelLabel != null) levelLabel.text = $"Lv {level}";
        }
    }
}
```

- [ ] **Step 4: Spawn gems on enemy death**

Modify `Assets/_Game/Scripts/Enemies/EnemySpawner.cs` to also own an XP-gem pool and drop a gem where an enemy dies. Add these members and update `CreateEnemy`:
```csharp
        [SerializeField] private XpGem gemPrefab;
        [SerializeField] private PlayerExperience experience;

        private ObjectPool<XpGem> _gemPool;

        // in Awake(), after _pool is created:
        _gemPool = new ObjectPool<XpGem>(
            factory: () => Instantiate(gemPrefab),
            onGet: g => g.gameObject.SetActive(true),
            onReturn: g => g.gameObject.SetActive(false));

        // replace CreateEnemy body with:
        private EnemyHealth CreateEnemy()
        {
            EnemyHealth e = Instantiate(enemyPrefab);
            e.OnDied += pos =>
            {
                _pool.Return(e);
                XpGem gem = _gemPool.Get();
                gem.transform.position = pos;
                gem.Init(player, experience, _stats, g => _gemPool.Return(g));
            };
            return e;
        }
```
Also add a serialized `PlayerStats` bridge. Since `PlayerStats` is not a MonoBehaviour, expose it from the player root (see Task 9's `PlayerRoot`) and assign it here via `[SerializeField] private PlayerRoot playerRoot;` then use `_stats = playerRoot.Stats;` in `Awake`. **If Task 9 is not yet done, temporarily use `_stats = new PlayerStats();`** and re-point it in Task 9.

- [ ] **Step 5: Refresh & check compile**

Run `assets-refresh`, then `console-get-logs` (Error). Expected: no errors.

- [ ] **Step 6: Build gem prefab + HUD, wire up**

- Create `Assets/_Game/Prefabs/XpGem.prefab`: `SpriteRenderer` (small green diamond placeholder), `XpGem`. Layer = `Pickup`. (No collider needed — pickup is distance-based.)
- Add `PlayerExperience` to the Player prefab.
- On the `EnemySpawner`, assign `gemPrefab`, `experience`.
- Build the HUD (health bar, XP bar, level label, timer label) on the Canvas with the GUI Pro pack; add `HUDController`, wire all fields.

- [ ] **Step 7: Play-mode verification**

Enter Play mode. Expected: killing visitors drops gems; walking near a gem pulls it in; the XP bar fills, the level label increments at thresholds, and the timer counts up. Exit Play mode.

- [ ] **Step 8: Commit**
```bash
git add "Assets/_Game/Scripts/Pickups/XpGem.cs" "Assets/_Game/Scripts/Player/PlayerExperience.cs" "Assets/_Game/Scripts/UI/HUDController.cs" "Assets/_Game/Scripts/Enemies/EnemySpawner.cs" "Assets/_Game/Prefabs" "Assets/_Game/Scenes/Game.unity"*
git commit -m "feat: xp gems, player experience, and HUD bars"
```

---

## Task 9: Player root wiring + upgrades + level-up panel (Play-mode verify)

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerRoot.cs`
- Create: `Assets/_Game/Scripts/Progression/UpgradeDefinition.cs`
- Create: `Assets/_Game/Scripts/Progression/UpgradeManager.cs`
- Create: `Assets/_Game/Scripts/UI/LevelUpPanel.cs`
- Create: `Assets/_Game/Scripts/UI/UpgradeCard.cs`
- Create ScriptableObject assets under `Assets/_Game/ScriptableObjects/Upgrades/`

- [ ] **Step 1: Write PlayerRoot (single stats owner + injection)**

Create `Assets/_Game/Scripts/Player/PlayerRoot.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Owns the single PlayerStats instance and injects it into every player system,
    /// so movement, weapon, and health all read the same live numbers.
    /// </summary>
    public class PlayerRoot : MonoBehaviour
    {
        public PlayerStats Stats { get; } = new PlayerStats();

        private void Awake()
        {
            if (TryGetComponent<PlayerMovement>(out var move)) move.Stats = Stats;
            if (TryGetComponent<PlayerHealth>(out var hp)) hp.Stats = Stats;
            if (TryGetComponent<KnifeWeapon>(out var weapon)) weapon.Stats = Stats;
        }
    }
}
```
> After adding this, update `EnemySpawner` (Task 8 Step 4) to set `_stats = playerRoot.Stats;` from a serialized `PlayerRoot playerRoot;` and assign it in the scene.

- [ ] **Step 2: Write UpgradeDefinition**

Create `Assets/_Game/Scripts/Progression/UpgradeDefinition.cs`:
```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Data for one pickable upgrade: which stat it boosts and by how much.</summary>
    [CreateAssetMenu(menuName = "Dinner Rush/Upgrade", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string title;
        [TextArea] public string description;
        public Sprite icon;
        public StatType stat;
        public float amount;

        public void Apply(PlayerStats stats) => stats.AddModifier(stat, amount);
    }
}
```

- [ ] **Step 3: Write UpgradeCard (one button)**

Create `Assets/_Game/Scripts/UI/UpgradeCard.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DinnerRush
{
    /// <summary>A single clickable upgrade choice in the level-up panel.</summary>
    public class UpgradeCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image icon;
        [SerializeField] private Button button;

        public void Bind(UpgradeDefinition def, Action<UpgradeDefinition> onChosen)
        {
            if (title != null) title.text = def.title;
            if (description != null) description.text = def.description;
            if (icon != null && def.icon != null) icon.sprite = def.icon;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onChosen(def));
        }
    }
}
```

- [ ] **Step 4: Write LevelUpPanel**

Create `Assets/_Game/Scripts/UI/LevelUpPanel.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>Shows up to 3 upgrade cards and reports the chosen one.</summary>
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private UpgradeCard[] cards; // length 3

        private Action<UpgradeDefinition> _onChosen;

        private void Awake() { if (root != null) root.SetActive(false); }

        public void Show(IReadOnlyList<UpgradeDefinition> choices, Action<UpgradeDefinition> onChosen)
        {
            _onChosen = onChosen;
            if (root != null) root.SetActive(true);
            for (int i = 0; i < cards.Length; i++)
            {
                bool has = i < choices.Count;
                cards[i].gameObject.SetActive(has);
                if (has) cards[i].Bind(choices[i], Choose);
            }
        }

        private void Choose(UpgradeDefinition def)
        {
            if (root != null) root.SetActive(false);
            _onChosen?.Invoke(def);
        }
    }
}
```

- [ ] **Step 5: Write UpgradeManager**

Create `Assets/_Game/Scripts/Progression/UpgradeManager.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// On level-up: pauses via GameManager, rolls 3 distinct upgrades, shows the panel,
    /// applies the chosen upgrade to PlayerStats, and resumes.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private PlayerRoot player;
        [SerializeField] private PlayerExperience experience;
        [SerializeField] private LevelUpPanel panel;
        [SerializeField] private List<UpgradeDefinition> pool = new List<UpgradeDefinition>();

        private void OnEnable() { if (experience != null) experience.OnLevelUp += HandleLevelUp; }
        private void OnDisable() { if (experience != null) experience.OnLevelUp -= HandleLevelUp; }

        private void HandleLevelUp(int newLevel)
        {
            GameManager.Instance?.SetState(GameState.LevelUp);
            panel.Show(RollChoices(3), Apply);
        }

        private List<UpgradeDefinition> RollChoices(int count)
        {
            var bag = new List<UpgradeDefinition>(pool);
            var picks = new List<UpgradeDefinition>();
            for (int i = 0; i < count && bag.Count > 0; i++)
            {
                int idx = Random.Range(0, bag.Count);
                picks.Add(bag[idx]);
                bag.RemoveAt(idx);
            }
            return picks;
        }

        private void Apply(UpgradeDefinition def)
        {
            def.Apply(player.Stats);
            GameManager.Instance?.SetState(GameState.Playing);
        }
    }
}
```

- [ ] **Step 6: Refresh & check compile**

Run `assets-refresh`, then `console-get-logs` (Error). Expected: no errors.

- [ ] **Step 7: Create upgrade assets**

Create at least 5 `UpgradeDefinition` assets in `Assets/_Game/ScriptableObjects/Upgrades/` via the Create menu (Dinner Rush → Upgrade), e.g.:
- "Sharper Knives" — `Damage +5`
- "Fast Hands" — `AttackRate +0.3`
- "Quick Feet" — `MoveSpeed +1`
- "Thick Apron" — `MaxHP +25`
- "Big Pockets" — `PickupRadius +0.5`

- [ ] **Step 8: Wire it up**

- Add `PlayerRoot` to the Player prefab (with `PlayerMovement`, `PlayerHealth`, `KnifeWeapon`, `PlayerExperience`).
- Update `EnemySpawner.playerRoot` reference and set `_stats = playerRoot.Stats;` (from Step 1 note).
- Build the Level-Up panel on the Canvas (3 `UpgradeCard`s) using the GUI Pro pack.
- Add an `UpgradeManager`; assign `player`, `experience`, `panel`, and the 5 upgrades into `pool`.

- [ ] **Step 9: Play-mode verification**

Enter Play mode; collect XP to level up. Expected: game pauses, 3 distinct upgrade cards appear; picking one applies its effect (e.g. more damage / faster fire visibly) and unpauses. Exit Play mode.

- [ ] **Step 10: Commit**
```bash
git add "Assets/_Game/Scripts/Player/PlayerRoot.cs" "Assets/_Game/Scripts/Progression" "Assets/_Game/Scripts/UI/LevelUpPanel.cs" "Assets/_Game/Scripts/UI/UpgradeCard.cs" "Assets/_Game/Scripts/Enemies/EnemySpawner.cs" "Assets/_Game/ScriptableObjects" "Assets/_Game/Prefabs" "Assets/_Game/Scenes/Game.unity"*
git commit -m "feat: upgrade system, level-up panel, and player stats wiring"
```

---

## Task 10: Full-loop verification & build settings

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset` (add Game scene)

- [ ] **Step 1: Add the scene to build settings**

Add `Assets/_Game/Scenes/Game.unity` as build index 0 (so Restart's `LoadScene(buildIndex)` works).

- [ ] **Step 2: Run all EditMode tests**

Run `tests-run` (EditMode, all). Expected: PlayerStats (3), LevelProgression (4), ObjectPool (3) — all PASS.

- [ ] **Step 3: Full playthrough verification**

Enter Play mode and play a full short run. Confirm the whole loop:
- Move with WASD, camera follows.
- Knives auto-fire and kill visitors; enemy/projectile/gem counts stay bounded (pooling works).
- Contact damage lowers HP; HP bar updates.
- Gems collect; XP bar fills; level-up shows 3 upgrades; picking one applies and resumes.
- Dying shows Game Over with survival time; Restart reloads cleanly.

Capture a `screenshot-game-view` for the record. Exit Play mode.

- [ ] **Step 4: Commit**
```bash
git add "ProjectSettings/EditorBuildSettings.asset"
git commit -m "chore: add Game scene to build settings; Milestone 1 complete"
```

---

## Notes for the implementer

- **Meta files:** commit the Unity-generated `.meta` files alongside each asset (the `"...".*` globs above capture them). Never delete a `.meta` for a tracked asset.
- **TMPro:** if TextMeshPro is not imported, Unity will prompt to import "TMP Essentials" the first time — accept it, and add `Unity.TextMeshPro` to `DinnerRush.asmdef` references.
- **Placeholder sprites:** any built-in `Knob`/square sprite or a solid-color 1×1 works; art is swapped later without code changes.
- **Order matters:** Task 9 finalizes the `PlayerStats` injection that Task 8 stubs. If you run out of order, honor the `_stats = new PlayerStats();` temporary note.
