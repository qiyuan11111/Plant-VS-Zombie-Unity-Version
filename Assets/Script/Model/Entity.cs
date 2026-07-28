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
        
        protected T PrePareToField<T>() where T : OnFieldEntity
        {
            var onFieldEntity = gameObject.AddComponent<T>();
            onFieldEntity.SetEntity(this);
            return onFieldEntity;
        }

        public abstract void AfterCreate(Dictionary<string, object> param);

        public abstract OnFieldEntity ToField(Dictionary<string, object> param = null);

        public Entity DisableAnimation()
        {
            if (Animator != null)
            {
                Animator.enabled = false;
            }

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
        
        
        
        
        
        
        
        
        // public OnFieldEntity DisableAnimation()
        // {
        //     GetEntity<Entity>().DisableAnimation();
        //     return this;
        // }
        // public OnFieldEntity DisableRaycast()
        // {
        //     GetEntity<Entity>().DisableRaycast();
        //     return this;
        // }
        // public Entity SetEntity(Entity entity)
        // {
        //     Sprite = entity;
        //     return entity;
        // }

        // protected T GetEntity<T>() where T : Entity
        // {
        //     return (T) Sprite;
        // }
        
        // public string GetEnglishName()
        // {
        //     return GetEntity<Entity>().GetEnglishName();
        // }
        //
        // public string GetChineseName()
        // {
        //     return GetEntity<Entity>().GetChineseName();
        // }
        
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
                .SetSortingLayer("plant")
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
            var shadowPosition = ((Plant)this).shadowTransform.position;
            var shadowObject = Instantiate(MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow), shadowPosition, Quaternion.identity, Transform);
            var shadow = shadowObject.GetComponent<Shadow>();

            shadow.SetSize(0.7f)
                .ToField();
            
            return this;
        }
        
        private static bool hasSunUnderPointer = false;
        private static GameObject currentTopObject = null;
        
        

        protected void Update()
        {
        }
    }
}
