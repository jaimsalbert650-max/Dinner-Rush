# 20-Wave Survival Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace endless time-ramped spawning with 20 timed waves of a single enemy type; surviving wave 20 shows a YOU WIN screen.

**Architecture:** A new `WaveManager` owns all wave state (number, timer, difficulty curve) and fires `OnWaveChanged` / `OnAllWavesCleared`. `EnemySpawner` is rewritten to just read spawn interval + enemy HP/speed from it and pool one enemy type. HUD gains a `WAVE n/20` readout, the existing elite banner is repurposed into a wave banner, and a `WinPanel` shows on all-waves-cleared.

**Tech Stack:** Unity 2D, C# (Assembly-CSharp, namespace `DinnerRush`), uGUI runtime-built UI, Unity MCP tools (`script-execute`, `assets-refresh`, `gameobject-*`, `screenshot-camera`) for scene wiring + verification. **No unit-test framework** — pure logic is verified via `script-execute` reflection, MonoBehaviours via play-mode + screenshots.

**Spec:** `docs/superpowers/specs/2026-07-23-wave-survival-design.md`

---

## File structure

- **Create** `Assets/_Game/Scripts/Enemies/WaveManager.cs` — wave state + difficulty curve + events.
- **Rewrite** `Assets/_Game/Scripts/Enemies/EnemySpawner.cs` — thin spawner reading `WaveManager`.
- **Modify** `Assets/_Game/Scripts/Enemies/EnemyVisual.cs` — one fixed sprite instead of random.
- **Create** `Assets/_Game/Scripts/UI/WaveBanner.cs` — centre "WAVE n" flash (from `EliteWarning`).
- **Delete** `Assets/_Game/Scripts/UI/EliteWarning.cs` — replaced by `WaveBanner` (its `EnemySpawner.OnElite` source is gone).
- **Modify** `Assets/_Game/Scripts/UI/GameHud.cs` — add `WAVE n / 20` readout.
- **Create** `Assets/_Game/Scripts/UI/WinPanel.cs` — YOU WIN screen on all-waves-cleared.
- **Scene** `Assets/_Game/Scenes/Game.unity` — add `WaveManager`, wire spawner, swap banner component, add `WinPanel`.

---

### Task 1: WaveManager (wave state + difficulty)

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/WaveManager.cs`

- [ ] **Step 1: Write `WaveManager.cs`**

```csharp
using System;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Drives a fixed sequence of timed waves. Owns the current wave number, the per-wave
    /// countdown, and the difficulty curve (enemy HP + spawn interval). The spawner reads its
    /// parameters from here; HUD / win UI subscribe to the static events. Honours StageConfig tiers.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Waves")]
        [SerializeField] private int waveCount = 20;
        [SerializeField] private float waveDuration = 20f;      // seconds per wave

        [Header("Difficulty")]
        [SerializeField] private float baseEnemyHp = 20f;
        [SerializeField] private float hpPerWave = 0.18f;       // +18% enemy HP per wave
        [SerializeField] private float startInterval = 1.2f;    // spawn gap at wave 1
        [SerializeField] private float minInterval = 0.35f;     // spawn gap at last wave
        [SerializeField] private float baseEnemySpeed = 2.2f;

        /// <summary>Raised when a new wave begins (arg = new wave number, 1-based).</summary>
        public static event Action<int> OnWaveChanged;
        /// <summary>Raised once, after the final wave's timer expires.</summary>
        public static event Action OnAllWavesCleared;

        private int _wave;
        private float _waveTimer;
        private bool _running;
        private bool _started;

        private float _diffHp = 1f, _diffSpd = 1f, _diffRate = 1f;   // StageConfig tier multipliers

        public int CurrentWave => _wave;
        public int WaveCount => waveCount;
        public bool Running => _running;
        public float WaveTimeLeft => Mathf.Max(0f, _waveTimer);

        public float CurrentEnemyHp => baseEnemyHp * (1f + (_wave - 1) * hpPerWave) * _diffHp;
        public float EnemySpeed => baseEnemySpeed * _diffSpd;

        public float CurrentSpawnInterval
        {
            get
            {
                float t = waveCount <= 1 ? 1f : (float)(_wave - 1) / (waveCount - 1);
                return Mathf.Lerp(startInterval, minInterval, t) / _diffRate;
            }
        }

        private void Awake()
        {
            var tier = StageConfig.Current;
            _diffHp = tier.hpMult; _diffSpd = tier.spdMult; _diffRate = tier.spawnRateMult;
        }

        private void Update()
        {
            // First frame: kick off wave 1 here (not in Start) so every subscriber's Start() has
            // already run and none miss the initial OnWaveChanged.
            if (!_started)
            {
                _started = true;
                _wave = 1;
                _waveTimer = waveDuration;
                _running = true;
                OnWaveChanged?.Invoke(_wave);
                return;
            }

            if (!_running) return;

            _waveTimer -= Time.deltaTime;
            if (_waveTimer > 0f) return;

            if (_wave >= waveCount)
            {
                _running = false;
                OnAllWavesCleared?.Invoke();
                return;
            }

            _wave++;
            _waveTimer = waveDuration;
            OnWaveChanged?.Invoke(_wave);
        }
    }
}
```

- [ ] **Step 2: Compile**

Use the `assets-refresh` skill (forces recompile). Expected: no compile errors reported for `WaveManager.cs`.

- [ ] **Step 3: Verify the difficulty curves via reflection**

Use the `script-execute` skill with this body (full-code mode). It instantiates a `WaveManager`, drives it frame-by-frame with a fake `deltaTime` is not needed — instead it sets private fields directly to sample the curve at wave 1, 10, and 20:

```csharp
using UnityEngine;
using System.Reflection;
using DinnerRush;

