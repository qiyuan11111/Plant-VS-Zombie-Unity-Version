using UnityEngine;
using PvZ.Presentation;

namespace PvZ.Core.Entities
{
    public abstract class EntitySprite : MonoBehaviour
    {
        protected Transform Transform;
    
        /// <summary>
        /// Origin of the entity's FLA coordinate space. Source values use X-right/Y-down.
        /// </summary>
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

        public virtual EntitySprite ResetRuntimeState()
        {
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
