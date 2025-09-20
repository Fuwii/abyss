using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class RandomizationManager : MonoBehaviour
{
    [Header("General")]
    public long worldSeed = 12345;
    public RandomMode mode = RandomMode.Independent;

    [Header("Filled by Collector (editor)")]
    public List<RandomizableComponent> allRandomizables = new List<RandomizableComponent>();

    [Header("Assign the IRandomizer components here (drag your TreeRandomizer, LootRandomizer, etc.)")]
    public MonoBehaviour[] randomizerComponents; // drag components that implement IRandomizer

    Dictionary<RandomCategory, IRandomizer> map = new Dictionary<RandomCategory, IRandomizer>();

    void Awake()
    {
        map.Clear();
        foreach (var mb in randomizerComponents)
            if (mb is IRandomizer r) map[r.Category] = r;
    }

    public void RunAllRandomizations()
    {
        if (mode == RandomMode.Sequential)
        {
            var rng = new SeededRandom((ulong)worldSeed);
            allRandomizables.Sort((a, b) => a.persistentId.CompareTo(b.persistentId));
            foreach (var comp in allRandomizables)
            {
                if (map.TryGetValue(comp.Category, out var r))
                    r.Randomize(comp, ref rng, worldSeed, mode);
            }
        }
        else 
        {
            foreach (var comp in allRandomizables)
            {
                if (map.TryGetValue(comp.Category, out var r))
                {
                    var dummy = new SeededRandom((ulong)worldSeed);
                    r.Randomize(comp, ref dummy, worldSeed, mode);
                }
            }
        }
    }
}