var go = new GameObject("WM_TEST");
var wm = go.AddComponent<WaveManager>();
var t = typeof(WaveManager);
var waveF = t.GetField("_wave", BindingFlags.NonPublic | BindingFlags.Instance);

System.Action<int> setWave = w => waveF.SetValue(wm, w);

setWave(1);
Debug.Log($"W1 hp={wm.CurrentEnemyHp} interval={wm.CurrentSpawnInterval} spd={wm.EnemySpeed}");
setWave(10);
Debug.Log($"W10 hp={wm.CurrentEnemyHp} interval={wm.CurrentSpawnInterval}");
setWave(20);
Debug.Log($"W20 hp={wm.CurrentEnemyHp} interval={wm.CurrentSpawnInterval}");
Debug.Log($"WaveCount={wm.WaveCount}");
Object.DestroyImmediate(go);
```

Expected logs (StageConfig tier 1 = all mults 1.0):
- `W1 hp=20 interval=1.2 spd=2.2`
- `W10 hp≈52.4 interval≈0.75...`  (hp = 20*(1+9*0.18)=52.4)
- `W20 hp≈88.4 interval=0.35`     (hp = 20*(1+19*0.18)=88.4)
- `WaveCount=20`

If numbers differ, fix the formulas before continuing.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/Enemies/WaveManager.cs
git commit -m "feat: WaveManager drives 20 timed waves + difficulty curve"
```

---

### Task 2: Rewrite EnemySpawner as a thin wave-driven spawner

**Files:**
- Rewrite: `Assets/_Game/Scripts/Enemies/EnemySpawner.cs`

- [ ] **Step 1: Replace the whole file**

```csharp
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Spawns a single enemy type on a ring around the player. All wave/difficulty state lives in
    /// WaveManager; this just reads the current spawn interval + enemy HP/speed and pools enemies.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private WaveManager waves;
        [SerializeField] private float spawnRadius = 9f;

        private ObjectPool<EnemyHealth> _pool;
        private float _timer;

        private void Awake()
        {
            _pool = new ObjectPool<EnemyHealth>(
                factory: CreateEnemy,
                onGet: e => e.gameObject.SetActive(true),
                onReturn: e => e.gameObject.SetActive(false));
            if (waves == null) waves = FindObjectOfType<WaveManager>();
        }

        private EnemyHealth CreateEnemy()
        {
            EnemyHealth e = Instantiate(enemyPrefab);
            e.OnDied += _ => _pool.Return(e);
            return e;
        }

        private void Update()
        {
            if (waves == null || !waves.Running) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            Spawn();
            _timer = waves.CurrentSpawnInterval;
        }

        private void Spawn()
        {
            if (player == null || enemyPrefab == null) return;
            Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
            EnemyHealth e = _pool.Get();
            e.transform.position = (Vector2)player.position + offset;

            e.Configure(waves.CurrentEnemyHp);
            if (e.TryGetComponent<EnemyMovement>(out var mv))
            {
                mv.SetTarget(player);
                mv.SetSpeed(waves.EnemySpeed);
            }
        }
    }
}
```

