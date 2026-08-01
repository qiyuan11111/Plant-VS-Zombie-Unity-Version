using UnityEngine;

namespace Script.Manager
{
    /// <summary>
    /// Scene-scoped singleton lifecycle shared by all game managers.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            var current = (T)this;
            if (Instance != null && Instance != current)
            {
                Debug.LogError(
                    $"Duplicate {typeof(T).Name} detected. Keeping '{Instance.name}' and destroying '{name}'.",
                    this);
                enabled = false;
                Destroy(gameObject);
                return;
            }

            Instance = current;
            OnSingletonAwake();

            if (ValidateReferences())
            {
                OnReferencesValidated();
                return;
            }

            InvalidateInstance();
        }

        protected virtual void Start()
        {
            if (Instance != this) return;

            if (ValidateDependencies())
            {
                OnSingletonStart();
                return;
            }

            InvalidateInstance();
        }

        protected virtual void OnDestroy()
        {
            if (Instance != this) return;

            OnSingletonDestroy();
            Instance = null;
        }

        protected virtual void OnSingletonAwake()
        {
        }

        protected virtual bool ValidateReferences()
        {
            return true;
        }

        protected virtual void OnReferencesValidated()
        {
        }

        protected virtual bool ValidateDependencies()
        {
            return true;
        }

        protected virtual void OnSingletonStart()
        {
        }

        protected virtual void OnSingletonDestroy()
        {
        }

        protected bool RequireReference(UnityEngine.Object reference, string referenceName)
        {
            if (reference != null) return true;

            Debug.LogError(
                $"{typeof(T).Name} requires {referenceName} to be assigned or available.",
                this);
            return false;
        }

        protected bool RequireManager<TManager>(TManager manager) where TManager : MonoBehaviour
        {
            if (manager != null && manager.isActiveAndEnabled) return true;

            Debug.LogError(
                $"{typeof(T).Name} requires an active {typeof(TManager).Name} in the scene.",
                this);
            return false;
        }

        private void InvalidateInstance()
        {
            enabled = false;
            if (Instance == this) Instance = null;
        }
    }
}
