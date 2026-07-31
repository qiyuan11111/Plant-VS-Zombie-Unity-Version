using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using Script;
using UnityEditor;
using UnityEngine;

public class PeaShooterSingleGenerator : AnimGenerator
{
    // Start is called before the first frame update

    // 创建一个静态字符串数组，用来存储动画的名字
    private static string NAME = "PeaShooterSingle";
    // private static string[] props = {"position_x", "position_y", "scale_x", "scale_y", "skew_x", "skew_y"};
    // private static string[] ani_props = {"position.x", "position.y", "scale.x", "scale.y", "skew.x", "skew.y"};

    protected override string[] getClipNames()
    {
        return new[] { "shoot", "head_idle" };
    }

    protected override string getAnimationPath(string objectName)
    {
        return objectName switch
        {
            "PeaShooterSingle_backleaf" => "component/basic/leaf/backleaf/backleaf",
            "PeaShooterSingle_backleaf_left_tip" => "component/basic/leaf/backleaf/left_tip",
            "PeaShooterSingle_backleaf_right_tip" => "component/basic/leaf/backleaf/right_tip",
            "PeaShooterSingle_front_leaf" => "component/basic/leaf/frontleaf/frontleaf",
            "PeaShooterSingle_frontleaf_left_tip" => "component/basic/leaf/frontleaf/left_tip",
            "PeaShooterSingle_frontleaf_right_tip" => "component/basic/leaf/frontleaf/right_tip",
            "PeaShooterSingle_head" => "component/basic/head",
            "PeaShooterSingle_stalk_bottom" => "component/basic/stalk/bottom",
            "PeaShooterSingle_stalk_top" => "component/basic/stalk/top",
            "PeaShooterSingle_head/PeaShooterSingle_head" => "component/basic/head/pod/head",
            "PeaShooterSingle_head/PeaShooterSingle_mouth" => "component/basic/head/pod/mouth",
            "PeaShooterSingle_head/PeaShooterSingle_sprout" => "component/basic/head/sprout",
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

