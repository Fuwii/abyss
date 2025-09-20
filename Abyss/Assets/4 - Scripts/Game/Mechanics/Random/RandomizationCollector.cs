// Editor/RandomizationCollector.cs

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Mechanics.Random
{
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
            var maxExisting = list.Where(x => x.persistentId != 0)
                .Select(x => x.persistentId)
                .DefaultIfEmpty(0).Max();

            var next = System.Math.Max(1, maxExisting + 1);
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
}
#endif