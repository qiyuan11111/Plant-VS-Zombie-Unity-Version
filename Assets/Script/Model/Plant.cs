using UnityEngine;

namespace Script.Model
{
    public abstract class Plant : Character
    {
        public Transform shadowTransform;
        public abstract int GetDefaultSunPrice();
        
        public abstract float GetDefaultCdTime();
        
        protected new void Awake()
        {
            base.Awake();
            shadowTransform = transform.Find("Shadow_Anchor"); 
        } 
        
        public new void SetNormalMode()
        {
            SetSortingLayer("plant-"+Row);
        }
    }
}