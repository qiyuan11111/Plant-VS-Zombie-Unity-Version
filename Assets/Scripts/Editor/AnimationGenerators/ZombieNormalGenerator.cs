using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using PvZ.Presentation;
using UnityEditor;
using UnityEngine;

public class ZombieNormalGenerator : AnimGenerator
{
    // Start is called before the first frame update

    // 创建一个静态字符串数组，用来存储动画的名字
    private static string NAME = "ZombieNormal";
    // private static string[] props = {"position_x", "position_y", "scale_x", "scale_y", "skew_x", "skew_y"};
    // private static string[] ani_props = {"position.x", "position.y", "scale.x", "scale.y", "skew.x", "skew.y"};

    protected override string[] getClipNames()
    {
        return new[] { "idle", "walk", "eat" };
    }

    public override Type getPropertyType(string prop)
    {
        if (prop.Equals("m_IsActive"))
        {
            return typeof(GameObject);
        }
        else if (prop.Equals("m_LocalPosition.x"))
        {
            return typeof(Transform);
        }
        return  typeof(SpriteTransform);
    }

    protected override string getPrefabName()
    {
        return NAME;
    }

}

