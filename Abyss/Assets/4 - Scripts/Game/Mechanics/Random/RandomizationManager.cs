using System;
using System.Collections.Generic;
using Core.Singleton;
using Game.Mechanics.Random.Seed;
using UnityEngine;

namespace Game.Mechanics.Random
{
    public class RandomizationManager : Singleton<RandomizationManager>
    {
        [Header("General")]
        [SerializeField] private RandomMode mode = RandomMode.Independent;

        [Header("Filled by Collector (editor)")]
        [SerializeField] private RandomizableComponent[] randomizableComponents;

        [Header("Assign the IRandomizer components here (drag your TreeRandomizer, LootRandomizer, etc.)")]
        [SerializeField] private MonoBehaviour[] randomizers;

        private readonly Dictionary<RandomCategory, IRandomizer> _map = new();

        protected override void Awake()
        {
            base.Awake();

            _map.Clear();

            foreach (var mb in randomizers)
                if (mb is IRandomizer r)
                    _map[r.Category] = r;
        }

        public void Run(ulong seed)
        {
            if (mode == RandomMode.Sequential)
            {
                var rng = new SeededRandom(seed);

                Array.Sort(randomizableComponents, (a, b) => a.persistentId.CompareTo(b.persistentId));

                foreach (var comp in randomizableComponents)
                {
                    if (!_map.TryGetValue(comp.Category, out var randomizer)) continue;

                    randomizer.Randomize(comp, ref rng, seed, mode);
                }
            }
            else
            {
                foreach (var comp in randomizableComponents)
                {
                    if (!_map.TryGetValue(comp.Category, out var randomizer)) continue;

                    var dummy = new SeededRandom(seed);

                    randomizer.Randomize(comp, ref dummy, seed, mode);
                }
            }
        }
    }
}