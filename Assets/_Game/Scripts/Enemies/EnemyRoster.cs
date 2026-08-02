using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// A single enemy "character": which customer sprite it wears, its size, and stat/ability
    /// multipliers applied on top of the wave's base HP / speed. Multipliers keep the difficulty
    /// curve in WaveManager the single source of truth — a Brute is always ~2.8x whatever the
    /// current wave's base HP is, so it stays threatening without hand-tuning every wave.
    /// </summary>
    public struct EnemyType
    {
        public string name;
        public int spriteIndex;      // index into EnemyVisual's customer sheet
        public float scale;          // body size (via EnemyAnimator.SetBaseScaleMult)
        public float hpMult;
        public float speedMult;
        public float contactDps;     // flat damage-per-second on contact
        public float burstDamage;    // >0 = kamikaze: detonates once on contact then dies
        public float standoffOverride; // >0 overrides the size-derived standoff (exploders get right up close)
        public int splitCount;       // >0 = on death, spawns this many small "spawnling" enemies
        public EnemyMovement.Ability ability;
        public Color ringColor;      // ground-ring colour-code (Color.clear = no ring)
        public int minWave;          // first wave this character can appear on
        public float weight;         // relative spawn weight once unlocked

        public bool HasRing => ringColor.a > 0.01f;
    }

    /// <summary>
    /// The full cast of enemy characters and the per-wave weighting that decides who shows up. Early
    /// waves are almost all ordinary office clerks; faster, tankier and special-ability characters
    /// unlock as the waves climb, with a heavy "manager" boss weighting in from the back third.
    /// </summary>
    public static class EnemyRoster
    {
        // Customer sheet indices (see EnemyVisual.Customers): 0 pilot, 1 judge, 2 doctor, 3 woman,
        // 4 clown, 5 worker, 6 chef, 7 painter, 8 red-haired woman.
        public static readonly EnemyType[] Types =
        {
            // The "ordinary enemy": office worker in the blue suit — the bread-and-butter chaser.
            new EnemyType { name = "Clerk",   spriteIndex = 5, scale = 1.00f, hpMult = 1.0f, speedMult = 1.00f, contactDps = 9f,  ability = EnemyMovement.Ability.None,    ringColor = Color.clear,                       minWave = 1,  weight = 10f },
            // Fast, fragile rusher.
            new EnemyType { name = "Runner",  spriteIndex = 3, scale = 0.85f, hpMult = 0.65f, speedMult = 1.6f, contactDps = 7f,  ability = EnemyMovement.Ability.None,    ringColor = new Color(0.3f,0.9f,1f,0.8f),      minWave = 3,  weight = 6f  },
            // Erratic weaver — hard to line up.
            new EnemyType { name = "Clown",   spriteIndex = 4, scale = 0.95f, hpMult = 0.8f,  speedMult = 1.25f, contactDps = 8f, ability = EnemyMovement.Ability.Zigzag,  ringColor = new Color(1f,0.4f,0.9f,0.8f),      minWave = 4,  weight = 5f  },
            // Gunner — hangs back at range and lobs projectiles at the player.
            new EnemyType { name = "Gunner",  spriteIndex = 0, scale = 1.00f, hpMult = 1.0f,  speedMult = 0.9f, contactDps = 6f, standoffOverride = 3.5f, ability = EnemyMovement.Ability.Ranged, ringColor = new Color(1f,0.5f,0.15f,0.85f),  minWave = 6,  weight = 4f  },
            // Medic: a support enemy that periodically heals nearby allies (green ring).
            new EnemyType { name = "Medic",   spriteIndex = 1, scale = 1.05f, hpMult = 1.4f,  speedMult = 0.85f, contactDps = 8f,  ability = EnemyMovement.Ability.Healer, ringColor = new Color(0.3f,1f,0.5f,0.85f),     minWave = 5,  weight = 3.5f},
            // Slow, heavy tank that occasionally lunges (Charger) to close the gap.
            new EnemyType { name = "Brute",   spriteIndex = 2, scale = 1.40f, hpMult = 2.8f,  speedMult = 0.6f, contactDps = 18f, ability = EnemyMovement.Ability.Charger, ringColor = new Color(1f,0.25f,0.2f,0.85f),    minWave = 8,  weight = 3.5f},
            // Splitter: a bloated cook that bursts into two small spawnlings when killed.
            new EnemyType { name = "Chef",    spriteIndex = 6, scale = 1.15f, hpMult = 1.8f,  speedMult = 0.95f, contactDps = 13f, splitCount = 2, ability = EnemyMovement.Ability.None, ringColor = new Color(0.7f,0.5f,1f,0.85f),     minWave = 9,  weight = 4f  },
            // Kamikaze: rushes right up to the player and detonates for a big burst, then dies.
            new EnemyType { name = "Painter", spriteIndex = 7, scale = 1.00f, hpMult = 0.7f,  speedMult = 1.35f, contactDps = 0f, burstDamage = 35f, standoffOverride = 0.2f, ability = EnemyMovement.Ability.None, ringColor = new Color(1f,0.55f,0.1f,0.9f), minWave = 7, weight = 4f },
            // "Manager" mini-boss: huge, tanky, hits like a truck, and summons minions. Rare, back third.
            new EnemyType { name = "Manager", spriteIndex = 8, scale = 1.90f, hpMult = 8f,    speedMult = 0.7f, contactDps = 25f, ability = EnemyMovement.Ability.Summoner, ringColor = new Color(0.9f,0.1f,0.1f,0.9f),   minWave = 10, weight = 1.2f},
        };

        /// <summary>
        /// Weighted-random pick of a character for the given wave. Ordinary clerks dominate early and
        /// taper off as tougher characters unlock, so the crowd's makeup shifts over the 20 waves.
        /// </summary>
        public static EnemyType PickForWave(int wave)
        {
            float total = 0f;
            for (int i = 0; i < Types.Length; i++)
                total += WeightFor(Types[i], wave);

            if (total <= 0f) return Types[0];

            float r = Random.value * total;
            for (int i = 0; i < Types.Length; i++)
            {
                float w = WeightFor(Types[i], wave);
                if (w <= 0f) continue;
                r -= w;
                if (r <= 0f) return Types[i];
            }
            return Types[0];
        }

        private static float WeightFor(in EnemyType t, int wave)
        {
            if (wave < t.minWave) return 0f;
            // The ordinary Clerk fades from full weight at wave 1 to a quarter by wave 20.
            if (t.name == "Clerk")
                return t.weight * Mathf.Lerp(1f, 0.25f, Mathf.Clamp01((wave - 1) / 19f));
            // Boss beats: the Manager mini-boss floods the milestone waves 10 and 20.
            if (t.name == "Manager")
                return (wave % 10 == 0) ? t.weight * 8f : t.weight;
            return t.weight;
        }
    }
}
