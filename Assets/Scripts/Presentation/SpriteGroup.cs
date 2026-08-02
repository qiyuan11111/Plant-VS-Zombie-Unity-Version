using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Script
{
    public class SpriteGroup : SpriteImage
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Collider2D[] _childColliders = Array.Empty<Collider2D>();
        private SpriteRenderer[] _childSpriteRenderers = Array.Empty<SpriteRenderer>();
        private readonly List<SpriteTransform> _childSpriteTransforms = new();
        private MaterialPropertyBlock _materialProperties;
        private SortingGroup _sortingGroup;
        private bool _componentCacheInitialized;
        private bool _spriteTransformOrderDirty;
        private bool _scheduleSpriteTransforms = true;

        public SpriteGroup SetTransparentMaterial()
        {
            EnsureComponentCache();
            _materialProperties ??= new MaterialPropertyBlock();
            foreach (var spriteRenderer in _childSpriteRenderers)
            {
                spriteRenderer.GetPropertyBlock(_materialProperties);
                _materialProperties.SetColor(ColorProperty, new Color(1, 1, 1, 0.5f));
                spriteRenderer.SetPropertyBlock(_materialProperties);
            }

            return this;
        }
        
        public SpriteGroup SetSortingLayer(string layerName)
        {
            EnsureComponentCache();
            if (CanUseSortingGroup(layerName))
            {
                EnsureSortingGroup();

                _sortingGroup.sortingLayerName = layerName;
                _sortingGroup.sortingOrder = 0;
                return this;
            }

            foreach (var spriteRenderer in _childSpriteRenderers)
            {
                spriteRenderer.sortingLayerName = layerName;
            }
        
            return this;
        }

        private static bool CanUseSortingGroup(string layerName)
        {
            return !layerName.StartsWith("plant-", StringComparison.Ordinal) &&
                   !layerName.StartsWith("zombie-", StringComparison.Ordinal);
        }
        
        public SpriteGroup SetLayer(string layerName)
        {
            EnsureComponentCache();
            var layer = LayerMask.NameToLayer(layerName);
            foreach (var sprite in _childSpriteTransforms)
            {
                sprite.gameObject.layer = layer;
            }

            return this;
        }
        
        public SpriteGroup SetColliderState(bool state)
        {
            EnsureComponentCache();
            foreach (var childCollider in _childColliders)
            {
                childCollider.enabled = state;
            }

            return this;
        }

        public void ApplyAndDisableSpriteTransforms()
        {
            EnsureComponentCache();
            SortSpriteTransformsIfNeeded();
            foreach (var spriteTransform in _childSpriteTransforms)
            {
                if (spriteTransform == null) continue;
                spriteTransform.ApplyAndDisable();
            }

            _scheduleSpriteTransforms = false;
        }

        private void Awake()
        {
            RefreshComponentCache();
        }

        private void LateUpdate()
        {
            if (!_scheduleSpriteTransforms) return;

            EnsureComponentCache();
            SortSpriteTransformsIfNeeded();

            // Animator properties have been evaluated before LateUpdate. Apply
            // every part once, parent first, without one Unity Update callback
            // per SpriteTransform.
            foreach (var spriteTransform in _childSpriteTransforms)
            {
                if (spriteTransform == null ||
                    !spriteTransform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                spriteTransform.ApplyFromScheduler(this);
            }
        }

        internal void RegisterSpriteTransform(SpriteTransform spriteTransform)
        {
            if (spriteTransform == null ||
                spriteTransform.GetComponentInParent<SpriteGroup>() != this)
            {
                return;
            }

            if (!_childSpriteTransforms.Contains(spriteTransform))
            {
                _childSpriteTransforms.Add(spriteTransform);
                _spriteTransformOrderDirty = true;
            }

            spriteTransform.SetScheduler(this);
        }

        internal void UnregisterSpriteTransform(SpriteTransform spriteTransform)
        {
            if (spriteTransform == null) return;

            if (_childSpriteTransforms.Remove(spriteTransform))
            {
                _spriteTransformOrderDirty = true;
            }

            spriteTransform.ClearScheduler(this);
        }

        private void EnsureComponentCache()
        {
            if (_componentCacheInitialized) return;
            RefreshComponentCache();
        }

        private void RefreshComponentCache()
        {
            _childColliders = GetComponentsInChildren<Collider2D>(true);
            _childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _childSpriteTransforms.Clear();

            foreach (var spriteTransform in GetComponentsInChildren<SpriteTransform>(true))
            {
                // A nested SpriteGroup owns and schedules its own subtree.
                if (spriteTransform.GetComponentInParent<SpriteGroup>() != this) continue;
                RegisterSpriteTransform(spriteTransform);
            }

            _componentCacheInitialized = true;
            _spriteTransformOrderDirty = true;
            SortSpriteTransformsIfNeeded();
        }

        private void SortSpriteTransformsIfNeeded()
        {
            if (!_spriteTransformOrderDirty) return;

            _childSpriteTransforms.RemoveAll(spriteTransform => spriteTransform == null);
            _childSpriteTransforms.Sort((left, right) =>
                GetHierarchyDepth(left.transform).CompareTo(GetHierarchyDepth(right.transform)));
            _spriteTransformOrderDirty = false;
        }

        private int GetHierarchyDepth(Transform target)
        {
            var depth = 0;
            while (target != null && target != transform)
            {
                depth++;
                target = target.parent;
            }

            return depth;
        }

        private void EnsureSortingGroup()
        {
            if (_sortingGroup != null) return;

            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingGroup == null)
            {
                _sortingGroup = gameObject.AddComponent<SortingGroup>();
            }
        }
    }
}