Note: this deletes `SpawnElite`, `OnElite`, the fast/tough roll, `hpRampPerMinute`, all elite fields, and the `EnemyAnimator.SetBaseScaleMult` / `EnemyVisual.SetVariant` calls (enemies now use their default scale and no ring).

- [ ] **Step 2: Compile — EXPECT an error in EliteWarning**

Use `assets-refresh`. Expected: **one** compile error — `EliteWarning.cs` references `EnemySpawner.OnElite`, which no longer exists. This is expected and fixed in Task 4. Do not try to fix it here.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Scripts/Enemies/EnemySpawner.cs
git commit -m "refactor: EnemySpawner reads WaveManager, drops variants/elite"
```

---

### Task 3: EnemyVisual — one fixed sprite

**Files:**
- Modify: `Assets/_Game/Scripts/Enemies/EnemyVisual.cs`

- [ ] **Step 1: Add a `spriteIndex` field**

In `EnemyVisual.cs`, under the existing `[SerializeField] private Color tint = Color.white;` line, add:

```csharp
        [SerializeField, Tooltip("Which customer in the sheet every enemy uses (single enemy type).")]
        private int spriteIndex = 0;
```

- [ ] **Step 2: Use the fixed index instead of a random pick**

In `Awake()`, replace:

```csharp
            if (_sprites != null && _sprites.Length > 0)
                sr.sprite = _sprites[Random.Range(0, _sprites.Length)];
```

with:

```csharp
            if (_sprites != null && _sprites.Length > 0)
                sr.sprite = _sprites[Mathf.Clamp(spriteIndex, 0, _sprites.Length - 1)];
```

- [ ] **Step 3: Compile**

Use `assets-refresh`. Expected: still the single `EliteWarning` error from Task 2, and no new errors in `EnemyVisual.cs`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/Enemies/EnemyVisual.cs
git commit -m "feat: enemies all use one fixed sprite (single type)"
```

---

### Task 4: WaveBanner replaces EliteWarning

**Files:**
- Create: `Assets/_Game/Scripts/UI/WaveBanner.cs`
- Delete: `Assets/_Game/Scripts/UI/EliteWarning.cs`

- [ ] **Step 1: Create `WaveBanner.cs`** (structure copied from `EliteWarning`, now driven by `WaveManager.OnWaveChanged`)

```csharp
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
        private float _t = -1f;

        private void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            _text = UIBuilder.Text("WaveBanner", canvas.transform, "WAVE 1", 48,
                Gold, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.78f));
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
            if (_t < 0f) { if (_text != null && _text.enabled) _text.enabled = false; return; }

            _t += Time.deltaTime;
            float u = _t / Duration;
            if (u >= 1f) { _t = -1f; if (_text != null) _text.enabled = false; return; }

            _text.enabled = true;
            _text.transform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)) * 0.18f);

            float a = u < 0.15f ? u / 0.15f : (u > 0.7f ? (1f - u) / 0.3f : 1f);
            _text.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Clamp01(a));
        }
    }
}
```

- [ ] **Step 2: Delete `EliteWarning.cs`**

Use the `script-delete` skill on `Assets/_Game/Scripts/UI/EliteWarning.cs` (removes the `.cs` and its `.meta`).

- [ ] **Step 3: Compile**

