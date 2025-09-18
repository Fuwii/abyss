using UnityEngine;

namespace Core
{
    public abstract class Singleton<T> : MonoBehaviour
        where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"Another instance of {Instance} is already running");
                Destroy(this);
            }

            Instance = this as T;
        }
    }
}