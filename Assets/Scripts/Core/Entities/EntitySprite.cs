using UnityEngine;
using PvZ.Presentation;

namespace PvZ.Core.Entities
{
    public abstract class EntitySprite : MonoBehaviour
    {
        protected Transform Transform;
    
        public SpriteGroup ComponentRoot { get; private set; }
        
        public SpriteGroup GetComponentRoot()
        {
            if (ComponentRoot == null)
            {
                throw new MissingComponentException($"{name} requires a SpriteGroup component");
            }

            return ComponentRoot;
        }

        public virtual EntitySprite ResetRuntimeState()
        {
            Transform = transform;
            CacheSpriteGroup();
            return this;
        }

        protected virtual void Awake()
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
