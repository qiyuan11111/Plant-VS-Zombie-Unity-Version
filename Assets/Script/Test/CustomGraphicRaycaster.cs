using System.Collections;
using System.Collections.Generic;
using Script.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomGraphicRaycaster : GraphicRaycaster
{
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        base.Raycast(eventData, resultAppendList);
    }
}