Use `assets-refresh`. Expected: **zero** compile errors now (the `EliteWarning` → `OnElite` error is gone; `WaveBanner` compiles). If a "missing script" or leftover reference to `EliteWarning` appears, it's the scene component — that is swapped in Task 7.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/UI/WaveBanner.cs
git add -A Assets/_Game/Scripts/UI/EliteWarning.cs.meta
git commit -m "feat: WaveBanner (WAVE n flash) replaces EliteWarning"
```

---

### Task 5: HUD — WAVE n / 20 readout

**Files:**
- Modify: `Assets/_Game/Scripts/UI/GameHud.cs`

- [ ] **Step 1: Add fields**

In `GameHud.cs`, add a colour field near the other colours:

```csharp
        [SerializeField] private Color waveColor = new Color(1f, 0.85f, 0.3f);
```

and add to the private fields block (the `private Text _timer, _kills, _level, _coins;` line) a wave text + manager ref:

```csharp
        private Text _wave;
        private WaveManager _waves;
```

- [ ] **Step 2: Find the WaveManager in `Start`**

In `Start()`, after `_xp = FindObjectOfType<PlayerExperience>();` add:

```csharp
            _waves = FindObjectOfType<WaveManager>();
```

- [ ] **Step 3: Build the readout**

In `Build()`, after the `_level = NewText(...)` block, add (sits just under the centre timer):

```csharp
            _wave = NewText("HudWave", canvas.transform, "WAVE 1 / 20", 28, waveColor,
                new Vector2(0.33f, 0.862f), new Vector2(0.67f, 0.898f));
```

- [ ] **Step 4: Update it each frame**

In `Update()`, before the closing brace, add:

```csharp
            if (_wave != null && _waves != null)
                _wave.text = "WAVE " + Mathf.Max(1, _waves.CurrentWave) + " / " + _waves.WaveCount;
```

- [ ] **Step 5: Compile**

Use `assets-refresh`. Expected: zero errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/UI/GameHud.cs
git commit -m "feat: HUD shows WAVE n / 20"
```

---

### Task 6: WinPanel — YOU WIN screen

**Files:**
- Create: `Assets/_Game/Scripts/UI/WinPanel.cs`

