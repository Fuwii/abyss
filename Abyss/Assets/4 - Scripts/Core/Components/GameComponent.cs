using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Scene
{
    public abstract class GameComponent<T> : MonoBehaviour
        where T : GameComponent<T>
    {
        private static readonly HashSet<T> Registered = new();

        /// <summary>
        /// Все зарегистрированные компоненты.
        /// </summary>
        /// <remarks>
        /// НЕ использовать в <c>Awake</c>.
        /// </remarks>
        public static IEnumerable<T> AllComponents => Registered;

        /// <summary>
        /// Все зарегистрированные и активные компоненты.
        /// </summary>
        /// <remarks>
        /// НЕ использовать в <c>Awake</c>.
        /// </remarks>
        public static IEnumerable<T> EnabledComponents => Registered.Where(g => g.isActiveAndEnabled);

        public static T GetComponent(bool ignoreDisabled = true)
        {
            if (ignoreDisabled)
            {
                return AllComponents.FirstOrDefault();
            }

            return EnabledComponents.FirstOrDefault();
        }

        protected virtual void Awake()
        {
            Registered.Add(this as T);
        }

        protected virtual void OnDestroy()
        {
            Registered.Remove(this as T);
        }
    }
}