# Dinner Rush — Combat Juice Design

**Date:** 2026-07-20
**Status:** Approved, implementing
**Branch:** feature/m1-core-loop

## Goal

Combat currently has zero visual feedback: enemies vanish instantly on death (`SetActive(false)`),
knives disappear on hit, and enemies slide without animation. Add "juice" so fighting feels alive.

## Approach

Hybrid: **code** for effects particle packs can't do (flash, screen shake, hit-stop, enemy wobble);
the **vfx_2 "Casual RPG VFX"** sprite-based particle prefabs for burst effects. Skip vfx_1 (heavy 3D)
and vfx_3 (empty). Reuse the existing `ObjectPool<T>` for VFX pooling to avoid GC spikes.

## Components (each small, single-purpose)

1. **EnemyHealth (extend)** — add `event Action OnDamaged`, raised in `TakeDamage` before the death check.
   Keep the existing `OnDied(Vector3)`.

2. **HitFlash** *(new MonoBehaviour, on enemy)* — subscribes to `OnDamaged`; tints the enemy
   `SpriteRenderer`(s) white for ~0.08s, then restores original color. Coroutine-free (timer in Update).

3. **DeathEffect** *(new MonoBehaviour, on enemy)* — subscribes to `OnDied`; does a fast squash before
   the object deactivates and asks the `VfxSpawner` to play `Poof_generic` at the death position.

4. **Projectile (extend)** — on `OnTriggerEnter2D` hit, ask `VfxSpawner` to play a small
   `Flash_generic`/`Burst_sharp` at the contact point before despawning.

5. **VfxSpawner** *(new, singleton-ish MonoBehaviour)* — `Play(key, position)`; holds a small set of
   referenced VFX prefabs, pools instances via `ObjectPool<T>`, auto-returns them after their duration.

6. **CameraShake** *(new MonoBehaviour, on Main Camera)* — `Shake(strength, duration)`; applies a
   decaying random offset in `LateUpdate` **after** `CameraFollow` positions the camera, so it layers
   cleanly. Fired on enemy kills.

7. **HitStop** *(new static helper + runner)* — `Do(seconds)` dips `Time.timeScale` to ~0.05 for a short
   unscaled-real-time window (~40ms), then restores to 1. Fired on kills for punch.

8. **EnemyAnimator** *(new MonoBehaviour, on enemy)* — squash/tilt wobble driven by movement, same idea as
   `CookAnimator` (detect motion via position delta, sine wobble).

## Data flow

```
Projectile hit ─► EnemyHealth.TakeDamage
                    ├─► OnDamaged  ─► HitFlash (white flash)
                    └─► if dead: OnDied(pos) ─► DeathEffect (squash + VfxSpawner Poof)
                                              ├─► CameraShake.Shake()
                                              └─► HitStop.Do()
Projectile hit point ─► VfxSpawner.Play(impact spark)
Enemy moving ─► EnemyAnimator (wobble)
```

## Tuning (serialized, sensible defaults)

Flash 0.08s / white; death squash ~0.1s; shake strength small (kills only, not every hit — avoid nausea);
hit-stop ~0.04s; wobble amplitude subtle. All inspector-exposed.

## Testing

All components are visual MonoBehaviours → **play-mode verified** (drive/observe in the editor via MCP,
screenshot). No EditMode unit tests (no pure logic added). Confirm no console errors after each step.

## Build order (verify each in play mode before the next)

1. HitFlash  2. DeathEffect + VfxSpawner (Poof)  3. Projectile impact spark
4. EnemyAnimator wobble  5. CameraShake  6. HitStop

## Out of scope

XP/level-up VFX (belongs with the progression task), enemy sprite replacement, sound.
