using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Mechanics.Random.Editor
{
    public static class RandomizationCollector
    {
        [MenuItem("Tools/Randomization/Collect To Manager")]
        public static void Collect()
        {
            var manager = Object.FindFirstObjectByType<RandomizationManager>();

            if (manager == null)
            {
                Debug.LogWarning("There is no RandomizationManager in the scene.");
                return;
            }

            var serializedObject = new SerializedObject(manager);
            var property = serializedObject.FindProperty("randomizableComponents");

            var list = Object.FindObjectsByType<RandomizableComponent>(FindObjectsSortMode.None).ToList();

            // assign IDs if zero
            var maxExisting = list
                .Where(x => x.persistentId != 0)
                .Select(x => x.persistentId)
                .DefaultIfEmpty(0)
                .Max();

            var next = System.Math.Max(1, maxExisting + 1);

            foreach (var c in list.Where(c => c.persistentId == 0))
            {
                c.persistentId = next++;
                EditorUtility.SetDirty(c);
            }

            list = list.OrderBy(x => x.persistentId).ToList();

            for (var i = 0; i < list.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }

            serializedObject.ApplyModifiedProperties();

            Debug.Log($"Collected {property.arraySize} randomizable components");
        }
    }
}