# Food-Heal Pickup — Design + Plan

**Date:** 2026-07-23
**Status:** Approved (user delegated: "make what you want, don't add audio")

## Goal

Add survivability to the new 20-wave mode with an on-theme **healing food pickup**:
enemies occasionally drop a plate of food; the player walks over it (magnet pull like
XP gems) and regains a fraction of max HP. **No audio** (explicit user constraint).

## Behaviour

- On enemy death, a **6%** chance to drop one food plate at the death position.
- The plate sits until the player is within pickup radius, then homes to the player
  (same magnet feel as `XpGem`), and on contact heals **15% of the player's Max HP**
  via `PlayerHealth.Heal`, then returns itself to the pool.
- Purely procedural visual (white round plate + warm food blob + highlight) with a
  pop-in, so no art asset or prefab wiring is required.

## Architecture (mirrors the existing XpGem / GemSpawner / GemDropper trio)

### `FoodPickup.cs` (new — `Assets/_Game/Scripts/Progression/`)
- Like `XpGem` but heals instead of granting XP.
- `Init(Transform player, PlayerHealth health, PlayerStats stats, Action<FoodPickup> despawn)`.
- `Awake` builds the plate visual. `Update`: pop-in ease, then if within
  `stats.PickupRadius` home toward player at `magnetSpeed`; within `collectDistance`,
  `Collect()`.
- `Collect()`: `health.Heal(healFraction * health.Max)`, then `despawn`.
- Serialized: `healFraction = 0.15f`, `magnetSpeed = 11f`, `collectDistance = 0.4f`.

### `FoodSpawner.cs` (new — `Assets/_Game/Scripts/Progression/`)
- Singleton with a static `Spawn(Vector3 pos)`, exactly like `GemSpawner`, BUT it
  creates pooled `FoodPickup` GameObjects **in code** (`new GameObject` +
  `AddComponent<FoodPickup>`) — no `foodPrefab` field, since `FoodPickup` builds its
  own visual. Resolves `PlayerRoot` lazily for `transform` / `Stats` / `PlayerHealth`.

### `GemDropper.cs` (modify)
- Add `[SerializeField, Range(0,1)] float foodChance = 0.06f;` and one line in `Drop`:
  `if (Random.value < foodChance) FoodSpawner.Spawn(pos);`
- New field takes the code default at runtime (prefab has no serialized value for it).

### Scene (`Game.unity`)
- Add a `FoodSpawner` component to the scene (on the same GameObject that hosts
  `GemSpawner`), so its static `Spawn` has a live instance. Done via `script-execute`
  in edit mode + SaveScene.

## Non-goals
- No new enemy type, no boss, no audio, no art asset, no prefab.
- Not tied to waves specifically (drops from any enemy death) — keeps it simple.

## Verification
- Compile clean via `assets-refresh`.
- Reflection/`script-execute`: spawn a `FoodPickup` near the player in play mode,
  confirm player HP rises after pickup; confirm `FoodSpawner.Spawn` pools/reuses.
- Screenshot the plate visual on the floor.

## Task list (subagent-driven)
1. `FoodPickup.cs` — the pickup (build + magnet + heal). Verify compile.
2. `FoodSpawner.cs` — pooled code-built singleton. Verify compile.
3. `GemDropper.cs` — add `foodChance` drop hook. Verify compile.
4. Scene: add `FoodSpawner` component (script-execute, edit mode). Verify present.
5. Play-mode verify: force a drop near player, confirm HP heals; screenshot the plate.
