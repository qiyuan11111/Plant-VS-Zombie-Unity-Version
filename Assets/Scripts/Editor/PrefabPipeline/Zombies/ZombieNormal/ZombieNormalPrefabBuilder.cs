using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using PvZ.Config;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Zombies;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Zombies.ZombieNormal
{
public static partial class ZombieNormalPrefabBuilder
{
    private const string BasePath = "Assets/Prefab/Zombie/ZombieNormal";
    private const string SpritePath = BasePath + "/Sprite";
    private const string AnimationPath = BasePath + "/Animation";
    private const string IdleXmlPath = "Assets/StreamingAssets/Zombie_anim_idle1.xml";
    private const string WalkXmlPath = "Assets/StreamingAssets/Zombie_anim_walk1.xml";
    private const string IdlePath = AnimationPath + "/idle.anim";
    private const string WalkPath = AnimationPath + "/walk.anim";
    private const string ControllerPath = AnimationPath + "/ZombieNormal.controller";
    private const string PrefabPath = BasePath + "/ZombieNormal.prefab";
    private const string SharedMaterialPath = "Assets/Material/LightnessSkew.mat";
    private const string GameConfigPath = "Assets/Resources/GameConfigObject.asset";
    private const string SortingLayer = "zombie-0";
    private const string ComponentPath = "component";
    private const string BasicPath = ComponentPath + "/basic";
    private const string BasicVisualPath = BasicPath + "/" + SpriteTransform.NativeContentName;
    private const string AnchorsPath = ComponentPath + "/anchors";
    private const string ShadowAnchorPath = AnchorsPath + "/shadow";
    private const string PartPathPrefix = BasicVisualPath + "/";
    private static readonly Vector2 ColliderOffset = new(-8.125f, 2.475f);
    private static readonly Vector2 ColliderSize = new(45f, 105f);
    // Stable center between the two animated feet. This is only used when the
    // anchor is first created; later prefab rebuilds preserve authored tuning.
    private static readonly Vector2 DefaultShadowAnchorLocalPosition = new(12.75f, -50f);

    private readonly struct Part
    {
        public Part(string name, int sortingOrder)
        {
            Name = name;
            SortingOrder = sortingOrder;
        }

        public string Name { get; }
        public int SortingOrder { get; }
    }

    // Reanimation track order is also its back-to-front draw order.
    private static readonly Part[] Parts =
    {
        new("Zombie_innerarm_hand", 12),
        new("Zombie_innerarm_lower", 13),
        new("Zombie_innerarm_upper", 14),
        new("Zombie_head", 18),
        new("Zombie_innerleg_upper", 19),
        new("Zombie_innerleg_lower", 20),
        new("Zombie_innerleg_foot", 21),
        new("Zombie_outerleg_upper", 22),
        new("Zombie_outerleg_foot", 23),
        new("Zombie_outerleg_lower", 24),
        new("Zombie_body", 25),
        new("Zombie_tie", 28),
        new("Zombie_jaw", 29),
        new("Zombie_outerarm_hand", 35),
        new("Zombie_outerarm_upper", 36),
        new("Zombie_outerarm_lower", 38),
    };

    [MenuItem("Tools/PvZ/Build Normal Zombie Prefab")]
    public static void Build()
    {
        EnsureFolders();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureTextureImporters();

        var idle = CreateReanimationClip(IdleXmlPath, IdlePath, "idle", false);
        var walk = CreateReanimationClip(WalkXmlPath, WalkPath, "walk", true);
        var controller = CreateController(idle, walk);
        var prefab = CreatePrefab(idle, controller);
        ConnectGameConfig(prefab);
        Validate(prefab, idle, walk, controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"Normal zombie prefab built successfully: {PrefabPath}", prefab);
    }

    [MenuItem("Tools/PvZ/Verify Normal Zombie Walk Root Motion")]
    public static void VerifyWalkRootMotionPlayback()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new MissingReferenceException($"Missing prefab: {PrefabPath}");

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) throw new InvalidOperationException("Failed to instantiate ZombieNormal for verification.");

        try
        {
            var animator = instance.GetComponent<Animator>();
            if (animator == null) throw new MissingComponentException("ZombieNormal Animator is missing.");

            animator.Rebind();
            animator.Play("walk", 0, 0f);
            animator.Update(0f);
            var startX = instance.transform.position.x;
            for (var frame = 0; frame < 48; frame++)
            {
                animator.Update(1f / 12f);
            }

            var endX = instance.transform.position.x;
            if (endX >= startX - 45f)
            {
                throw new InvalidOperationException(
                    $"Walk root motion did not accumulate across the loop: {startX} -> {endX}.");
            }

            Debug.Log($"ZombieNormal walk root motion verified: {startX} -> {endX} after 48 frames.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
}
