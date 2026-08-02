using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// Holds the player's weapons. Registers any weapons already on the player (e.g. the starting
    /// knife) and can add new weapons or level existing ones at runtime — used by the level-up
    /// skill-choice. Injects the shared PlayerStats into every weapon.
    /// </summary>
    [RequireComponent(typeof(PlayerRoot))]
    public class WeaponManager : MonoBehaviour
    {
        private PlayerStats _stats;
        private readonly List<Weapon> _weapons = new();

        public IReadOnlyList<Weapon> Weapons => _weapons;

        private void Awake()
        {
            var root = GetComponent<PlayerRoot>();
            _stats = root != null ? root.Stats : new PlayerStats();
            foreach (var w in GetComponents<Weapon>()) Register(w);
            ApplyStartingTool();
        }

        /// <summary>
        /// Honour the tool picked on the Loadout screen (spec 04): make sure it exists, and switch off
        /// whichever other weapon the prefab shipped with, so the run starts with exactly one tool.
        /// Only touches weapons present at Awake — anything the level-up overlay adds later is safe.
        /// </summary>
        private void ApplyStartingTool()
        {
            var chosen = PlayerLoadout.CurrentTool.weapon;
            if (chosen == null) return;                       // locked pick: keep the prefab's weapon
            if (Get(chosen) == null) AddOrLevel(chosen);
            foreach (var w in _weapons)
                w.enabled = w.GetType() == chosen;
        }

        private void Register(Weapon w)
        {
            w.Stats = _stats;
            if (!_weapons.Contains(w)) _weapons.Add(w);
        }

        public Weapon Get(Type type)
        {
            foreach (var w in _weapons)
                if (w.GetType() == type) return w;
            return null;
        }

        /// <summary>Add the weapon if the player doesn't have it yet, otherwise level it up.</summary>
        public Weapon AddOrLevel(Type weaponType)
        {
            var existing = Get(weaponType);
            if (existing != null) { existing.LevelUp(); return existing; }
            var w = (Weapon)gameObject.AddComponent(weaponType);
            Register(w);
            return w;
        }
    }
}
