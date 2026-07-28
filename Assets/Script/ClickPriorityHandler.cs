using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Script
{
    public class ClickPriorityHandler : MonoBehaviour
    {
        [Header("优先级配置")]
        public string physicsPriorityTag = "Sun"; // 需优先处理的 2D 物体 Tag

        [Header("组件引用")]
        public GraphicRaycaster graphicRaycaster;
        public Physics2DRaycaster physicsRaycaster;
        public EventSystem eventSystem;
        
        private GameObject _lastHoveredObject;

        void Start()
        {
            // 自动获取组件（若未手动拖入）
            if (graphicRaycaster == null) graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
            if (physicsRaycaster == null) physicsRaycaster = FindObjectOfType<Physics2DRaycaster>();
            if (eventSystem == null) eventSystem = FindObjectOfType<EventSystem>();
        }

        void Update()
        {
            ProcessHoverEvents();
            if (Input.GetMouseButtonDown(0))
            {
                ProcessClickWithPriority();
            }
        }
        
        void ProcessHoverEvents()
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;

            // 1. 检测 UI 悬停
            List<RaycastResult> uiResults = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerData, uiResults);
            GameObject currentUIHover = uiResults.Count > 0 ? uiResults[0].gameObject : null;

            // 2. 检测 2D 物理悬停
            RaycastHit2D physicsHit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(pointerData.position), 
                Vector2.zero
            );
            GameObject currentPhysicsHover = physicsHit.collider?.gameObject;

            // 3. 优先级判断
            GameObject finalHoverTarget = currentPhysicsHover != null && 
                                          currentPhysicsHover.CompareTag(physicsPriorityTag) 
                ? currentPhysicsHover 
                : currentUIHover;

            // 4. 触发事件
            if (finalHoverTarget != _lastHoveredObject)
            {
                // 触发退出事件
                if (_lastHoveredObject)
                {
                    ExecuteEvents.Execute(_lastHoveredObject, pointerData, ExecuteEvents.pointerExitHandler);
                }

                // 触发进入事件
                if (finalHoverTarget != null)
                {
                    ExecuteEvents.Execute(finalHoverTarget, pointerData, ExecuteEvents.pointerEnterHandler);
                }

                _lastHoveredObject = finalHoverTarget;
            }
        }

        void ProcessClickWithPriority()
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;

            // 1. 检测 UI 点击
            List<RaycastResult> uiResults = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerData, uiResults);

            // 2. 检测 2D 物理点击
            RaycastHit2D physicsHit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(Input.mousePosition), 
                Vector2.zero
            );

            // 3. 判断优先级
            bool isPhysicsHitValid = physicsHit.collider != null;
            bool isUIHitValid = uiResults.Count > 0;

            if (isPhysicsHitValid && isUIHitValid)
            {
                // Debug.Log(physicsPriorityTag);
                // 当两者同时命中时，优先检查 2D 物体 Tag
                if (physicsHit.collider.CompareTag(physicsPriorityTag))
                {
                    HandlePhysicsClick(physicsHit.collider.gameObject);
                    return; // 阻断后续 UI 事件
                }
            }

            // 4. 默认处理（UI 优先或无优先级时）
            if (isUIHitValid)
            {
                HandleUIClick(uiResults[0].gameObject);
            }
            else if (isPhysicsHitValid)
            {
                HandlePhysicsClick(physicsHit.collider.gameObject);
            }
        }

        void HandleUIClick(GameObject uiObject)
        {
            // 触发 UI 点击事件（如按钮回调）
            ExecuteEvents.Execute(uiObject, new PointerEventData(eventSystem), ExecuteEvents.pointerClickHandler);
            // Debug.Log($"点击 UI: {uiObject.name}");
        }

        void HandlePhysicsClick(GameObject physicsObject)
        {
            // 触发 2D 物体点击事件（需实现 IPointerClickHandler）
            ExecuteEvents.Execute(physicsObject, new PointerEventData(eventSystem), ExecuteEvents.pointerClickHandler);
            // Debug.Log($"点击 2D 物体: {physicsObject.name} (Tag: {physicsObject.tag})");
        }
    }
}
