using System;
using System.Collections.Generic;
using Prefab.Object.Shadow.Script;
using Script.Manager;
using Script.Util;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Script.Model
{
    public abstract class OnFieldEntity: OnFieldSprite
    {
        public OnFieldEntity DisableAnimation()
        {
            GetEntity<Entity>().DisableAnimation();
            return this;
        }
        public OnFieldEntity DisableRaycast()
        {
            GetEntity<Entity>().DisableRaycast();
            return this;
        }
        public Entity SetEntity(Entity entity)
        {
            Sprite = entity;
            return entity;
        }

        protected T GetEntity<T>() where T : Entity
        {
            return (T) Sprite;
        }
        
        public string GetEnglishName()
        {
            return GetEntity<Entity>().GetEnglishName();
        }
        
        public string GetChineseName()
        {
            return GetEntity<Entity>().GetChineseName();
        }
        
        public OnFieldEntity SetParent(Transform parentTransform)
        {
            Transform.SetParent(parentTransform);
            return this;
        }
    
        public OnFieldEntity SetPosition(Vector3 position)
        {
            Transform.position = position;
            return this;
        }
    
        public OnFieldEntity SetLocalPosition(Vector3 position)
        {
            Transform.localPosition = position;
            return this;
        }
    
        public OnFieldEntity SetLocalScale(Vector3 localScale)
        {
            Transform.localScale = localScale;
            return this;
        }

        public OnFieldEntity SetName(string name)
        {
            Transform.name = name;
            return this;
        }
        
        public OnFieldEntity SetCardIconMode()
        {
            Sprite.GetComponentRoot()
                .SetSortingLayer("card")
                .SetColliderState(false);
            SetLocalScale(new Vector3(0.54f, 0.54f, 1f));
            SetLocalPosition(new Vector3(0, 1.1f, 0));
            DisableAnimation();
            DisableRaycast();
            return this;
        }
        
        
        public OnFieldEntity SetMouseIconMode()
        {
            Sprite.GetComponentRoot()
                .SetSortingLayer("front")
                .SetColliderState(false);
            
            SetLocalScale(new Vector3(1.025f, 1.025f, 1f));
            SetName(GetEnglishName() + "-Mouse");
            DisableAnimation();
            DisableRaycast();
            return this;
        }
        
        
        public OnFieldEntity SetGridIconMode()
        {
            Sprite.GetComponentRoot()
                .SetTransparentMaterial()
                .SetSortingLayer("plant")
                .SetColliderState(false);
            SetLocalScale(new Vector3(1.025f, 1.025f, 1f));
            SetName(GetEnglishName() + "-Grid");
            DisableAnimation();
            DisableRaycast();
            return this;
        }
        
        
        public virtual OnFieldEntity SetPlaceMode(GridManager.Grid grid)
        {
            Sprite.GetComponentRoot()
                .SetSortingLayer("plant-" + grid.Point.y)
                .SetColliderState(true);
            SetLocalScale(new Vector3(1.025f, 1.025f, 1f));
            SetLocalPosition(new Vector3(grid.Position.x, grid.Position.y, 10f));
            SetName(GetEnglishName() + "-" + grid.Point.x + "-" + grid.Point.y);
            SetShadow();
            return this;
        }
        
        public OnFieldEntity SetShadow()
        {
            var shadowPosition = GetEntity<Plant>().shadowTransform.position;
            var shadowObject = Instantiate(MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow), shadowPosition, Quaternion.identity, Transform);
            var shadow = shadowObject.GetComponent<Shadow>();
            
            shadow.SetSize(0.7f);
            shadow.ToField();
            return this;
        }
        
        private static bool hasSunUnderPointer = false;
        private static GameObject currentTopObject = null;

        private void Update()
        {
            // base.Update();
            // PointerEventData pointerData = new PointerEventData(EventSystem.current)
            // {
            //     position = Input.mousePosition
            // };
            //
            // List<RaycastResult> results = new List<RaycastResult>();
            // EventSystem.current.RaycastAll(pointerData, results);
            //
            // hasSunUnderPointer = false;
            // currentTopObject = results.Count > 0 ? results[0].gameObject : null;
            //
            // foreach (var result in results)
            // {
            //     if (result.gameObject.CompareTag("Sun"))
            //     {
            //         hasSunUnderPointer = true;
            //         break;
            //     }
            // }
        }

        // public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        // {
        //     // 创建指针数据
        //     PointerEventData pointerData = new PointerEventData(EventSystem.current)
        //     {
        //         position = sp
        //     };
        //
        //     // 获取所有命中的对象（UI 和 2D 都行）
        //     List<RaycastResult> results = new List<RaycastResult>();
        //     EventSystem.current.RaycastAll(pointerData, results);
        //
        //     bool hasSun = false;
        //     foreach (var result in results)
        //     {
        //         if (result.gameObject.CompareTag("Sun"))
        //         {
        //             hasSun = true;
        //             break;
        //         }
        //     }
        //
        //     // 如果有 Sun，就只让 Sun 接收事件
        //     // 否则允许所有对象响应事件
        //     if (hasSun)
        //     {
        //         return gameObject.CompareTag("Sun");
        //     }
        //     else
        //     {
        //         return true;
        //     }
        // }
        
        // public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        // {
        //     
        //     if (MainGameManager.Instance.GetMouseStatus() == MainGameManager.MouseEvent.None)
        //     {
        //         if (hasSunUnderPointer)
        //         {
        //             // 如果当前有 Sun 且本对象不是 Sun，就不接收事件（变成透明）
        //             return gameObject.CompareTag("Sun");
        //         }
        //         // 没有 Sun，则谁都可以响应
        //         return true;
        //     }
        //     Debug.Log("IsRaycastLocationValid");
        //     return !gameObject.CompareTag("Sun");
        // }
    }
}