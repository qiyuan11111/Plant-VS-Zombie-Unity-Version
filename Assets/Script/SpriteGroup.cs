using System;
using Script.Manager;
using UnityEngine;

namespace Script
{
    public class SpriteGroup : SpriteImage
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Collider2D[] _childCollider2D;
        private MaterialPropertyBlock _materialProperties;
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
            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            var point = SpriteManager.Instance.GetSortingLayerNewOrder(layerName, spriteRenderers.Length);
            foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.sortingLayerName = layerName;
                spriteRenderer.sortingOrder += (int)point;
            }
        
            return this;
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
