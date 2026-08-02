# 20-Wave Survival — Design

**Date:** 2026-07-23
**Status:** Approved, ready for planning

## Goal

Replace the current endless, time-ramped enemy spawning with a **structured 20-wave
survival mode using a single enemy type**, in the style of survivor.io timed waves.

- The run is divided into **20 timed waves**. Each wave lasts a fixed duration.
- A banner announces each wave (`WAVE 1` … `WAVE 20`).
- Enemies spawn continuously during a wave; each successive wave is harder
  (faster spawns + more enemy HP).
- **Exactly one enemy type** — identical picture, HP, and speed for every enemy.
  Fast/tough variants and the elite mini-boss are removed.
- Surviving to the end of **Wave 20** shows a **YOU WIN** screen.
- Dying at any point shows the existing **Game Over** screen (unchanged).

## Non-goals

- No per-wave rest/intermission phase (waves flow continuously; difficulty steps
  up at each wave boundary).
- No new enemy art, no boss, no multiple enemy types.
- No changes to weapons, XP, upgrades, coins/gems, or the lobby.
- Audio stays out (previously rejected by the user).

## Architecture

Chosen approach: a dedicated **`WaveManager`** owns all wave state; the spawner and
UI read from it. Each file keeps one clear job.

### 1. `WaveManager.cs` (new — `Assets/_Game/Scripts/Enemies/`)

Single source of truth for wave progression.

- Serialized config: `waveCount = 20`, `waveDuration = 20f` (seconds per wave).
- Difficulty config (tunable): `baseEnemyHp`, `hpPerWave` (~0.18 = +18%/wave),
  `startInterval` (1.2s), `minInterval` (0.35s), `baseEnemySpeed`.
- State: `CurrentWave` (1..20), time remaining in the current wave.
- Per-frame: counts down the wave timer; at 0 advances to the next wave and
  raises `OnWaveChanged`. After the last wave's timer expires, raises
  `OnAllWavesCleared` once and stops.
- Public read API used by the spawner:
  - `float CurrentEnemyHp` → `baseEnemyHp * (1 + (CurrentWave-1) * hpPerWave)`
  - `float CurrentSpawnInterval` → lerp `startInterval → minInterval` across
    waves 1→20
  - `float EnemySpeed` → constant `baseEnemySpeed`
  - `bool Running` → false after all waves cleared (spawner stops spawning)
- Static events for UI (matches the existing `EnemySpawner.OnElite` pattern):
  - `event Action<int> OnWaveChanged` (arg = new wave number)
  - `event Action OnAllWavesCleared`
- Honors `StageConfig.Current` multipliers (hp/spd/spawnRate) the same way the
  old spawner did, so the difficulty tiers keep working.

### 2. `EnemySpawner.cs` (rewrite)

Strips out all variant/elite logic. Becomes a thin spawner driven by `WaveManager`.

- Holds `enemyPrefab`, `player`, `spawnRadius`, and a reference to `WaveManager`
  (found in `Awake` via `FindObjectOfType` if not wired).
- `Update`: if `WaveManager.Running`, count down a local spawn timer using
  `WaveManager.CurrentSpawnInterval`; on tick, `Spawn()`.
- `Spawn()`: get a pooled enemy, place on the ring, `Configure(WaveManager.CurrentEnemyHp)`,
  set movement target + `WaveManager.EnemySpeed`. No variant ring, no scale mult.
- Deleted: `SpawnElite`, `OnElite`, fast/tough roll, `hpRampPerMinute`, all elite fields.

### 3. `EnemyVisual.cs` (small edit)

- Replace the random sprite pick with a **single fixed sprite**. Add a serialized
  `int spriteIndex = 0` (default = first customer) and use `_sprites[spriteIndex]`
  instead of `_sprites[Random.Range(...)]`. Everything else (Body child, ring
  object, HitFlash) stays intact.
- The `SetVariant` method stays but is simply never called now (harmless).

### 4. HUD (`GameHud.cs` edit + a small banner)

- Add a `WAVE n / 20` readout to the top bar (new `Text`, same `NewText` helper).
  Update it from `WaveManager.CurrentWave` each frame, or on `OnWaveChanged`.
- Add a short-lived centered **"WAVE n"** banner shown for ~1.5s on
  `OnWaveChanged` (a `Text` that fades out). Can live in `GameHud` or a tiny
  `WaveBanner.cs`; implementation plan decides which is cleaner.

### 5. `WinPanel.cs` (new — `Assets/_Game/Scripts/UI/`)

- Subscribes to `WaveManager.OnAllWavesCleared`.
- On fire: `Time.timeScale = 0`, shows a full-screen **YOU WIN** panel styled like
  `GameOverPanel` (title, survived-time/kills summary, RESTART + SHOP buttons).
- Reuses the same reward banking as `GameOverPanel.Show` (coins/gems/account XP,
  best-time) so a win still pays out. Factor the shared banking into a small
  helper if it's clean to do so; otherwise duplicate the few lines.

## Data flow

```
WaveManager (timer) ──OnWaveChanged──▶ HUD banner + "WAVE n/20"
      │  Running / CurrentSpawnInterval / CurrentEnemyHp / EnemySpeed
      ▼
EnemySpawner ──▶ pooled Enemy (one type) ──▶ moves at player
      │
WaveManager (wave 20 ends) ──OnAllWavesCleared──▶ WinPanel (YOU WIN, pause)
PlayerHealth.OnDied ──▶ GameOverPanel (unchanged)
```

## Tunable numbers (defaults)

| Field | Default | Effect |
|---|---|---|
| `waveCount` | 20 | number of waves |
| `waveDuration` | 20s | length of each wave (~6:40 total) |
| `baseEnemyHp` | 20 | Wave 1 enemy HP |
| `hpPerWave` | 0.18 | +18% HP per wave (Wave 20 ≈ 4.4×) |
| `startInterval` | 1.2s | spawn gap at Wave 1 |
| `minInterval` | 0.35s | spawn gap at Wave 20 |
| `baseEnemySpeed` | 2.2 | constant enemy speed |

All exposed as `[SerializeField]` for in-editor tuning. **Reminder:** scene/prefab
serialized values override code defaults — set them on the scene component too.

## Testing / verification

- Pure logic in `WaveManager` (wave advance, HP/interval curves, cleared event)
  verified via `script-execute` reflection (no test framework in this project).
- MonoBehaviour behavior verified in play mode: enter play, confirm wave banner
  advances, enemies are one look/stat, and reaching wave 20's end shows YOU WIN.
- Confirm dying still shows Game Over.
- Screenshot the WAVE HUD + a YOU WIN screen.

## Risks / gotchas

- Serialized-field override: after rewriting `EnemySpawner`, its scene component
  may keep stale/removed fields — re-check the scene component and re-wire the
  `WaveManager` reference.
- `EnemySpawner` currently reads `StageConfig` in `Awake`; that logic moves to
  `WaveManager` so difficulty tiers keep applying.
- Keep the `OnElite`/`EliteWarning` UI from erroring now that elites are gone —
  `EliteWarning.cs` subscribes to `EnemySpawner.OnElite`; remove or neutralize it.
