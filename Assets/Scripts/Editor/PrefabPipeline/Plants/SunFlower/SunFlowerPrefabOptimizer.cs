using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.AnimationAssetUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.SunFlower
{
public static partial class SunFlowerPrefabOptimizer
{
    private const string PrefabPath = "Assets/Prefab/Plant/SunFlower/SunFlower.prefab";
    private const string IdleXmlPath = "Assets/StreamingAssets/SunFlower_anim_idle.xml";
    private const string IdleClipPath = "Assets/Prefab/Plant/SunFlower/Animation/idle.anim";
    private const string BlinkClipPath = "Assets/Prefab/Plant/SunFlower/Animation/blink.anim";
    private const string NoSunClipPath = "Assets/Prefab/Plant/SunFlower/Animation/nosun.anim";
    private const string SunClipPath = "Assets/Prefab/Plant/SunFlower/Animation/sun.anim";
    private const string ControllerPath = "Assets/Prefab/Plant/SunFlower/Animation/SunFlower.controller";
    private const string SharedMaterialPath = "Assets/Prefab/Plant/SunFlower/Material/LightnessSkew 1.mat";
    private const string Blink1SpritePath = "Assets/Prefab/Plant/SunFlower/Sprite/SunFlower_blink1.png";
    private const string Blink2SpritePath = "Assets/Prefab/Plant/SunFlower/Sprite/SunFlower_blink2.png";
    private const string NativeContent = SpriteTransform.NativeContentName;
    private const string BasicPath = "component/basic";
    private const string BasicVisualPath = BasicPath + "/" + NativeContent;
    private const string HeadPath = BasicVisualPath + "/head/SunFlower_head";
    private const string HeadVisualPath = HeadPath + "/" + NativeContent;
    private const string LegacyNestedHeadPath = "component/basic/head/face/SunFlower_head";
    private const string LegacyBlinkRootPath = "component/basic/head/blink";
    private const string LegacyNestedBlinkRootPath = "component/basic/head/face/blink";
    private const string LegacyFacePath = "component/basic/head/face";
    private const int DefaultPoseFrame = 5;
    private const string OptimizationVersion = "sunflower-structure-v20-centered-root";

    private static readonly Dictionary<string, string> PartPaths = new()
    {
        { "SunFlower_head", HeadPath },
        { "SunFlower_toppetals", BasicVisualPath + "/head/SunFlower_toppetals" },
        { "SunFlower_bottompetals", BasicVisualPath + "/head/SunFlower_bottompetals" },
        { "SunFlower_rightpetal1", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal1" },
        { "SunFlower_rightpetal2", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal2" },
        { "SunFlower_rightpetal3", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal3" },
        { "SunFlower_rightpetal4", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal4" },
        { "SunFlower_rightpetal5", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal5" },
        { "SunFlower_rightpetal6", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal6" },
        { "SunFlower_rightpetal7", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal7" },
        { "SunFlower_rightpetal8", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal8" },
        { "SunFlower_rightpetal9", BasicVisualPath + "/head/petals/right/SunFlower_rightpetal9" },
        { "SunFlower_leftpetal1", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal1" },
        { "SunFlower_leftpetal2", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal2" },
        { "SunFlower_leftpetal3", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal3" },
        { "SunFlower_leftpetal4", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal4" },
        { "SunFlower_leftpetal5", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal5" },
        { "SunFlower_leftpetal6", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal6" },
        { "SunFlower_leftpetal7", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal7" },
        { "SunFlower_leftpetal8", BasicVisualPath + "/head/petals/left/SunFlower_leftpetal8" },
        { "SunFlower_stalk_top", BasicVisualPath + "/stalk/top" },
        { "SunFlower_stalk_bottom", BasicVisualPath + "/stalk/bottom" },
        { "SunFlower_frontleaf", BasicVisualPath + "/leaf/frontleaf/frontleaf" },
        { "SunFlower_frontleaf_left_tip", BasicVisualPath + "/leaf/frontleaf/left_tip" },
        { "SunFlower_frontleaf_right_tip", BasicVisualPath + "/leaf/frontleaf/right_tip" },
        { "SunFlower_backleaf", BasicVisualPath + "/leaf/backleaf/backleaf" },
        { "SunFlower_backleaf_left_tip", BasicVisualPath + "/leaf/backleaf/left_tip" },
        { "SunFlower_backleaf_right_tip", BasicVisualPath + "/leaf/backleaf/right_tip" }
    };

    private static readonly Dictionary<string, string> BlinkPartPaths = new()
    {
        { "SunFlower_blink1", HeadVisualPath + "/blink/SunFlower_blink1" },
        { "SunFlower_blink2", HeadVisualPath + "/blink/SunFlower_blink2" }
    };

    [MenuItem("Tools/PvZ/Optimize SunFlower Prefab")]
    public static void OptimizeSunFlower()
    {
        var clip = CreateIdleClipFromXml();

        var blinkClip = GetOrCreateClip(BlinkClipPath, "blink", 12f);
        var noSunClip = GetOrCreateClip(NoSunClipPath, "nosun", 12f);
        var sunClip = GetOrCreateClip(SunClipPath, "sun", 12f);
        ConfigureBlinkClip(blinkClip);
        ConfigureBrightnessClip(noSunClip, false);
        ConfigureBrightnessClip(sunClip, true);
        ConfigureAnimatorController(clip, noSunClip, sunClip, blinkClip);

        ConvertAnimationBindings(clip);
        MigrateHeadAnimationBindings(clip);
        MigrateBasicContentBindings(clip);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildHierarchy(root.transform);
            ConfigureRoot(root);
            EnsureSharedMaterial(root.transform);
            ConfigureParts(root.transform, clip);
            ConfigureHeadMotion(root.transform, clip);
            ConfigureBlinkParts(root.transform);
            MigrateToNativeHierarchy(root.transform);
            ConfigureAnchors(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        EditorUtility.SetDirty(clip);
        var prefabImporter = AssetImporter.GetAtPath(PrefabPath);
        prefabImporter.userData = OptimizationVersion;
        EditorUtility.SetDirty(prefabImporter);
        AssetDatabase.WriteImportSettingsIfDirty(PrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SunFlower prefab hierarchy, production glow, blink layer and animation bindings optimized.");
    }

    public static void OptimizeSunFlowerFromCommandLine()
    {
        OptimizeSunFlower();
    }

}
}
