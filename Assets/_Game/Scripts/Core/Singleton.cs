using UnityEngine;

namespace Crumble.Core
{
    /// <summary>
    /// Base class for the persistent manager singletons living on the _Bootstrap object.
    /// A duplicate bootstrap (e.g. re-loading the Main scene) destroys itself.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
