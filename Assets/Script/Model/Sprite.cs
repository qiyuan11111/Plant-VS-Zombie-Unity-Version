using System;
using System.Collections.Generic;
using Script.Manager;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Model
{
    public abstract class Sprite : MonoBehaviour
    {
        private long _timeId;
        protected Transform Transform;
    
        [SerializeField, Tooltip("Sub sprite transforms under this object")]
        private SpriteTransform[] childSpriteTransforms;
        public IReadOnlyList<SpriteTransform> ChildSpriteTransforms => childSpriteTransforms;

        public virtual Vector3 SpritePosition { get; set; }

        public SpriteGroup ComponentRoot { get; private set; }
        
        // public virtual Vector3 spritePosition{set; get;}
        
        public SpriteGroup GetComponentRoot()
        {
            return ComponentRoot;
        }

        public virtual Sprite Reset()
        {
            _timeId = DateTime.Now.ToUniversalTime().Ticks;
            RefreshChildTransforms();
            CacheSpriteGroup();
            return this;
        }

        protected void Awake()
        {
            Transform = transform;
            CacheSpriteGroup();
            RefreshChildTransforms();
        }

        private void CacheSpriteGroup()
        {
            ComponentRoot = GetComponent<SpriteGroup>();
            if (ComponentRoot == null)
            {
                Debug.LogWarning($"Missing SpriteGroup component in parent hierarchy for {name}", this);
            }
        }

        private void RefreshChildTransforms()
        {
            var children = new List<SpriteTransform>();
            foreach (Transform child in Transform)
            {
                var childTransforms = child.GetComponentsInChildren<SpriteTransform>(true);
                children.AddRange(childTransforms);
            }
            childSpriteTransforms = children.ToArray();
        }
        
        /**********/
    
        // 设置所有材质为半透明
        protected void SetTransparentMaterial()
        {
            GetComponentRoot().SetTransparentMaterial();
        }

        protected void SetComponentState(bool state)
        {
            GetComponentRoot().SetColliderState(state);
        }
    
        public void SetLayer(string layerName)
        {
            GetComponentRoot().SetLayer(layerName);
        }
        
        public void SetSortingLayer(string layerName)
        {
            GetComponentRoot().SetSortingLayer(layerName);
        }
    }
}