using System;
using Script.Manager;
using UnityEngine;
using UnityEngine.Rendering;

namespace Script
{
    public class SpriteGroup : SpriteImage
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Collider2D[] _childCollider2D;
        private MaterialPropertyBlock _materialProperties;
        private SortingGroup _sortingGroup;
        // public Animator[] childAnimator;
        
        public SpriteGroup SetTransparentMaterial()
        {
            _materialProperties ??= new MaterialPropertyBlock();
            foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
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
                if (_sortingGroup == null)
                {
                    _sortingGroup = GetComponent<SortingGroup>();
                    if (_sortingGroup == null)
                    {
                        _sortingGroup = gameObject.AddComponent<SortingGroup>();
                    }
                }

                _sortingGroup.sortingLayerName = layerName;
                _sortingGroup.sortingOrder = (int)SpriteManager.Instance.GetSortingLayerNewOrder(layerName, 1);
                return this;
            }

            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            var point = SpriteManager.Instance.GetSortingLayerNewOrder(layerName, spriteRenderers.Length);
            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.sortingLayerName = layerName;
                spriteRenderer.sortingOrder += (int)point;
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
            foreach (var sprite in GetComponentsInChildren<SpriteTransform>())
            {
                sprite.gameObject.layer = LayerMask.NameToLayer(layerName);
            }

            return this;
        }
        
        public SpriteGroup SetColliderState(bool state)
        {
            foreach (var _collider in _childCollider2D)
            {
                _collider.enabled = state;
            }

            return this;
        }

        private void Awake()
        {
            _childCollider2D = GetComponentsInChildren<Collider2D>();
        }
    }
}
