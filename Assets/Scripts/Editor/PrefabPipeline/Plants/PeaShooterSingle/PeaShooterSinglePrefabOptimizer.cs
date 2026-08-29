using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LitJson;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Plants.Presentation.Animation;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.AnimationAssetUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.PeaShooterSingle
{
public static partial class PeaShooterSinglePrefabOptimizer
{
    private const string PrefabPath = "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab";
    private const string SourcePath = "Assets/StreamingAssets/PeaShooterSingle.json";
    private const string IdleClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/idle.anim";
    private const string HeadIdleClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/head_idle.anim";
    private const string ShootClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/shoot.anim";
    private const string BlinkClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/blink.anim";
    private const string ControllerPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/PeaShooterSingle.controller";
    private const string Blink1SpritePath = "Assets/Prefab/Plant/PeaShooterSingle/Sprite/PeaShooter_blink1.png";
    private const string Blink2SpritePath = "Assets/Prefab/Plant/PeaShooterSingle/Sprite/PeaShooter_blink2.png";
    private const string NativeContent = SpriteTransform.NativeContentName;
    private const string BasicPath = "component/basic";
    private const string BasicVisualPath = BasicPath + "/" + NativeContent;
    private const string HeadAttachmentPath = BasicVisualPath + "/head";
    private const string HeadAttachmentVisualPath = HeadAttachmentPath + "/" + NativeContent;
    private const string HeadImagePath = HeadAttachmentVisualPath + "/head";
    private const string HeadVisualPath = HeadImagePath + "/" + NativeContent;
    private const string StalkVisualPath = BasicVisualPath + "/stalk/" + NativeContent;
    private const string LeafVisualPath = BasicVisualPath + "/leaf/" + NativeContent;
    private const string BackLeafVisualPath = LeafVisualPath + "/backleaf/" + NativeContent;
    private const string FrontLeafVisualPath = LeafVisualPath + "/frontleaf/" + NativeContent;
    private const string DetectPath = "detect";
    private const string DetectZombiePath = DetectPath + "/zombie";
    private const string OptimizationVersion = "pea-shooter-single-animation-v27-centered-root";
    private const float FrameRate = 12f;
    private const float IdleStateSpeed = 1.4f;
    private const float ShootStateSpeed = 2.8f;
    private const float SourceShootDelaySeconds = 0.32f;

    private static readonly Dictionary<string, string> PartPaths = new()
    {
        { "PeaShooterSingle_backleaf", BackLeafVisualPath + "/backleaf" },
        { "PeaShooterSingle_backleaf_left_tip", BackLeafVisualPath + "/left_tip" },
        { "PeaShooterSingle_backleaf_right_tip", BackLeafVisualPath + "/right_tip" },
        { "PeaShooterSingle_front_leaf", FrontLeafVisualPath + "/frontleaf" },
        { "PeaShooterSingle_frontleaf_left_tip", FrontLeafVisualPath + "/left_tip" },
        { "PeaShooterSingle_frontleaf_right_tip", FrontLeafVisualPath + "/right_tip" },
        { "PeaShooterSingle_head", HeadAttachmentPath },
        { "PeaShooterSingle_stalk_bottom", StalkVisualPath + "/bottom" },
        { "PeaShooterSingle_stalk_top", StalkVisualPath + "/top" },
        { "PeaShooterSingle_head/PeaShooterSingle_head", HeadImagePath },
        { "PeaShooterSingle_head/PeaShooterSingle_mouth", HeadAttachmentVisualPath + "/mouth" },
        { "PeaShooterSingle_head/PeaShooterSingle_sprout", HeadAttachmentVisualPath + "/sprout" }
    };

    private static readonly Dictionary<string, string> BlinkPartPaths = new()
    {
        { "PeaShooter_blink1", HeadVisualPath + "/blink/PeaShooter_blink1" },
        { "PeaShooter_blink2", HeadVisualPath + "/blink/PeaShooter_blink2" }
    };

    // PeaShooterSingle1.fla, anim_blink. These are absolute FLA transforms.
    // Native hierarchy converts them through the anim_face reference matrix,
    // leaving the generated Unity child transform reference-relative.
    private static readonly Vector2 Blink1Position = new(5.85f, -17.9f);
    private static readonly Vector2 Blink2Position = new(5.7699f, -17.967585f);
    private static readonly Vector2 Blink1Scale =
        new(55.54046630859375f, 55.54046630859375f);
    private static readonly Vector2 Blink2Scale = new(55.499268f, 55.499268f);

    [MenuItem("Tools/PvZ/Optimize PeaShooterSingle Prefab")]
    public static void OptimizePeaShooterSingle()
    {
        OptimizePeaShooterSingle(LoadSource());
    }

    public static void OptimizePeaShooterSingleFromCommandLine()
    {
        OptimizePeaShooterSingle();
    }

    private static void OptimizePeaShooterSingle(JsonData source)
    {
        EnsureSpriteImportSettings(Blink1SpritePath);
        EnsureSpriteImportSettings(Blink2SpritePath);

        var idleClip = GetOrCreateClip(IdleClipPath, "idle");
        var headIdleClip = GetOrCreateClip(HeadIdleClipPath, "head_idle");
        var shootClip = GetOrCreateClip(ShootClipPath, "shoot");
        var blinkClip = GetOrCreateClip(BlinkClipPath, "blink");

        SynchronizeClip(idleClip, source["idle"], true, Array.Empty<AnimationEvent>());
        SynchronizeClip(headIdleClip, source["head_idle"], true, Array.Empty<AnimationEvent>());
        SynchronizeClip(shootClip, source["shoot"], false, new[]
        {
            new AnimationEvent
            {
                time = SourceShootDelaySeconds * ShootStateSpeed,
                functionName = "ShootProjectilePea"
            }
        });
        ConfigureBlinkClip(blinkClip);

        var controller = ConfigureController(idleClip, headIdleClip, shootClip, blinkClip);
        ConfigurePrefab(controller, idleClip, headIdleClip);

        var importer = AssetImporter.GetAtPath(PrefabPath);
        importer.userData = BuildOptimizationMarker();
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(PrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PeaShooterSingle prefab, independent animation clips and source blink overlay synchronized.");
    }

    private static bool IsCurrent(JsonData source)
    {
        if (!SpriteImportSettingsAreCurrent(Blink1SpritePath) ||
            !SpriteImportSettingsAreCurrent(Blink2SpritePath))
        {
            return false;
        }

        var importer = AssetImporter.GetAtPath(PrefabPath);
        if (importer == null || importer.userData != BuildOptimizationMarker()) return false;

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        var headIdleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HeadIdleClipPath);
        var shootClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ShootClipPath);
        var blinkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BlinkClipPath);
        if (!ClipMatchesSource(idleClip, source["idle"], true, false) ||
            !ClipMatchesSource(headIdleClip, source["head_idle"], true, false) ||
            !ClipMatchesSource(shootClip, source["shoot"], false, true) ||
            !BlinkClipIsCurrent(blinkClip))
        {
            return false;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!ControllerIsCurrent(controller, idleClip, headIdleClip, shootClip, blinkClip))
        {
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var animator = prefab != null ? prefab.GetComponent<Animator>() : null;
        var head = prefab != null ? prefab.transform.Find(HeadAttachmentPath) : null;
        var headTransform = head != null ? head.GetComponent<SpriteTransform>() : null;
        var headAttachmentBasePose = GetParentReferencePose(idleClip, HeadAttachmentPath);
        var faceBasePose = GetParentReferencePose(headIdleClip, HeadImagePath);
        return animator != null && animator.runtimeAnimatorController == controller &&
               headTransform != null &&
               headTransform.providesChildSpritePosition &&
               headTransform.providesChildSpriteAffine &&
               headTransform.spritePosition == headAttachmentBasePose.Position &&
               headTransform.spriteScale == headAttachmentBasePose.Scale &&
               headTransform.spriteSkew == headAttachmentBasePose.Skew &&
               PartPaths.Values.All(path => prefab.transform.Find(path) != null) &&
               NativeHierarchyIsCurrent(prefab) &&
               FirstFrameMatchesPrefab(prefab.transform, idleClip) &&
               FirstFrameMatchesPrefab(prefab.transform, headIdleClip) &&
               PrefabBlinkIsCurrent(prefab, animator, faceBasePose) &&
               PrefabCollisionIsCurrent(prefab);
    }

    private static JsonData LoadSource()
    {
        var absolutePath = GetAbsoluteSourcePath();
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Missing PeaShooterSingle animation source data.", absolutePath);
        }

        return JsonMapper.ToObject(File.ReadAllText(absolutePath));
    }

    private static string GetAbsoluteSourcePath()
    {
        return Path.Combine(Application.dataPath, "StreamingAssets/PeaShooterSingle.json");
    }

    private static string BuildOptimizationMarker()
    {
        var sourceHash = ComputeSourceHash(File.ReadAllText(GetAbsoluteSourcePath()));
        return $"{OptimizationVersion}-{sourceHash}";
    }

    public static Hash128 ComputeSourceHash(string sourceText)
    {
        if (sourceText == null) throw new ArgumentNullException(nameof(sourceText));

        // Git may check the JSON out as CRLF on Windows and LF on other
        // platforms. Line endings do not change the animation data, so they
        // must not make the generated prefab look stale.
        var normalizedSource = sourceText
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
        return Hash128.Compute(normalizedSource);
    }

    private static void EnsureSpriteImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new MissingReferenceException($"Missing PeaShooter blink texture: {assetPath}");
        }

        if (SpriteImportSettingsAreCurrent(importer)) return;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        settings.spritePixelsPerUnit = 1f;
        settings.filterMode = FilterMode.Point;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static bool SpriteImportSettingsAreCurrent(string assetPath)
    {
        return SpriteImportSettingsAreCurrent(
            AssetImporter.GetAtPath(assetPath) as TextureImporter);
    }

    private static bool SpriteImportSettingsAreCurrent(TextureImporter importer)
    {
        if (importer == null) return false;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        return importer.textureType == TextureImporterType.Sprite &&
               importer.spriteImportMode == SpriteImportMode.Single &&
               Mathf.Approximately(importer.spritePixelsPerUnit, 1f) &&
               !importer.mipmapEnabled &&
               importer.alphaIsTransparency &&
               settings.spriteAlignment == (int)SpriteAlignment.Center &&
               settings.spritePivot == new Vector2(0.5f, 0.5f) &&
               settings.filterMode == FilterMode.Point;
    }

}
}
