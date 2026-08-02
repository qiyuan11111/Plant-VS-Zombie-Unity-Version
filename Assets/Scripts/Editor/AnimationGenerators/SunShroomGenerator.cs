using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using Script;
using UnityEditor;
using UnityEngine;

public class SunShroomGenerator : AnimGenerator
{
    // Start is called before the first frame update

    // 创建一个静态字符串数组，用来存储动画的名字
    private static string NAME = "SunShroom";
    // private static string[] props = {"position_x", "position_y", "scale_x", "scale_y", "skew_x", "skew_y"};
    // private static string[] ani_props = {"position.x", "position.y", "scale.x", "scale.y", "skew.x", "skew.y"};

    protected override string[] getClipNames()
    {
        return new[] { "idle", "sleep" };
    }

    protected override string getAnimationPath(string objectName)
    {
        return objectName switch
        {
            "SunShroom_head" => "component/basic/head/SunShroom_head",
            "SunShroom_body" => "component/basic/body/SunShroom_body",
            "SunShroom_sleep" => "component/basic/body/SunShroom_body/SunShroom_sleep",
            _ => base.getAnimationPath(objectName)
        };
    }

    public override Type getPropertyType(string prop)
    {
        // Debug.Log(prop);
        return prop.Equals("m_IsActive") ? typeof(GameObject) : typeof(SpriteTransform);
    }

    protected override string getPrefabName()
    {
        return NAME;
    }

}
