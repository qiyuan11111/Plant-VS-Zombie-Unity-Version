using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using Script;
using UnityEditor;
using UnityEngine;

public class PeaShooterSingleHeadGenerator : PeaShooterSingleGenerator
{
    // Start is called before the first frame update

    // 创建一个静态字符串数组，用来存储动画的名字
    private static string NAME = "PeaShooterSingle";
    // private static string[] props = {"position_x", "position_y", "scale_x", "scale_y", "skew_x", "skew_y"};
    // private static string[] ani_props = {"position.x", "position.y", "scale.x", "scale.y", "skew.x", "skew.y"};

    protected override string[] getClipNames()
    {
        return new[] { "head_idle", "shoot" };
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

