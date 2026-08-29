using System;
using PvZ.Gameplay.Entities;
using UnityEngine;

namespace PvZ.Gameplay.Detection
{
    public abstract class ColliderBinding : MonoBehaviour
    {
        [SerializeField] private Collider2D boundCollider;
        [SerializeField] private GameEntity owner;

        public Collider2D Collider => boundCollider;
        public GameEntity Owner => owner;

        protected virtual void Awake()
        {
            ResolveReferences();
        }

        public void LoadCollider(Collider2D collider)
        {
            if (collider == null) throw new ArgumentNullException(nameof(collider));
            if (collider.gameObject != gameObject)
            {
                throw new ArgumentException(
                    $"{GetType().Name} can only bind a Collider2D on the same GameObject.",
                    nameof(collider));
            }

            EnsureColliderIsNotBoundByAnotherScript(collider);
            boundCollider = collider;
        }

        public ColliderBinding Configure(GameEntity bindingOwner, Collider2D collider)
        {
            owner = bindingOwner != null
                ? bindingOwner
                : throw new ArgumentNullException(nameof(bindingOwner));
            LoadCollider(collider);
            return this;
        }

        protected void EnsureReady()
        {
            ResolveReferences();
            if (boundCollider == null)
            {
                throw new MissingComponentException(
                    $"{name} requires exactly one local {nameof(Collider2D)}.");
            }

            if (owner == null)
            {
                throw new MissingComponentException(
                    $"{name} requires a {nameof(GameEntity)} in its parent hierarchy.");
            }
        }

        private void ResolveReferences()
        {
            if (owner == null) owner = GetComponentInParent<GameEntity>();
            if (boundCollider == null)
            {
                var colliders = GetComponents<Collider2D>();
                if (colliders.Length == 1)
                {
                    LoadCollider(colliders[0]);
                }
                else if (colliders.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"{name} has {colliders.Length} Collider2D components. " +
                        "A collision script requires exactly one Collider2D on its GameObject.");
                }
            }
        }

        private void EnsureColliderIsNotBoundByAnotherScript(Collider2D collider)
        {
            foreach (var binding in GetComponents<ColliderBinding>())
            {
                if (binding != this && binding.Collider == collider)
                {
                    throw new InvalidOperationException(
                        $"{collider.name} is already bound by {binding.GetType().Name}.");
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (owner == null) owner = GetComponentInParent<GameEntity>();
            if (boundCollider == null && TryGetComponent(out Collider2D collider))
            {
                boundCollider = collider;
            }
        }
#endif
    }
}
