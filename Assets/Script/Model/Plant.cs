using UnityEngine;

namespace Script.Model
{
    public abstract class Plant : Character
    {
        public Transform shadowTransform;

        protected new void Awake()
        {
            base.Awake();
            shadowTransform = transform.Find("Shadow_Anchor"); 
        } 
        
        public void SetNormalMode()
        {
            SetSortingLayer("plant-"+Row);
        }
    }
}
