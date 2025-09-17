// Editor/RandomizationCollector.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class RandomizationCollector
{
    [MenuItem("Tools/Randomization/Collect To Manager")]
    public static void Collect()
    {
        var manager = Object.FindFirstObjectByType<RandomizationManager>();
        if (manager == null)
        {
            Debug.LogError("Add RandomizationManager to scene");
            return;
        }

        var list = Object.FindObjectsByType<RandomizableComponent>(FindObjectsSortMode.None).ToList();

        // assign IDs if zero
        long maxExisting = list.Where(x => x.persistentId != 0)
                               .Select(x => x.persistentId)
                               .DefaultIfEmpty(0).Max();

        long next = System.Math.Max(1, maxExisting + 1);
        foreach (var c in list)
        {
            if (c.persistentId == 0)
            {
                c.persistentId = next++;
                EditorUtility.SetDirty(c);
            }
        }

        manager.allRandomizables = list.OrderBy(x => x.persistentId).ToList();
        EditorUtility.SetDirty(manager);

        Debug.Log($"Collected {manager.allRandomizables.Count} randomizables");
    }
}
#endif
