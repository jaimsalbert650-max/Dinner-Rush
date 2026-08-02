# Dinner Rush

A bullet-heaven survivors game set in a kitchen. You cook, the crowd comes for you, and the
weapons are food: a pasta twister that hoovers the crowd up and throws it back, meatballs
that fall slowly and land as a splash, a sauce puddle that burns what stands in it.

Unity 6 (6000.5.3f1), C#, Android. Solo project by Daniil Muratov, 2026.

## What is in this repository

**Gameplay code and design documents only.** The art, scenes and prefabs are left out: part
of the project's visual effects came from third-party Asset Store packs that may not be
redistributed, and rather than publish a half-cleaned project this repository carries the
work that is mine. It is therefore a reading repository, not a project you can open in Unity
and press Play.

```
Assets/_Game/Scripts/
  Core/         run state, timing, spawning, object pooling
  Enemies/      enemy types, movement, health, death
  Weapons/      weapon behaviours and their projectiles
  Player/       movement, input, collection radius
  Progression/  XP, levels, upgrade offers, passive items
  UI/           HUD, level-up screen, end screen
docs/           the design documents, written before the code
```

## The design documents

Each system was written down before it was built, and rewritten when playtesting disagreed
with it:

| Document | What it decides |
|---|---|
| `docs/2026-07-20-dinner-rush-bullet-heaven-design.md` | The core run: what a run is, how it ends, what carries between runs |
| `docs/2026-07-23-wave-survival-design.md` | Wave pacing and the difficulty curve |
| `docs/2026-07-22-passive-items-design.md` | Passive items and how they interact with weapons |
| `docs/2026-07-23-food-heal-pickup-design.md` | Healing pickups and why healing is scarce |
| `docs/2026-07-20-dinner-rush-combat-juice-design.md` | Hit feedback: what the player feels when something dies |

## How it was built

259 commits over four weeks, each one small enough to read. Weapons were designed around a
behaviour rather than a damage number, so that two weapons at the same power level still
play differently. The difficulty curve was tuned against real runs, and what the playtest
showed was written back into the balance document instead of only into the code.

Built with [Claude Code](https://claude.com/claude-code) as a working partner: specification
and plan first, implementation and review after.
