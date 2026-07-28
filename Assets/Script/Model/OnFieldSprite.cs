using UnityEngine;

namespace Script.Model
{
    public abstract class OnFieldSprite : MonoBehaviour
    {
        protected Sprite Sprite;

        public Transform Transform;

        private SpriteGroup GetComponentRoot()
        {
            return Sprite.ComponentRoot;
        }
    
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

        private void Awake()
        {
            Transform = transform;
        }
    }
}