- [ ] **Step 1: Create `WinPanel.cs`** (mirrors `GameOverPanel`'s runtime UI + reward banking; green YOU WIN title; triggered by `WaveManager.OnAllWavesCleared`)

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinnerRush
{
    /// <summary>
    /// Shows a "YOU WIN" screen when all waves are cleared: pauses, banks the run rewards (same
    /// scheme as GameOverPanel, plus a win bonus), and offers Restart + Shop. Built at runtime.
    /// </summary>
    public class WinPanel : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private Text _stats;
        private ShopPanel _shop;

        private void Start()
        {
            _shop = FindObjectOfType<ShopPanel>();
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Helvetica", "Verdana" }, 40);
            Build();
            _root.SetActive(false);
            WaveManager.OnAllWavesCleared += Show;
        }

        private void OnDestroy() => WaveManager.OnAllWavesCleared -= Show;

        private void Show()
        {
            Time.timeScale = 0f;
            var gs = GameStats.Instance;
            int kills = gs != null ? gs.Kills : 0;
            float rewardMult = StageConfig.Current.rewardMult;
            var pr = FindObjectOfType<PlayerRoot>();
            float coinMult = pr != null ? pr.Stats.CoinMult : 1f;
            int coins = Mathf.RoundToInt((gs != null ? gs.Coins : 0) * rewardMult * coinMult);
            MetaProgress.AddCoins(coins);
            PlayerPrefs.SetInt("total_kills", PlayerPrefs.GetInt("total_kills", 0) + kills);

            float t = gs != null ? gs.Elapsed : 0f;
            int m = (int)(t / 60f), s = (int)(t % 60f);
            int gems = Mathf.RoundToInt((1 + m) * rewardMult) + 10;   // +10 win bonus
            PlayerGems.Add(gems);

            float best = PlayerPrefs.GetFloat("meta_best_time", 0f);
            bool record = t > best;
            if (record) { PlayerPrefs.SetFloat("meta_best_time", t); PlayerPrefs.Save(); }

            int acctXp = kills * 2 + m * 4 + 50;   // +50 win bonus
            PlayerProfile.AddXp(acctXp);

            if (_stats != null) _stats.text = "All 20 waves cleared!"
                + "\nTime   " + m.ToString("00") + ":" + s.ToString("00")
                + "\nKills   " + kills
                + "\n+" + coins + " coins   +" + gems + " gems   +" + acctXp + " XP";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            var sprite = WhiteSprite();

            _root = NewImage("WinPanel", canvas.transform, sprite, new Color(0.05f, 0.09f, 0.06f, 0.98f)).gameObject;
            Anchor((RectTransform)_root.transform, Vector2.zero, Vector2.one);

            var title = NewText("WinTitle", _root.transform, "YOU WIN", 68, new Color(0.5f, 0.95f, 0.45f));
            Anchor((RectTransform)title.transform, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.76f));

            _stats = NewText("WinStats", _root.transform, "", 34, new Color(0.9f, 0.94f, 0.9f));
            Anchor((RectTransform)_stats.transform, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.60f));

            var btn = NewImage("WinRestartButton", _root.transform, sprite, new Color(0.35f, 0.6f, 0.28f, 1f));
            Anchor(btn.rectTransform, new Vector2(0.28f, 0.30f), new Vector2(0.72f, 0.39f));
            var button = btn.gameObject.AddComponent<Button>();
            button.targetGraphic = btn;
            button.onClick.AddListener(Restart);
            var label = NewText("WinRestartLabel", btn.transform, "PLAY AGAIN", 34, Color.white);
            Anchor((RectTransform)label.transform, Vector2.zero, Vector2.one);
            label.raycastTarget = false;

            var shopBtn = NewImage("WinShopButton", _root.transform, sprite, new Color(0.3f, 0.42f, 0.62f, 1f));
            Anchor(shopBtn.rectTransform, new Vector2(0.28f, 0.19f), new Vector2(0.72f, 0.28f));
            var sBtn = shopBtn.gameObject.AddComponent<Button>();
            sBtn.targetGraphic = shopBtn;
            sBtn.onClick.AddListener(() => { if (_shop != null) _shop.Open(); });
            var sLabel = NewText("WinShopLabel", shopBtn.transform, "SHOP", 34, Color.white);
            Anchor((RectTransform)sLabel.transform, Vector2.zero, Vector2.one);
            sLabel.raycastTarget = false;
        }

        private Image NewImage(string name, Transform parent, Sprite s, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            return img;
        }

        private Text NewText(string name, Transform parent, string content, int size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tx = go.AddComponent<Text>();
            tx.font = _font;
            tx.text = content;
            tx.fontSize = size;
            tx.color = c;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            return tx;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite WhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
```

- [ ] **Step 2: Compile**

Use `assets-refresh`. Expected: zero errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Scripts/UI/WinPanel.cs
git commit -m "feat: YOU WIN screen on all waves cleared"
```

---

### Task 7: Scene wiring (Game.unity)

All edits below run in **edit mode** via the `script-execute` skill. Do NOT save the scene while play mode is running (project gotcha). Save with `EditorSceneManager.MarkSceneDirty` + `SaveScene` in edit mode only.

**Files:**
- Modify: `Assets/_Game/Scenes/Game.unity` (via MCP)

- [ ] **Step 1: Confirm the scene is open**

Use `scene-list-opened`. Expected: `Game` scene listed. If not, use `scene-open` on `Assets/_Game/Scenes/Game.unity`.

- [ ] **Step 2: Find the spawner GameObject + the EliteWarning host**

Use `gameobject-find` for a GameObject with an `EnemySpawner` component (likely named "Spawner" or "EnemySpawner"), and one with an `EliteWarning` component. Record their paths/instanceIds.

- [ ] **Step 3: Add WaveManager to the spawner GameObject and wire the reference + serialized values**

Use `script-execute` (edit mode). This adds `WaveManager` next to `EnemySpawner`, points `EnemySpawner.waves` at it, and sets the tunable difficulty values on the scene component (scene overrides code defaults — set them explicitly):

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using DinnerRush;

var spawner = Object.FindObjectOfType<EnemySpawner>();
if (spawner == null) { Debug.LogError("No EnemySpawner in scene"); return; }
var go = spawner.gameObject;

var wm = go.GetComponent<WaveManager>();
if (wm == null) wm = go.AddComponent<WaveManager>();

BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
void SetF(object o, string n, object v) { var f = o.GetType().GetField(n, F); if (f != null) f.SetValue(o, v); }

SetF(wm, "waveCount", 20);
SetF(wm, "waveDuration", 20f);
SetF(wm, "baseEnemyHp", 20f);
SetF(wm, "hpPerWave", 0.18f);
SetF(wm, "startInterval", 1.2f);
SetF(wm, "minInterval", 0.35f);
SetF(wm, "baseEnemySpeed", 2.2f);

// wire EnemySpawner.waves -> wm
SetF(spawner, "waves", wm);

EditorUtility.SetDirty(spawner);
EditorUtility.SetDirty(wm);
EditorSceneManager.MarkSceneDirty(go.scene);
EditorSceneManager.SaveScene(go.scene);
Debug.Log("WaveManager added + wired. waves ref set = " + (spawner.GetType().GetField("waves", F).GetValue(spawner) != null));
```

Expected log: `WaveManager added + wired. waves ref set = True`.

- [ ] **Step 4: Swap the EliteWarning component for WaveBanner**

Use `script-execute` (edit mode). Removes the now-scriptless `EliteWarning` component and adds `WaveBanner` on the same GameObject:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DinnerRush;

// Find the GameObject that hosted EliteWarning. After deleting EliteWarning.cs the component is a
// "missing script"; find it by checking every MonoBehaviour == null on HUD-ish objects, or just add
// WaveBanner to the same canvas object that has GameHud (a safe, known host).
var hud = Object.FindObjectOfType<GameHud>();
GameObject host = hud != null ? hud.gameObject : Object.FindObjectOfType<Canvas>().gameObject;

if (host.GetComponent<WaveBanner>() == null) host.AddComponent<WaveBanner>();

// Clean up any missing-script components on that host (leftover EliteWarning).
var so = new SerializedObject(host);
int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(host);

EditorUtility.SetDirty(host);
EditorSceneManager.MarkSceneDirty(host.scene);
EditorSceneManager.SaveScene(host.scene);
Debug.Log($"WaveBanner added to {host.name}; missing scripts removed = {removed}");
```

Expected log: `WaveBanner added to <name>; missing scripts removed = 1` (or 0 if EliteWarning lived elsewhere — if 0, re-run targeting the correct host and also remove the missing script there).

- [ ] **Step 5: Add WinPanel to the GameOverPanel host**

Use `script-execute` (edit mode). WinPanel builds its own UI, so it just needs to live under a Canvas — reuse the GameOverPanel's GameObject:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DinnerRush;

var gop = Object.FindObjectOfType<GameOverPanel>();
GameObject host = gop != null ? gop.gameObject : Object.FindObjectOfType<Canvas>().gameObject;
if (host.GetComponent<WinPanel>() == null) host.AddComponent<WinPanel>();

EditorUtility.SetDirty(host);
EditorSceneManager.MarkSceneDirty(host.scene);
EditorSceneManager.SaveScene(host.scene);
Debug.Log("WinPanel added to " + host.name);
```

Expected log: `WinPanel added to <name>`.

- [ ] **Step 6: Commit the scene**

```bash
git add Assets/_Game/Scenes/Game.unity
git commit -m "chore: wire WaveManager, WaveBanner, WinPanel into Game scene"
```

---

### Task 8: Play-mode verification

- [ ] **Step 1: Clear the console**

Use `console-clear-logs`.

- [ ] **Step 2: Enter play mode**

Use `editor-application-set-state` to start playmode. Expected: no exceptions in `console-get-logs`.

- [ ] **Step 3: Verify wave 1 + one enemy type**

Use `screenshot-game-view` (or set the canvas to ScreenSpaceCamera + `screenshot-camera` per the project's HUD-capture note). Expected: `WAVE 1 / 20` in the HUD, a "WAVE 1" banner near the top on start, and every enemy showing the **same** customer picture (no cyan/red rings, no giant elite).

- [ ] **Step 4: Verify wave advance**

Use `script-execute` to fast-forward the wave for verification without waiting 20s — set `_waveTimer` low and read `CurrentWave`:

```csharp
using UnityEngine;
using System.Reflection;
using DinnerRush;

var wm = Object.FindObjectOfType<WaveManager>();
var F = BindingFlags.NonPublic | BindingFlags.Instance;
wm.GetType().GetField("_waveTimer", F).SetValue(wm, 0.05f);
Debug.Log("Forced wave timer low; current wave = " + wm.CurrentWave);
```

Wait a moment (let the editor tick), then `console-get-logs` + a screenshot. Expected: the wave number increments and a new "WAVE n" banner flashes.

- [ ] **Step 5: Verify YOU WIN**

Use `script-execute` to jump to the last wave and expire it:

```csharp
using UnityEngine;
using System.Reflection;
using DinnerRush;

var wm = Object.FindObjectOfType<WaveManager>();
var F = BindingFlags.NonPublic | BindingFlags.Instance;
wm.GetType().GetField("_wave", F).SetValue(wm, wm.WaveCount);      // wave 20
wm.GetType().GetField("_waveTimer", F).SetValue(wm, 0.05f);        // about to expire
Debug.Log("Jumped to final wave; waiting for OnAllWavesCleared...");
```

Let the editor tick, then screenshot. Expected: **YOU WIN** panel appears, game pauses (`Time.timeScale == 0`), stats show "All 20 waves cleared!". Confirm `PLAY AGAIN` and `SHOP` buttons render.

- [ ] **Step 6: Verify death still shows Game Over**

Use `script-execute` to reset timescale and kill the player:

```csharp
using UnityEngine;
using DinnerRush;

Time.timeScale = 1f;
var ph = Object.FindObjectOfType<PlayerHealth>();
if (ph != null) ph.Damage(99999f);
Debug.Log("Player killed for Game Over check");
```

(If `PlayerHealth`'s damage method has a different name, check `PlayerHealth.cs` first.) Screenshot. Expected: the existing **GAME OVER** panel appears.

- [ ] **Step 7: Exit play mode + reset timescale**

Use `editor-application-set-state` to stop playmode. Then `script-execute`: `Time.timeScale = 1f;` (safety — the project has frozen the editor before by leaving timescale at 0).

- [ ] **Step 8: Final commit if any tuning changed**

If play-testing prompted numeric tweaks, set them on the scene `WaveManager` component (edit mode + SaveScene) and:

```bash
git add Assets/_Game/Scenes/Game.unity
git commit -m "tune: wave difficulty after playtest"
```

---

## Self-review notes

- **Spec coverage:** 20 timed waves (Task 1) ✓; one enemy type, no variants/elite (Tasks 2, 3) ✓; same single picture (Task 3) ✓; WAVE n/20 HUD + banner (Tasks 4, 5) ✓; YOU WIN on wave 20 (Task 6) ✓; Game Over unchanged (Task 8 step 6) ✓; StageConfig tiers preserved (Task 1 `Awake`) ✓; EliteWarning neutralized (Task 4) ✓.
- **Type consistency:** `WaveManager` public API (`Running`, `CurrentSpawnInterval`, `CurrentEnemyHp`, `EnemySpeed`, `CurrentWave`, `WaveCount`, `OnWaveChanged`, `OnAllWavesCleared`) is used identically in `EnemySpawner` (Task 2), `WaveBanner` (Task 4), `GameHud` (Task 5), `WinPanel` (Task 6). Private field names (`_wave`, `_waveTimer`) referenced in verification steps match Task 1.
- **Known unknown:** the scene component names/hosts for `EnemySpawner`, `EliteWarning`, `GameOverPanel` are resolved at runtime via `FindObjectOfType` in Task 7 rather than hard-coded, so exact GameObject names don't need to be known in advance.
- **Death-damage method:** Task 8 step 6 assumes `PlayerHealth.Damage(float)`; verify the actual name in `PlayerHealth.cs` before running (non-blocking for the feature).
```
