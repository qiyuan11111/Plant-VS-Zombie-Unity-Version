using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PriorityTagRaycaster : BaseRaycaster 
{
    
    private static readonly string[] _tagPriority = {
        "Sun",
        "Untagged",
    };

    public override Camera eventCamera {
        get { return Camera.main; } // 根据需要调整
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
        // 1. 收集所有可点击对象（UI 和 2D 物体）
        List<GameObject> allObjects = new List<GameObject>();

        // 检测 UI
        List<RaycastResult> uiResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, uiResults);
        allObjects.AddRange(uiResults.Select(r => r.gameObject));

        // 检测 2D 物体
        RaycastHit2D[] physicsResults = Physics2D.RaycastAll(
            eventCamera.ScreenToWorldPoint(eventData.position),
            Vector2.zero
        );
        allObjects.AddRange(physicsResults.Select(r => r.collider.gameObject));
        
        Debug.Log(allObjects.Count);

        // 2. 按 Tag 优先级排序
        var sortedObjects = allObjects
            .OrderBy(obj => GetTagPriorityIndex(obj.tag))
            .Select(obj => new RaycastResult {
                gameObject = obj,
                module = this,
                screenPosition = eventData.position,
                distance = 0 // 根据实际需求设置距离
            });

        // 3. 将排序后的结果添加至 EventSystem
        resultAppendList.AddRange(sortedObjects);
    }

    private int GetTagPriorityIndex(string tag) {
        for (int i = 0; i < _tagPriority.Length; i++) {
            if (tag == _tagPriority[i]) return i;
        }
        return _tagPriority.Length; // 未定义 Tag 的优先级最低
    }
}
