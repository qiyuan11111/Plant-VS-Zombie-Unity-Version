using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Script
{
    public class SpriteGroup : SpriteImage
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Collider2D[] _childColliders = Array.Empty<Collider2D>();
        private SpriteRenderer[] _childSpriteRenderers = Array.Empty<SpriteRenderer>();
        private SpriteTransform[] _childSpriteTransforms = Array.Empty<SpriteTransform>();
        private MaterialPropertyBlock _materialProperties;
        private SortingGroup _sortingGroup;

        public SpriteGroup SetTransparentMaterial()
        {
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
            var layer = LayerMask.NameToLayer(layerName);
            foreach (var sprite in _childSpriteTransforms)
            {
                sprite.gameObject.layer = layer;
            }

            return this;
        }
        
        public SpriteGroup SetColliderState(bool state)
        {
            foreach (var childCollider in _childColliders)
            {
                childCollider.enabled = state;
            }

            return this;
        }

        public void ApplyAndDisableSpriteTransforms()
        {
            foreach (var spriteTransform in _childSpriteTransforms)
            {
                spriteTransform.ApplyAndDisable();
            }
        }

        private void Awake()
        {
            _childColliders = GetComponentsInChildren<Collider2D>(true);
            _childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _childSpriteTransforms = GetComponentsInChildren<SpriteTransform>(true);
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
