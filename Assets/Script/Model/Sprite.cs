using System;
using UnityEngine;

namespace Script.Model
{
    public abstract class Sprite : MonoBehaviour
    {
        private long _timeId;
        protected Transform Transform;
    
        public virtual Vector3 SpritePosition { get; set; }

        public SpriteGroup ComponentRoot { get; private set; }
        
        // public virtual Vector3 spritePosition{set; get;}
        
        public SpriteGroup GetComponentRoot()
        {
            if (ComponentRoot == null)
            {
                throw new MissingComponentException($"{name} requires a SpriteGroup component");
            }

            return ComponentRoot;
        }

        public virtual Sprite Reset()
        {
            _timeId = DateTime.Now.ToUniversalTime().Ticks;
            CacheSpriteGroup();
            return this;
        }

        protected void Awake()
        {
            Transform = transform;
            CacheSpriteGroup();
        }

        private void CacheSpriteGroup()
        {
            ComponentRoot = GetComponent<SpriteGroup>();
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
