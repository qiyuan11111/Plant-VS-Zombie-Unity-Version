using System.Collections.Generic;
using Script.InputModule;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Script
{
    public class CustomInputModule : StandardInputModule
    {
        protected override void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
        {
            
            if (pressed)
            {
                // 执行射线检测
                eventSystem.RaycastAll(pointerEvent, m_RaycastResultCache);
                // 自定义排序
                SortBySunTag(m_RaycastResultCache);
                // 更新射线检测结果
                if (m_RaycastResultCache.Count > 0)
                    pointerEvent.pointerCurrentRaycast = m_RaycastResultCache[0];
            }
            base.ProcessTouchPress(pointerEvent, pressed, released);
        }

        // public override void Process()
        // {
        //     Debug.Log("ProcessTouchPress");
        //     base.Process();
        // }

        private void SortBySunTag(List<RaycastResult> results)
        {
            results.Sort((a, b) =>
            {
                bool aIsSun = a.gameObject.CompareTag("Sun");
                bool bIsSun = b.gameObject.CompareTag("Sun");

                // 优先级规则：
                // 1. 有"Sun"标签的排在前
                // 2. 同为"Sun"或非"Sun"，按默认规则排序
                if (aIsSun && !bIsSun) return 1;
                if (!aIsSun && bIsSun) return -1;
                return 0;
                // 默认排序（如Sort Order、Depth等）
                // return RaycastResultComparer.Default.Compare(a, b);
            });
        }
    }
}