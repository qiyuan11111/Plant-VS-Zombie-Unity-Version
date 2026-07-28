using System;
using Script.Manager;
using UnityEngine;

namespace Script
{
    public class SpriteGroup : SpriteImage
    {
        private Collider2D[] _childCollider2D;
        // public Animator[] childAnimator;
        
        public SpriteGroup SetTransparentMaterial()
        {
            foreach (var material in GetComponentsInChildren<SpriteRenderer>())
            {
                material.material.color = new Color(1,1,1,0.5f);
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