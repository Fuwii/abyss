using UnityEngine;

namespace Core.Singleton
{
    public abstract class Singleton<T> : MonoBehaviour
        where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"Another instance of {Instance} is already exists");
                Destroy(this);
            }

            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
        }
    }
}