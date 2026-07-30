using System.Collections.Generic;
using System.Linq;
using Prefab.Object.Sun.Script;
using Script.Manager;
using Script.Util;
using UnityEngine;
using UnityEngine.UI;
using Shadow = Prefab.Object.Shadow.Script.Shadow;

// using UnityEngine.UI;

namespace Script.Model
{
    public abstract class Entity : Sprite
    {
        // private readonly Dictionary<string, Task> _functions = new();
    
        protected Animator Animator; //动画

        public abstract string GetChineseName();
        public abstract string GetEnglishName();
        
        

        protected new void Awake()
        {
            base.Awake();
            Animator = GetComponentInChildren<Animator>();
        } 
        
        public abstract void AfterCreate(Dictionary<string, object> param);

        public abstract Entity ToField(Dictionary<string, object> param = null);

        public Entity DisableAnimation()
        {
            if (Animator != null)
            {
                Animator.enabled = false;
            }

            GetComponentRoot().ApplyAndDisableSpriteTransforms();

            return this;
        }

        public Entity DisableRaycast()
        {
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
            return this;
        }
        public Entity SetParent(Transform parentTransform)
        {
            Transform.SetParent(parentTransform);
            return this;
        }
    
        public Entity SetPosition(Vector3 position)
        {
            Transform.position = position;
            return this;
        }
    
        public Entity SetLocalPosition(Vector3 position)
        {
            Transform.localPosition = position;
            return this;
        }
    
        public Entity SetLocalScale(Vector3 localScale)
        {
            Transform.localScale = localScale;
            return this;
        }

        public Entity SetName(string name)
        {
            Transform.name = name;
            return this;
        }
        
        public Entity SetCardIconMode()
        {
            GetComponentRoot()
                .SetSortingLayer("card")
                .SetColliderState(false);
            
            this.SetLocalScale(new Vector3(0.54f, 0.54f, 1f))
                .SetLocalPosition(new Vector3(0, 1.1f, 0));
            
            this.DisableAnimation()
                .DisableRaycast();
            return this;
        }
        
        
        public Entity SetMouseIconMode()
        {
            GetComponentRoot()
                .SetSortingLayer("front")
                .SetColliderState(false);
            
            this.SetLocalScale(new Vector3(1.025f, 1.025f, 1f))
                .SetName(GetEnglishName() + "-Mouse");
            
            this.DisableAnimation()
                .DisableRaycast();
            
            return this;
        }
        
        
        public Entity SetGridIconMode()
        {
            GetComponentRoot()
                .SetTransparentMaterial()
                .SetColliderState(false);
            
            this.SetLocalScale(new Vector3(1.025f, 1.025f, 1f))
                .SetName(GetEnglishName() + "-Grid");
            
            this.DisableAnimation()
                .DisableRaycast();
            return this;
        }
        
        
        public virtual Entity SetPlaceMode(GridManager.Grid grid)
        {
            GetComponentRoot()
                .SetSortingLayer("plant-" + grid.Point.y)
                .SetColliderState(true);
            
            this.SetLocalScale(new Vector3(1.025f, 1.025f, 1f))
                .SetLocalPosition(new Vector3(grid.Position.x, grid.Position.y, 10f));
            
            this.SetName(GetEnglishName() + "-" + grid.Point.x + "-" + grid.Point.y)
                .SetShadow();
            return this;
        }
        
        public Entity SetShadow()
        {
            var plant = (Plant)this;
            var shadowPosition = plant.shadowTransform != null
                ? plant.shadowTransform.position
                : Transform.TransformPoint(new Vector3(0f, -17f, 0f));
            var shadowObject = Instantiate(MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow), shadowPosition, Quaternion.identity, Transform);
            var shadow = shadowObject.GetComponent<Shadow>();

            shadow.SetSize(0.7f)
                .ToField();
            
            return this;
        }
        
    }
}
