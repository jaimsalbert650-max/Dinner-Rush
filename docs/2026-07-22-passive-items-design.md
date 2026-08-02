# Passive Items ("Supplies") — Design

**Date:** 2026-07-22
**Status:** approved

## Goal
Add a system of leveled **passive items** (VS / Survivor.io style) that are offered at
level-up alongside weapons/stats, tracked per-run with levels 1–5, and displayed in the
pause menu's currently-empty **Supplies** panel. Adds build variety.

## The 5 passives

| Item | Field (PlayerStats) | Per level | At Lv 5 | Hook |
|------|---------------------|-----------|---------|------|
| Apron (Armor)       | `DamageReduction` (0→) | +0.06 | −30% contact damage | `PlayerHealth.Damage` |
| Coffee (Regen)      | `HpRegen` hp/s (0→)    | +0.35 | 1.75 HP/s           | `PlayerHealth.Update` |
| Notepad (Wisdom)    | `XpMult` (1→)          | +0.12 | +60% XP             | `PlayerExperience.AddXp` |
| Tip Jar (Greed)     | `CoinMult` (1→)        | +0.15 | +75% coins          | `GameOverPanel` banking |
| Sharp Knife (Crit)  | `CritChance` (0→)      | +0.06 | 30% chance, 2× dmg  | `PlayerStats.RollDamage()` |

All values are inspector/const-tunable; a balance pass can adjust later.

## Components

### PlayerStats
- New fields: `DamageReduction=0`, `HpRegen=0`, `XpMult=1`, `CoinMult=1`, `CritChance=0`, `CritMult=2`.
- New `StatType` values: `Armor, Regen, Wisdom, Greed, Crit` (routed by `AddModifier` to the
  matching field).
- `float RollDamage(float dmg)` → `Random.value < CritChance ? dmg * CritMult : dmg`.

### Hooks
- `PlayerHealth.Damage(amount)`: `amount *= 1 - Mathf.Clamp(DamageReduction, 0, 0.8)`.
- `PlayerHealth.Update()`: if alive and not full, `Heal(HpRegen * Time.deltaTime)` (silent — no
  event spam; reuse Heal but guard the tiny amounts).
- `PlayerExperience.AddXp(amount)`: multiply by `XpMult` (needs a `PlayerStats` ref, resolved from
  `PlayerRoot` in `Start`), round to int, min 1.
- `GameOverPanel`: banked `coins = round(gs.Coins * rewardMult * stats.CoinMult)`.
- Weapons (`KnifeWeapon`, `AuraWeapon`, `MolotovWeapon`, `DroneWeapon`): replace the `Stats.Damage`
  they pass into projectiles with `Stats.RollDamage(<that damage>)`.

### PassiveInventory (new component on Player)
- Holds `Dictionary<StatType,int> _levels` for the 5 passive stats; `IReadOnlyList` accessor for UI.
- `int AddOrLevel(StatType stat, float amountPerLevel, int maxLevel)` — increments the level (capped),
  applies `amountPerLevel` via `Stats.AddModifier`, returns the new level (0 if already max → no-op).
- `int LevelOf(StatType)`, `IEnumerable<(StatType stat,int level)> Owned`.
- `Stats` injected by `PlayerRoot.Awake` (like `PlayerMovement`/`KnifeWeapon`).

### UpgradeDefinition / LevelUpPanel
- Add `UpgradeKind.Passive`. Passive assets carry `stat` (one of the 5), `amount` (per level),
  `maxLevel` (5).
- `LevelUpPanel.Pick`: Passive → `PassiveInventory.AddOrLevel(up.stat, up.amount, up.maxLevel)`.
- Status text like weapons: `★ NEW! / Lv N→N+1 / Lv N (MAX)` via a `PassiveStatus(up)` helper.
- Create 5 `UpgradeDefinition` assets under `Assets/_Game/ScriptableObjects/Upgrades/` and append
  them to the scene `LevelUpPanel.pool` array.

### PauseMenu (Supplies panel)
- Store the 6 supply-slot `Image`s (currently discarded).
- `RefreshSupplies()` (called from `SetPaused(true)` next to `RefreshWeapons`): fill slots from
  `PassiveInventory.Owned` — colored icon + short name + star row (level/max), mirroring
  `RefreshWeapons`.
- Colors: Apron steel-blue, Coffee brown, Notepad tan, Tip Jar gold, Sharp Knife red.

## Non-goals
- No new passive that needs an attacker reference (thorns) or per-frame global systems.
- Crit does not recolor damage numbers (a bigger number already reads as a crit).
- Core stat upgrades (Damage/Speed/etc.) stay invisible bumps; only the 5 passives show in Supplies.

## Verification
- Pure/logic: reflection test that `AddOrLevel` caps at 5 and applies amounts; `RollDamage` crit at
  `CritChance=1`.
- In-run: force-grant passives, open pause → Supplies shows them with correct star levels; confirm
  armor reduces damage and regen heals via reflection reads.
