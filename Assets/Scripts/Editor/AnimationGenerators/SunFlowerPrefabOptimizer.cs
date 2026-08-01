using System;
using System.Collections.Generic;
using Prefab.Plant.SunFlower.Script;
using Prefab.Plant.SunShroom.Script;
using Script;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SunFlowerPrefabOptimizer
{
    private const string PrefabPath = "Assets/Prefab/Plant/SunFlower/SunFlower.prefab";
    private const string IdleClipPath = "Assets/Prefab/Plant/SunFlower/Animation/idle.anim";
    private const string BlinkClipPath = "Assets/Prefab/Plant/SunFlower/Animation/blink.anim";
    private const string NoSunClipPath = "Assets/Prefab/Plant/SunFlower/Animation/nosun.anim";
    private const string SunClipPath = "Assets/Prefab/Plant/SunFlower/Animation/sun.anim";
    private const string ControllerPath = "Assets/Prefab/Plant/SunFlower/Animation/SunFlower.controller";
    private const string SharedMaterialPath = "Assets/Prefab/Plant/SunFlower/Material/LightnessSkew 1.mat";
    private const string Blink1SpritePath = "Assets/Prefab/Plant/SunFlower/Sprite/SunFlower_blink1.png";
    private const string Blink2SpritePath = "Assets/Prefab/Plant/SunFlower/Sprite/SunFlower_blink2.png";
    private const string LegacyHeadPath = "component/basic/head/SunFlower_head";
    private const string LegacyBlinkRootPath = "component/basic/head/blink";
    private const string FaceMotionPath = "component/basic/head/face";
    private const string OptimizationVersion = "sunflower-structure-v8-child-sprite-position";

    private static readonly Dictionary<string, string> PartPaths = new()
    {
        { "SunFlower_head", "component/basic/head/face/SunFlower_head" },
        { "SunFlower_toppetals", "component/basic/head/SunFlower_toppetals" },
        { "SunFlower_bottompetals", "component/basic/head/SunFlower_bottompetals" },
        { "SunFlower_rightpetal1", "component/basic/head/petals/right/SunFlower_rightpetal1" },
        { "SunFlower_rightpetal2", "component/basic/head/petals/right/SunFlower_rightpetal2" },
        { "SunFlower_rightpetal3", "component/basic/head/petals/right/SunFlower_rightpetal3" },
        { "SunFlower_rightpetal4", "component/basic/head/petals/right/SunFlower_rightpetal4" },
        { "SunFlower_rightpetal5", "component/basic/head/petals/right/SunFlower_rightpetal5" },
        { "SunFlower_rightpetal6", "component/basic/head/petals/right/SunFlower_rightpetal6" },
        { "SunFlower_rightpetal7", "component/basic/head/petals/right/SunFlower_rightpetal7" },
        { "SunFlower_rightpetal8", "component/basic/head/petals/right/SunFlower_rightpetal8" },
        { "SunFlower_rightpetal9", "component/basic/head/petals/right/SunFlower_rightpetal9" },
        { "SunFlower_leftpetal1", "component/basic/head/petals/left/SunFlower_leftpetal1" },
        { "SunFlower_leftpetal2", "component/basic/head/petals/left/SunFlower_leftpetal2" },
        { "SunFlower_leftpetal3", "component/basic/head/petals/left/SunFlower_leftpetal3" },
        { "SunFlower_leftpetal4", "component/basic/head/petals/left/SunFlower_leftpetal4" },
        { "SunFlower_leftpetal5", "component/basic/head/petals/left/SunFlower_leftpetal5" },
        { "SunFlower_leftpetal6", "component/basic/head/petals/left/SunFlower_leftpetal6" },
        { "SunFlower_leftpetal7", "component/basic/head/petals/left/SunFlower_leftpetal7" },
        { "SunFlower_leftpetal8", "component/basic/head/petals/left/SunFlower_leftpetal8" },
        { "SunFlower_stalk_top", "component/basic/stalk/top" },
        { "SunFlower_stalk_bottom", "component/basic/stalk/bottom" },
        { "SunFlower_frontleaf", "component/basic/leaf/frontleaf/frontleaf" },
        { "SunFlower_frontleaf_left_tip", "component/basic/leaf/frontleaf/left_tip" },
        { "SunFlower_frontleaf_right_tip", "component/basic/leaf/frontleaf/right_tip" },
        { "SunFlower_backleaf", "component/basic/leaf/backleaf/backleaf" },
        { "SunFlower_backleaf_left_tip", "component/basic/leaf/backleaf/left_tip" },
        { "SunFlower_backleaf_right_tip", "component/basic/leaf/backleaf/right_tip" }
    };

    private static readonly Dictionary<string, string> BlinkPartPaths = new()
    {
        { "SunFlower_blink1", "component/basic/head/face/blink/SunFlower_blink1" },
        { "SunFlower_blink2", "component/basic/head/face/blink/SunFlower_blink2" }
    };

    [InitializeOnLoadMethod]
    private static void SchedulePendingOptimization()
    {
        EditorApplication.delayCall += OptimizePendingAssets;
    }

    private static void OptimizePendingAssets()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += OptimizePendingAssets;
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        if (prefab == null || clip == null) return;

        if (AssetDatabase.LoadAssetAtPath<Sprite>(Blink1SpritePath) == null ||
            AssetDatabase.LoadAssetAtPath<Sprite>(Blink2SpritePath) == null)
        {
            EditorApplication.delayCall += OptimizePendingAssets;
            return;
        }

        var sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
        var requiresOptimization = prefab.GetComponent<SunFlower>() == null ||
                                   prefab.transform.Find("component/basic/head") == null ||
                                   HasInvalidSharedMaterial(prefab.transform, sharedMaterial) ||
                                   AssetImporter.GetAtPath(PrefabPath).userData != OptimizationVersion;
        if (!requiresOptimization)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path.StartsWith("SunFlower_", StringComparison.Ordinal) ||
                    binding.type == typeof(Transform) ||
                    binding.type == typeof(SpriteRenderer))
                {
                    requiresOptimization = true;
                    break;
                }
            }
        }

        if (!requiresOptimization) return;

        try
        {
            OptimizeSunFlower();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/PvZ/Optimize SunFlower Prefab")]
    public static void OptimizeSunFlower()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        if (clip == null) throw new InvalidOperationException($"Missing animation clip: {IdleClipPath}");

        var blinkClip = GetOrCreateClip(BlinkClipPath, "blink");
        var noSunClip = GetOrCreateClip(NoSunClipPath, "nosun");
        var sunClip = GetOrCreateClip(SunClipPath, "sun");
        ConfigureBlinkClip(blinkClip);
        ConfigureBrightnessClip(noSunClip, false);
        ConfigureBrightnessClip(sunClip, true);
        ConfigureAnimatorController(clip, noSunClip, sunClip, blinkClip);

        ConvertAnimationBindings(clip);
        MigrateHeadMotionBindings(clip);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildHierarchy(root.transform);
            ConfigureRoot(root);
            EnsureSharedMaterial(root.transform);
            ConfigureParts(root.transform, clip);
            ConfigureFaceMotion(root.transform, clip);
            ConfigureBlinkParts(root.transform);
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

    private static void BuildHierarchy(Transform root)
    {
        var originalParts = new Dictionary<string, Transform>();
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (PartPaths.ContainsKey(child.name)) originalParts[child.name] = child;
        }

        foreach (var part in PartPaths)
        {
            var existingTarget = root.Find(part.Value);
            if (existingTarget != null) continue;

            if (!originalParts.TryGetValue(part.Key, out var partTransform))
            {
                throw new MissingReferenceException($"SunFlower prefab is missing part '{part.Key}'.");
            }

            var separator = part.Value.LastIndexOf('/');
            var parentPath = part.Value.Substring(0, separator);
            var targetName = part.Value.Substring(separator + 1);
            var parent = GetOrCreatePath(root, parentPath);

            partTransform.SetParent(parent, false);
            partTransform.name = targetName;
        }

        GetOrCreatePath(root, "component/anchors");

        var faceMotion = GetOrCreatePath(root, FaceMotionPath);
        var blinkRoot = root.Find(LegacyBlinkRootPath);
        if (blinkRoot != null && blinkRoot.parent != faceMotion)
        {
            blinkRoot.SetParent(faceMotion, false);
        }
        else
        {
            GetOrCreateChild(faceMotion, "blink");
        }
    }

    private static void ConfigureRoot(GameObject root)
    {
        root.layer = LayerMask.NameToLayer("Plant");
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var strayRenderer = root.GetComponent<SpriteRenderer>();
        if (strayRenderer != null) UnityEngine.Object.DestroyImmediate(strayRenderer);

        var sunFlower = GetOrAddComponent<SunFlower>(root);
        var producer = GetOrAddComponent<SunProducer>(root);
        var blink = GetOrAddComponent<SunFlowerBlink>(root);
        var animator = GetOrAddComponent<Animator>(root);
        var collider = GetOrAddComponent<BoxCollider2D>(root);
        GetOrAddComponent<SpriteGroup>(root);

        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        collider.isTrigger = false;
        collider.offset = new Vector2(0f, 4f);
        collider.size = new Vector2(40f, 60f);

        var anchors = GetOrCreatePath(root.transform, "component/anchors");
        var sunAnchor = GetOrCreateChild(anchors, "Sun_Anchor");
        sunAnchor.localPosition = new Vector3(0f, 25f, 0f);
        var shadowAnchor = GetOrCreateChild(anchors, "Shadow_Anchor");
        shadowAnchor.localPosition = new Vector3(0f, -17f, 0f);

        var producerData = new SerializedObject(producer);
        producerData.FindProperty("productionAnchor").objectReferenceValue = sunAnchor;
        producerData.FindProperty("initialDelaySeconds").floatValue = 10f;
        producerData.FindProperty("initialDelayVariationSeconds").floatValue = 2f;
        producerData.FindProperty("intervalSeconds").floatValue = 24f;
        producerData.FindProperty("intervalVariationSeconds").floatValue = 3f;
        producerData.FindProperty("fallbackSunType").enumValueIndex = 1;
        producerData.ApplyModifiedPropertiesWithoutUndo();

        var flowerData = new SerializedObject(sunFlower);
        flowerData.FindProperty("sunProducer").objectReferenceValue = producer;
        flowerData.FindProperty("blink").objectReferenceValue = blink;
        flowerData.FindProperty("shadowTransform").objectReferenceValue = shadowAnchor;
        flowerData.ApplyModifiedPropertiesWithoutUndo();

        var blinkData = new SerializedObject(blink);
        blinkData.FindProperty("animator").objectReferenceValue = animator;
        blinkData.FindProperty("blink1Sprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>(Blink1SpritePath);
        blinkData.FindProperty("blink2Sprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>(Blink2SpritePath);
        blinkData.FindProperty("minimumIntervalSeconds").floatValue = 2.5f;
        blinkData.FindProperty("maximumIntervalSeconds").floatValue = 5.5f;
        blinkData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureParts(Transform root, AnimationClip clip)
    {
        var bindingsByPath = new Dictionary<string, List<EditorCurveBinding>>();
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!bindingsByPath.TryGetValue(binding.path, out var bindings))
            {
                bindings = new List<EditorCurveBinding>();
                bindingsByPath.Add(binding.path, bindings);
            }

            bindings.Add(binding);
        }

        foreach (var targetPath in PartPaths.Values)
        {
            var part = root.Find(targetPath);
            if (part == null) throw new MissingReferenceException($"Missing optimized part '{targetPath}'.");

            SetLayerRecursively(part.gameObject, root.gameObject.layer);
            var spriteTransform = GetOrAddComponent<SpriteTransform>(part.gameObject);
            spriteTransform.position = Vector2.zero;
            spriteTransform.scale = new Vector2(100f, 100f);
            spriteTransform.skew = Vector2.zero;
            spriteTransform.brightness = 1f;
            spriteTransform.alpha = 1f;
            spriteTransform.alphaCoef = 1f;
            spriteTransform.updatePosition = true;
            spriteTransform.updateHierarchyScale = false;

            if (bindingsByPath.TryGetValue(targetPath, out var bindings))
            {
                foreach (var binding in bindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0) continue;

                    var value = curve.Evaluate(0f);
                    switch (binding.propertyName)
                    {
                        case "position.x": spriteTransform.position.x = value; break;
                        case "position.y": spriteTransform.position.y = value; break;
                        case "scale.x": spriteTransform.scale.x = value; break;
                        case "scale.y": spriteTransform.scale.y = value; break;
                        case "skew.x": spriteTransform.skew.x = value; break;
                        case "skew.y": spriteTransform.skew.y = value; break;
                        case "brightness": spriteTransform.brightness = value; break;
                        case "alpha": spriteTransform.alpha = value; break;
                        case "alphaCoef": spriteTransform.alphaCoef = value; break;
                    }
                }
            }

            spriteTransform.Apply();
            EditorUtility.SetDirty(spriteTransform);
        }
    }

    private static void ConfigureFaceMotion(Transform root, AnimationClip clip)
    {
        var faceMotion = root.Find(FaceMotionPath);
        var head = root.Find(PartPaths["SunFlower_head"]);
        if (faceMotion == null || head == null)
        {
            throw new MissingReferenceException("SunFlower face motion hierarchy is incomplete.");
        }

        var firstFramePosition = new Vector2(
            EvaluateFirstFrame(clip, FaceMotionPath, "position.x", 0f),
            EvaluateFirstFrame(clip, FaceMotionPath, "position.y", 0f));
        var firstFrameScale = new Vector2(
            EvaluateFirstFrame(clip, FaceMotionPath, "scale.x", 100f),
            EvaluateFirstFrame(clip, FaceMotionPath, "scale.y", 100f));

        var faceTransform = GetOrAddComponent<SpriteTransform>(faceMotion.gameObject);
        faceTransform.enabled = true;
        faceTransform.position = firstFramePosition;
        faceTransform.scale = firstFrameScale;
        faceTransform.skew = Vector2.zero;
        faceTransform.brightness = 1f;
        faceTransform.alpha = 1f;
        faceTransform.alphaCoef = 1f;
        faceTransform.updatePosition = true;
        faceTransform.providesChildSpritePosition = true;
        faceTransform.childSpritePosition = firstFramePosition;
        faceTransform.updateHierarchyScale = true;
        faceTransform.hierarchyScaleReference = firstFrameScale;
        faceTransform.Apply();

        var headTransform = GetOrAddComponent<SpriteTransform>(head.gameObject);
        headTransform.enabled = true;
        headTransform.position = Vector2.zero;
        headTransform.scale = firstFrameScale;
        headTransform.updatePosition = false;
        headTransform.updateHierarchyScale = false;
        head.localPosition = Vector3.zero;
        head.localScale = Vector3.one;
        headTransform.Apply();

        EditorUtility.SetDirty(faceTransform);
        EditorUtility.SetDirty(headTransform);
    }

    private static float EvaluateFirstFrame(
        AnimationClip clip,
        string path,
        string propertyName,
        float fallback)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.path != path ||
                binding.type != typeof(SpriteTransform) ||
                binding.propertyName != propertyName)
            {
                continue;
            }

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve != null && curve.length > 0) return curve.Evaluate(0f);
        }

        return fallback;
    }

    private static bool HasInvalidSharedMaterial(Transform root, Material sharedMaterial)
    {
        if (sharedMaterial == null) return true;

        foreach (var targetPath in EnumerateVisualPartPaths())
        {
            var target = root.Find(targetPath);
            var renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null || renderer.sharedMaterial != sharedMaterial)
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureSharedMaterial(Transform root)
    {
        var sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
        if (sharedMaterial == null)
        {
            throw new MissingReferenceException($"Missing shared SunFlower material: {SharedMaterialPath}");
        }

        foreach (var part in PartPaths)
        {
            var target = root.Find(part.Value);
            var renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null)
            {
                throw new MissingReferenceException($"SunFlower part '{part.Value}' requires a SpriteRenderer and Sprite.");
            }

            renderer.sharedMaterial = sharedMaterial;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void ConfigureBlinkParts(Transform root)
    {
        EnsureSpritePivot(Blink1SpritePath, new Vector2(1f, 0f));
        EnsureSpritePivot(Blink2SpritePath, new Vector2(1f, 0f));

        var sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
        var blink1 = AssetDatabase.LoadAssetAtPath<Sprite>(Blink1SpritePath);
        var blink2 = AssetDatabase.LoadAssetAtPath<Sprite>(Blink2SpritePath);
        if (sharedMaterial == null || blink1 == null || blink2 == null)
        {
            throw new MissingReferenceException("SunFlower blink sprites or shared material are missing.");
        }

        ConfigureBlinkPart(
            root,
            BlinkPartPaths["SunFlower_blink1"],
            blink1,
            sharedMaterial,
            new Vector2(39.1f, 31.5f));
        ConfigureBlinkPart(
            root,
            BlinkPartPaths["SunFlower_blink2"],
            blink2,
            sharedMaterial,
            new Vector2(39.1f, 31.5f));
    }

    private static void EnsureSpritePivot(string assetPath, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteAlignment == (int)SpriteAlignment.Custom &&
            settings.spritePivot == pivot)
        {
            return;
        }

        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void ConfigureBlinkPart(
        Transform root,
        string path,
        Sprite sprite,
        Material sharedMaterial,
        Vector2 animationPosition)
    {
        var target = GetOrCreatePath(root, path);
        SetLayerRecursively(target.gameObject, root.gameObject.layer);

        var renderer = GetOrAddComponent<SpriteRenderer>(target.gameObject);
        renderer.sprite = sprite;
        renderer.sharedMaterial = sharedMaterial;
        renderer.sortingOrder = 10;

        var spriteTransform = GetOrAddComponent<SpriteTransform>(target.gameObject);
        spriteTransform.enabled = true;
        spriteTransform.position = animationPosition;
        spriteTransform.scale = new Vector2(100f, 100f);
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = true;
        spriteTransform.updateHierarchyScale = false;
        spriteTransform.Apply();

        target.gameObject.SetActive(false);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(spriteTransform);
    }

    private static AnimationClip GetOrCreateClip(string path, string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;

        clip = new AnimationClip
        {
            name = clipName,
            frameRate = 12f
        };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void ConfigureBlinkClip(AnimationClip clip)
    {
        ClearClip(clip);
        clip.frameRate = 12f;

        SetActiveCurve(
            clip,
            BlinkPartPaths["SunFlower_blink1"],
            0f, 0f, 1f, 0f, 0f);
        SetActiveCurve(
            clip,
            BlinkPartPaths["SunFlower_blink2"],
            0f, 1f, 0f, 1f, 0f);

        SetClipLoopTime(clip, false);
        EditorUtility.SetDirty(clip);
    }

    private static void SetActiveCurve(AnimationClip clip, string path, params float[] values)
    {
        var curve = new AnimationCurve();
        for (var index = 0; index < values.Length; index++)
        {
            curve.AddKey(index / 12f, values[index]);
            AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
        }

        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"),
            curve);
    }

    private static void ConfigureBrightnessClip(AnimationClip clip, bool producesSun)
    {
        ClearClip(clip);
        clip.frameRate = 12f;

        var brightnessCurve = producesSun
            ? new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(1f, 2f, 0f, 0f),
                new Keyframe(2f, 1f, 0f, 0f))
            : new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));

        foreach (var path in EnumerateVisualPartPaths())
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(SpriteTransform), "brightness"),
                brightnessCurve);
        }

        if (producesSun)
        {
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent
                {
                    time = 1f,
                    functionName = "ProduceSun",
                    intParameter = 1
                }
            });
        }

        SetClipLoopTime(clip, !producesSun);
        EditorUtility.SetDirty(clip);
    }

    private static IEnumerable<string> EnumerateVisualPartPaths()
    {
        foreach (var path in PartPaths.Values) yield return path;
        foreach (var path in BlinkPartPaths.Values) yield return path;
    }

    private static void ClearClip(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationUtility.SetEditorCurve(clip, binding, null);
        }

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        }

        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
    }

    private static void SetClipLoopTime(AnimationClip clip, bool loopTime)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loopTime;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static void ConfigureAnimatorController(
        AnimationClip idleClip,
        AnimationClip noSunClip,
        AnimationClip sunClip,
        AnimationClip blinkClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        ConfigureParameters(controller);

        var baseLayer = controller.layers.Length > 0
            ? controller.layers[0]
            : GetOrCreateLayer(controller, "SunFlower");
        baseLayer.name = "SunFlower";
        baseLayer.defaultWeight = 1f;
        var idleState = GetOrCreateState(baseLayer.stateMachine, "idle");
        idleState.motion = idleClip;
        baseLayer.stateMachine.defaultState = idleState;
        SaveLayer(controller, baseLayer);

        ConfigureTriggeredLayer(
            controller,
            "SunFlower_sun",
            "nosun",
            noSunClip,
            "sun",
            sunClip,
            "produce");
        ConfigureTriggeredLayer(
            controller,
            "SunFlower_blink",
            "blink_idle",
            null,
            "blink",
            blinkClip,
            "blink");

        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureParameters(AnimatorController controller)
    {
        var parameters = new List<AnimatorControllerParameter>();
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name == "produce" || parameter.name == "blink") continue;
            parameters.Add(parameter);
        }

        parameters.Add(new AnimatorControllerParameter
        {
            name = "produce",
            type = AnimatorControllerParameterType.Trigger
        });
        parameters.Add(new AnimatorControllerParameter
        {
            name = "blink",
            type = AnimatorControllerParameterType.Trigger
        });
        controller.parameters = parameters.ToArray();
    }

    private static void ConfigureTriggeredLayer(
        AnimatorController controller,
        string layerName,
        string idleStateName,
        Motion idleMotion,
        string activeStateName,
        Motion activeMotion,
        string triggerName)
    {
        var layer = GetOrCreateLayer(controller, layerName);
        layer.defaultWeight = 1f;
        var stateMachine = layer.stateMachine;
        var idleState = GetOrCreateState(stateMachine, idleStateName);
        var activeState = GetOrCreateState(stateMachine, activeStateName);
        idleState.motion = idleMotion;
        activeState.motion = activeMotion;
        stateMachine.defaultState = idleState;

        ClearTransitions(idleState);
        ClearTransitions(activeState);

        var enterTransition = idleState.AddTransition(activeState);
        enterTransition.duration = 0f;
        enterTransition.hasExitTime = false;
        enterTransition.canTransitionToSelf = false;
        enterTransition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

        var exitTransition = activeState.AddTransition(idleState);
        exitTransition.duration = 0f;
        exitTransition.exitTime = 1f;
        exitTransition.hasExitTime = true;
        exitTransition.canTransitionToSelf = false;

        SaveLayer(controller, layer);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(idleState);
        EditorUtility.SetDirty(activeState);
    }

    private static AnimatorControllerLayer GetOrCreateLayer(AnimatorController controller, string layerName)
    {
        foreach (var layer in controller.layers)
        {
            if (layer.name == layerName) return layer;
        }

        var stateMachine = new AnimatorStateMachine
        {
            name = layerName
        };
        AssetDatabase.AddObjectToAsset(stateMachine, controller);
        var newLayer = new AnimatorControllerLayer
        {
            name = layerName,
            defaultWeight = 1f,
            stateMachine = stateMachine
        };
        controller.AddLayer(newLayer);
        return newLayer;
    }

    private static void SaveLayer(AnimatorController controller, AnimatorControllerLayer layer)
    {
        var layers = controller.layers;
        for (var index = 0; index < layers.Length; index++)
        {
            if (layers[index].stateMachine != layer.stateMachine) continue;

            layers[index] = layer;
            controller.layers = layers;
            return;
        }
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == stateName) return childState.state;
        }

        return stateMachine.AddState(stateName);
    }

    private static void ClearTransitions(AnimatorState state)
    {
        foreach (var transition in state.transitions)
        {
            state.RemoveTransition(transition);
            UnityEngine.Object.DestroyImmediate(transition, true);
        }
    }

    private static void ConfigureAnchors(Transform root)
    {
        var anchors = root.Find("component/anchors");
        SetLayerRecursively(anchors.gameObject, root.gameObject.layer);
    }

    private static void ConvertAnimationBindings(AnimationClip clip)
    {
        var converted = new List<(EditorCurveBinding Binding, AnimationCurve Curve)>();
        var oldBindings = AnimationUtility.GetCurveBindings(clip);

        foreach (var oldBinding in oldBindings)
        {
            if (!PartPaths.TryGetValue(oldBinding.path, out var targetPath))
            {
                // The optimizer is idempotent: already migrated bindings stay unchanged.
                converted.Add((oldBinding, AnimationUtility.GetEditorCurve(clip, oldBinding)));
                continue;
            }

            var curve = AnimationUtility.GetEditorCurve(clip, oldBinding);
            if (!TryConvertBinding(oldBinding, targetPath, curve, out var newBinding, out var newCurve))
            {
                continue;
            }

            converted.Add((newBinding, newCurve));
        }

        foreach (var binding in oldBindings)
        {
            AnimationUtility.SetEditorCurve(clip, binding, null);
        }

        foreach (var entry in converted)
        {
            AnimationUtility.SetEditorCurve(clip, entry.Binding, entry.Curve);
        }
    }

    private static void MigrateHeadMotionBindings(AnimationClip clip)
    {
        var migrations = new List<(EditorCurveBinding OldBinding, EditorCurveBinding NewBinding, AnimationCurve Curve)>();
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(SpriteTransform) ||
                (binding.path != LegacyHeadPath && binding.path != PartPaths["SunFlower_head"]) ||
                !IsInheritedFaceProperty(binding.propertyName))
            {
                continue;
            }

            var migratedBinding = binding;
            migratedBinding.path = FaceMotionPath;
            migrations.Add((binding, migratedBinding, AnimationUtility.GetEditorCurve(clip, binding)));
        }

        foreach (var migration in migrations)
        {
            AnimationUtility.SetEditorCurve(clip, migration.OldBinding, null);
            AnimationUtility.SetEditorCurve(clip, migration.NewBinding, migration.Curve);
        }
    }

    private static bool IsInheritedFaceProperty(string propertyName)
    {
        return propertyName == "position.x" ||
               propertyName == "position.y" ||
               propertyName == "scale.x" ||
               propertyName == "scale.y";
    }

    private static bool TryConvertBinding(
        EditorCurveBinding oldBinding,
        string targetPath,
        AnimationCurve curve,
        out EditorCurveBinding newBinding,
        out AnimationCurve newCurve)
    {
        newBinding = oldBinding;
        newBinding.path = targetPath;
        newCurve = curve;

        if (oldBinding.type == typeof(SpriteTransform)) return true;

        if (oldBinding.type == typeof(SpriteRenderer))
        {
            if (oldBinding.propertyName == "material._SkewX")
            {
                newBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(SpriteTransform), "skew.x");
                return true;
            }

            if (oldBinding.propertyName == "material._SkewY")
            {
                newBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(SpriteTransform), "skew.y");
                return true;
            }

            return true;
        }

        if (oldBinding.type != typeof(Transform)) return true;

        switch (oldBinding.propertyName)
        {
            case "m_LocalPosition.x":
                newBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(SpriteTransform), "position.x");
                return true;
            case "m_LocalPosition.y":
                newBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(SpriteTransform), "position.y");
                newCurve = ScaleCurve(curve, -1f);
                return true;
            case "m_LocalScale.x":
                newBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(SpriteTransform), "scale.x");
                newCurve = ScaleCurve(curve, 100f);
                return true;
            case "m_LocalScale.y":
                newBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(SpriteTransform), "scale.y");
                newCurve = ScaleCurve(curve, 100f);
                return true;
            case "m_LocalPosition.z":
            case "m_LocalScale.z":
                return false;
            default:
                return true;
        }
    }

    private static AnimationCurve ScaleCurve(AnimationCurve source, float factor)
    {
        var result = new AnimationCurve(source.keys)
        {
            preWrapMode = source.preWrapMode,
            postWrapMode = source.postWrapMode
        };
        var keys = result.keys;
        for (var i = 0; i < keys.Length; i++)
        {
            keys[i].value *= factor;
            keys[i].inTangent *= factor;
            keys[i].outTangent *= factor;
        }

        result.keys = keys;
        return result;
    }

    private static Transform GetOrCreatePath(Transform root, string path)
    {
        var current = root;
        foreach (var name in path.Split('/'))
        {
            current = GetOrCreateChild(current, name);
        }

        return current;
    }

    private static Transform GetOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null && child.parent == parent) return child;

        var childObject = new GameObject(name);
        child = childObject.transform;
        child.SetParent(parent, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
