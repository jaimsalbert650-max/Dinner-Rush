# Dinner Rush — Bullet Heaven Roguelike: Milestone 1 Design

**Date:** 2026-07-20
**Status:** Approved (design phase)

## Concept

A 2D top-down roguelike "Bullet Heaven" (Vampire Survivors / Survivor.io style). The
player is a **cook** who auto-throws kitchen items (knives, spatulas, sauces, products)
to fight off waves of **visitors**. Survive as long as possible; level up by collecting
XP; pick upgrades to build power. This document covers **Milestone 1**, the first
playable vertical slice — the complete core loop with one weapon.

Later milestones (out of scope here): more weapons, weapon evolutions, meta-progression,
shop, bosses, audio, real art.

## Decisions

| Question | Decision |
|----------|----------|
| Dimension | 2D top-down (sprites, camera looking straight down) |
| Combat | Auto-attack — player controls movement only (WASD) |
| Input | Existing `Assets/InputSystem_Actions` (new Input System) |
| Art | Placeholder shapes/sprites; swap real art later without code changes |
| Data | ScriptableObjects for upgrades (and future weapons/enemies) |
| Decoupling | C# events + a shared runtime `PlayerStats` object |

## Milestone 1 Scope (the vertical slice)

- Player (cook) moves with WASD; camera follows.
- One weapon: auto-throws knives at the nearest visitor on a timer.
- Visitors spawn at screen edges, chase the player, damage on contact.
- Player has HP; reaching 0 → game-over screen.
- Killed visitors drop XP gems; collecting enough → level up → choose 1 of 3 upgrades.
- Survival timer shown on the HUD.

## Systems & Architecture

### Runtime stats hub
- **`PlayerStats`** — single runtime object holding modifiable numbers: `moveSpeed`,
  `maxHP`, `damage`, `attackRate`, `pickupRadius`. Gameplay systems read from it;
  upgrades write to it. One source of truth.

### Player (components on the cook GameObject)
- **`PlayerMovement`** — reads the Move action, moves the `Rigidbody2D`.
- **`PlayerHealth`** — HP, `TakeDamage()`, fires `OnDeath`.
- **`PlayerExperience`** — accumulates XP, level thresholds, fires `OnLevelUp`.

### Weapons & projectiles
- **`KnifeWeapon`** — timer driven by `attackRate`; finds nearest enemy; fires a projectile.
- **`Projectile`** — flies in a direction, deals `damage` on hit, returns to pool.
- **`ObjectPool`** — reuse for projectiles (avoid Instantiate/Destroy churn).

### Enemies (visitors)
- **`EnemySpawner`** — spawns on a ring around the player; spawn rate ramps over time.
- **`EnemyMovement`** — steers toward the player.
- **`EnemyHealth`** — HP, `TakeDamage()`, on death spawns an XP gem and returns to pool.
- **`ContactDamage`** — damages the player on touch, with a per-enemy cooldown.
- Pooled like projectiles.

### Pickups
- **`XpGem`** — when player within `pickupRadius`, flies to the player; on contact grants XP.

### Progression
- **`UpgradeDefinition`** (ScriptableObject) — name, icon, description, effect
  (e.g. +10% damage, +1 max HP, +move speed).
- **`UpgradeManager`** — on `OnLevelUp`: pause, roll 3 random upgrades, show cards,
  apply the chosen one to `PlayerStats`, resume.

### Game flow & UI
- **`GameManager`** — state machine (`Playing` / `LevelUp` / `GameOver`) + survival timer.
- **HUD** (uses the GUI Pro pack in `External Assets`): health bar, XP bar, level, timer.
- **`LevelUpPanel`** — 3 upgrade cards. **`GameOverPanel`** — time survived + restart.

## Project Structure

All first-party work lives under `Assets/_Game/` (separate from third-party
`Assets/External Assets`).

```
Assets/_Game/
  Scenes/        Game.unity
  Scripts/
    Core/        GameManager, PlayerStats
    Player/      PlayerMovement, PlayerHealth, PlayerExperience
    Weapons/     KnifeWeapon, Projectile, ObjectPool
    Enemies/     EnemySpawner, EnemyMovement, EnemyHealth, ContactDamage
    Pickups/     XpGem
    Progression/ UpgradeManager, UpgradeDefinition
    UI/          HUDController, LevelUpPanel, GameOverPanel
  Prefabs/       Player, Knife, Visitor, XpGem
  ScriptableObjects/ Upgrades/
  Art/           placeholder sprites
```

## Physics Setup

- 2D layers: `Player`, `Enemy`, `Projectile`, `Pickup`.
- Collision matrix: projectiles hit enemies (not player); enemies touch player; gems
  trigger on player.
- Enemies do **not** collide with each other in Milestone 1 (clean swarming, cheaper).

## Testing

- **EditMode unit tests** for the math-y pieces:
  - `PlayerStats` — upgrades stack/apply correctly.
  - `PlayerExperience` — level thresholds trigger correctly.
  - `ObjectPool` — objects are reused, not leaked.
- **Play-mode verification** for movement, spawning, collisions, and the full loop.

## Build Order (each step playable before the next)

1. Scene + player moving with WASD (placeholder sprite, camera follows).
2. Knife weapon auto-firing at a dummy target + projectile/pool.
3. Enemy spawner + chasing visitors + `EnemyHealth` (knives kill them).
4. `ContactDamage` + `PlayerHealth` + game-over screen.
5. XP gems + `PlayerExperience` + XP bar HUD.
6. `UpgradeManager` + level-up panel with 3 cards.
7. Survival timer + HUD polish.

## Out of Scope (future milestones)

More weapons and evolutions, additional enemy/boss types, meta-progression between runs,
in-run shop, audio, and final art.
