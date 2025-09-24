using UnityEngine;

namespace Core.Utils
{
    public static class ComponentsExtension
    {
        public static void SetActive(this Component component, bool active)
        {
            component.gameObject.SetActive(active);
        }
    }
}