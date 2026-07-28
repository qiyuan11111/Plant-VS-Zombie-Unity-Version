using System.Collections.Generic;
using Script.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Script
{
    public class SunPriorityRaycaster : Physics2DRaycaster
    {
        private const string SUN_TAG = "Sun";

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            base.Raycast(eventData, resultAppendList);
        
            // Debug.Log(resultAppendList.Count);
            // 分离 "Sun" 和普通元素
            List<RaycastResult> sunResults = new List<RaycastResult>();
            List<RaycastResult> normalResults = new List<RaycastResult>();
            
            foreach (var result in resultAppendList)
            {
                if (result.gameObject.CompareTag(SUN_TAG))
                {
                    sunResults.Add(result);
                }
                else
                {
                    normalResults.Add(result);
                }
            }
            resultAppendList.Clear();
            if(MainGameManager.Instance.GetMouseStatus() == MainGameManager.MouseEvent.None)
                resultAppendList.AddRange(sunResults.Count > 0 ? sunResults : normalResults);
            else
                resultAppendList.AddRange(normalResults);
            // 清空原列表后按优先级重新合并
            
            // resultAppendList.AddRange(normalResults); // 其他元素在后
            // resultAppendList.AddRange(sunResults);    // "Sun" 元素在前
            
            
        }
    }
}
