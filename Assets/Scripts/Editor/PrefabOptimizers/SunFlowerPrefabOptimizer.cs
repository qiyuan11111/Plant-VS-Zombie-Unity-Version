using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ComponentUtility = UnityEditorInternal.ComponentUtility;

public static class SunFlowerPrefabOptimizer
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

        var blinkClip = GetOrCreateClip(BlinkClipPath, "blink");
        var noSunClip = GetOrCreateClip(NoSunClipPath, "nosun");
        var sunClip = GetOrCreateClip(SunClipPath, "sun");
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

    private static AnimationClip CreateIdleClipFromXml()
    {
        var xmlAbsolutePath = Path.Combine(
            Application.dataPath,
            IdleXmlPath.Substring("Assets/".Length));
        if (!File.Exists(xmlAbsolutePath))
        {
            throw new FileNotFoundException("Missing SunFlower idle XML.", xmlAbsolutePath);
        }

        var document = new XmlDocument();
        document.Load(xmlAbsolutePath);
        var layers = document.SelectNodes("/animate/layer");
        if (layers == null || layers.Count == 0)
        {
            throw new InvalidDataException($"SunFlower idle XML has no animation layers: {IdleXmlPath}");
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, IdleClipPath);
        }

        ClearClip(clip);
        clip.name = "idle";
        clip.frameRate = 12f;
        var generatedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (XmlNode layer in layers)
        {
            var layerName = layer.Attributes?["name"]?.Value;
            var frames = layer.SelectNodes("frame");
            if (string.IsNullOrEmpty(layerName) || frames == null || frames.Count == 0)
            {
                throw new InvalidDataException("SunFlower idle XML contains an unnamed or empty layer.");
            }

            var targetPath = ResolveIdleLayerPath(layerName);
            if (!generatedPaths.Add(targetPath))
            {
                throw new InvalidDataException($"SunFlower idle XML maps more than one layer to '{targetPath}'.");
            }

            SetLinearCurve(clip, targetPath, "position.x", CreateXmlCurve(frames, "posx"));
            SetLinearCurve(clip, targetPath, "position.y", CreateXmlCurve(frames, "posy"));
            SetLinearCurve(clip, targetPath, "scale.x", CreateXmlCurve(frames, "scalex"));
            SetLinearCurve(clip, targetPath, "scale.y", CreateXmlCurve(frames, "scaley"));
            SetLinearCurve(clip, targetPath, "skew.x", CreateXmlCurve(frames, "skewx"));
            SetLinearCurve(clip, targetPath, "skew.y", CreateXmlCurve(frames, "skewy"));
        }

        var expectedPaths = new HashSet<string>(PartPaths.Values, StringComparer.Ordinal);
        if (!generatedPaths.SetEquals(expectedPaths))
        {
            var missing = string.Join(", ", expectedPaths.Except(generatedPaths));
            var unexpected = string.Join(", ", generatedPaths.Except(expectedPaths));
            throw new InvalidDataException(
                $"SunFlower idle XML layer mapping is incomplete. Missing: [{missing}]. Unexpected: [{unexpected}].");
        }

        SetClipLoopTime(clip, true);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
        return clip;
    }

    private static string ResolveIdleLayerPath(string layerName)
    {
        if (PartPaths.TryGetValue(layerName, out var targetPath)) return targetPath;

        var partName = layerName switch
        {
            "anim_idle" => "SunFlower_head",
            "frontleaf" => "SunFlower_frontleaf",
            "frontleaf_left_tip" => "SunFlower_frontleaf_left_tip",
            "frontleaf_right_tip" => "SunFlower_frontleaf_right_tip",
            "backleaf" => "SunFlower_backleaf",
            "backleaf_left_tip" => "SunFlower_backleaf_left_tip",
            "backleaf_right_tip" => "SunFlower_backleaf_right_tip",
            "stalk_top" => "SunFlower_stalk_top",
            "stalk_bottom" => "SunFlower_stalk_bottom",
            _ => null
        };

        if (partName != null && PartPaths.TryGetValue(partName, out targetPath)) return targetPath;
        throw new InvalidDataException($"Unknown SunFlower idle XML layer: {layerName}");
    }

    private static AnimationCurve CreateXmlCurve(XmlNodeList frames, string attribute)
    {
        var keys = new Keyframe[frames.Count];
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            keys[index] = new Keyframe(
                ParseXmlFloat(frame, "index") / 12f,
                ParseXmlFloat(frame, attribute));
        }

        return new AnimationCurve(keys);
    }

    private static float ParseXmlFloat(XmlNode node, string attribute)
    {
        var text = node.Attributes?[attribute]?.Value;
        if (string.IsNullOrEmpty(text) ||
            !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidDataException(
                $"SunFlower idle XML frame is missing a valid '{attribute}' value.");
        }

        return value;
    }

    private static void SetLinearCurve(
        AnimationClip clip,
        string path,
        string propertyName,
        AnimationCurve curve)
    {
        for (var index = 0; index < curve.length; index++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
        }

        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(SpriteTransform), propertyName),
            curve);
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

            var legacyTargetPath = BasicPath + part.Value.Substring(BasicVisualPath.Length);
            var partTransform = root.Find(legacyTargetPath);
            if (partTransform == null && !originalParts.TryGetValue(part.Key, out partTransform))
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

        var head = root.Find(HeadPath);
        if (head == null)
        {
            throw new MissingReferenceException("SunFlower head hierarchy is incomplete.");
        }

        ConsolidateBlinkHierarchy(root, head);

        var legacyFace = root.Find(LegacyFacePath);
        if (legacyFace == null) return;

        var legacyFaceTransform = legacyFace.GetComponent<SpriteTransform>();
        if (legacyFaceTransform != null)
        {
            UnityEngine.Object.DestroyImmediate(legacyFaceTransform);
        }

        if (legacyFace.childCount == 0)
        {
            UnityEngine.Object.DestroyImmediate(legacyFace.gameObject);
        }
    }

    private static void ConsolidateBlinkHierarchy(Transform root, Transform head)
    {
        var nativeHeadContent = head.Find(NativeContent);
        var blinkRoots = root.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name == "blink")
            .ToList();
        var blinkRoot = nativeHeadContent?.Find("blink") ??
                        head.Find("blink") ??
                        root.Find(LegacyNestedBlinkRootPath) ??
                        root.Find(LegacyBlinkRootPath) ??
                        blinkRoots.FirstOrDefault();
        if (blinkRoot == null)
        {
            blinkRoot = GetOrCreateChild(head, "blink");
            blinkRoots.Add(blinkRoot);
        }
        else if (blinkRoot.parent != head && blinkRoot.parent != nativeHeadContent)
        {
            blinkRoot.SetParent(head, false);
        }

        foreach (var sourceName in BlinkPartPaths.Keys)
        {
            var candidates = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == sourceName)
                .ToArray();
            var selected = candidates.FirstOrDefault(item =>
                               item.GetComponent<SpriteTransform>()?.NativeContent != null) ??
                           candidates.FirstOrDefault();
            if (selected != null && selected.parent != blinkRoot)
            {
                selected.SetParent(blinkRoot, false);
            }

            foreach (var duplicate in candidates)
            {
                if (duplicate != selected)
                {
                    UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                }
            }
        }

        foreach (var duplicateRoot in blinkRoots)
        {
            if (duplicateRoot != blinkRoot)
            {
                UnityEngine.Object.DestroyImmediate(duplicateRoot.gameObject);
            }
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
        var blink = GetOrAddComponent<Blink>(root);
        var animator = GetOrAddComponent<Animator>(root);
        var collider = GetOrAddComponent<BoxCollider2D>(root);
        GetOrAddComponent<SpriteGroup>(root);

        var basic = GetOrCreatePath(root.transform, BasicPath);
        ConfigureCenteredAnimationRoot(basic);

        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        collider.isTrigger = false;
        collider.offset = new Vector2(0f, 4f);
        collider.size = new Vector2(40f, 60f);

        var anchors = GetOrCreatePath(root.transform, "component/anchors");
        var sunAnchor = GetOrCreateChild(anchors, "Sun_Anchor");
        sunAnchor.localPosition = Vector3.zero;
        var legacyShadowAnchor = anchors.Find("Shadow_Anchor");
        if (legacyShadowAnchor != null)
        {
            UnityEngine.Object.DestroyImmediate(legacyShadowAnchor.gameObject);
        }

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
        flowerData.FindProperty("shadowLocalPosition").vector2Value = new Vector2(-0.4f, -26.4f);
        flowerData.ApplyModifiedPropertiesWithoutUndo();

        var blinkData = new SerializedObject(blink);
        blinkData.FindProperty("animator").objectReferenceValue = animator;
        blinkData.FindProperty("minimumIntervalSeconds").floatValue = 2.5f;
        blinkData.FindProperty("maximumIntervalSeconds").floatValue = 5.5f;
        blinkData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureParts(Transform root, AnimationClip clip)
    {
        var defaultPoseTime = GetDefaultPoseTime(clip);
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
            if (bindingsByPath.TryGetValue(targetPath, out var bindings))
            {
                foreach (var binding in bindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0) continue;

                    var value = curve.Evaluate(defaultPoseTime);
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

    private static void ConfigureHeadMotion(Transform root, AnimationClip clip)
    {
        var head = root.Find(HeadPath);
        if (head == null)
        {
            throw new MissingReferenceException("SunFlower head hierarchy is incomplete.");
        }

        var defaultPoseTime = GetDefaultPoseTime(clip);
        var defaultPosePosition = new Vector2(
            EvaluateFrame(clip, HeadPath, "position.x", defaultPoseTime, 0f),
            EvaluateFrame(clip, HeadPath, "position.y", defaultPoseTime, 0f));
        var defaultPoseScale = new Vector2(
            EvaluateFrame(clip, HeadPath, "scale.x", defaultPoseTime, 100f),
            EvaluateFrame(clip, HeadPath, "scale.y", defaultPoseTime, 100f));
        var defaultPoseSkew = new Vector2(
            EvaluateFrame(clip, HeadPath, "skew.x", defaultPoseTime, 0f),
            EvaluateFrame(clip, HeadPath, "skew.y", defaultPoseTime, 0f));

        var headTransform = GetOrAddComponent<SpriteTransform>(head.gameObject);
        headTransform.enabled = true;
        headTransform.position = defaultPosePosition;
        headTransform.scale = defaultPoseScale;
        headTransform.updatePosition = true;
        headTransform.providesChildSpritePosition = true;
        headTransform.providesChildSpriteAffine = true;
        headTransform.spritePosition = defaultPosePosition;
        headTransform.spriteScale = defaultPoseScale;
        headTransform.spriteSkew = defaultPoseSkew;
        head.localRotation = Quaternion.identity;
        head.localScale = Vector3.one;
        headTransform.Apply();

        EditorUtility.SetDirty(headTransform);
    }

    private static void ConfigureCenteredAnimationRoot(Transform target)
    {
        var spriteTransform = GetOrAddComponent<SpriteTransform>(target.gameObject);
        spriteTransform.enabled = true;
        spriteTransform.position = Vector2.zero;
        spriteTransform.scale = new Vector2(100f, 100f);
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = false;
        spriteTransform.providesChildSpritePosition = false;
        spriteTransform.providesChildSpriteAffine = false;
        spriteTransform.spritePosition = Vector2.zero;
        spriteTransform.spriteScale = new Vector2(100f, 100f);
        spriteTransform.spriteSkew = Vector2.zero;
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
        EditorUtility.SetDirty(spriteTransform);
    }

    private static float GetDefaultPoseTime(AnimationClip clip)
    {
        return clip.frameRate > 0f
            ? Mathf.Max(0, DefaultPoseFrame - 1) / clip.frameRate
            : 0f;
    }

    private static float EvaluateFrame(
        AnimationClip clip,
        string path,
        string propertyName,
        float time,
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
            if (curve != null && curve.length > 0) return curve.Evaluate(time);
        }

        return fallback;
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
            var spriteTransform = target != null ? target.GetComponent<SpriteTransform>() : null;
            var renderer = spriteTransform != null
                ? spriteTransform.VisualRenderer
                : target != null ? target.GetComponent<SpriteRenderer>() : null;
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
        EnsureCenteredSpritePivot(Blink1SpritePath);
        EnsureCenteredSpritePivot(Blink2SpritePath);

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
            new Vector2(2.57259f, -16.20185f),
            new Vector2(80f, 80.56224f),
            16);
        ConfigureBlinkPart(
            root,
            BlinkPartPaths["SunFlower_blink2"],
            blink2,
            sharedMaterial,
            new Vector2(2.57259f, -16.20185f),
            new Vector2(80f, 80.56224f),
            15);
    }

    private static void EnsureCenteredSpritePivot(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        var centerPivot = new Vector2(0.5f, 0.5f);
        if (settings.spriteAlignment == (int)SpriteAlignment.Center &&
            settings.spritePivot == centerPivot)
        {
            return;
        }

        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = centerPivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void ConfigureBlinkPart(
        Transform root,
        string path,
        Sprite sprite,
        Material sharedMaterial,
        Vector2 animationPosition,
        Vector2 animationScale,
        int sortingOrder)
    {
        var target = GetOrCreatePath(root, path);
        SetLayerRecursively(target.gameObject, root.gameObject.layer);

        var renderer = GetOrAddComponent<SpriteRenderer>(target.gameObject);
        renderer.sprite = sprite;
        renderer.sharedMaterial = sharedMaterial;
        renderer.sortingOrder = sortingOrder;

        var spriteTransform = GetOrAddComponent<SpriteTransform>(target.gameObject);
        spriteTransform.enabled = true;
        spriteTransform.position = animationPosition;
        spriteTransform.scale = animationScale;
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = true;
        EnsureNativeHierarchy(spriteTransform);
        renderer = spriteTransform.VisualRenderer;
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

    private static void MigrateToNativeHierarchy(Transform root)
    {
        var spriteTransforms = root.GetComponentsInChildren<SpriteTransform>(true)
            .OrderByDescending(item => GetDepth(item.transform))
            .ToArray();

        foreach (var spriteTransform in spriteTransforms)
        {
            EnsureNativeHierarchy(spriteTransform);
        }

        foreach (var spriteTransform in spriteTransforms.OrderBy(item => GetDepth(item.transform)))
        {
            spriteTransform.RefreshPositionReference();
            spriteTransform.Apply();
            EditorUtility.SetDirty(spriteTransform);
        }
    }

    private static bool EnsureNativeHierarchy(SpriteTransform spriteTransform)
    {
        var pivot = spriteTransform.transform;
        var content = spriteTransform.NativeContent;
        var changed = false;

        if (content == null || content.parent != pivot)
        {
            content = pivot.Find(NativeContent);
        }

        if (content == null)
        {
            content = new GameObject(NativeContent) { layer = pivot.gameObject.layer }.transform;
            content.SetParent(pivot, false);
            changed = true;
        }
        else if (content.gameObject.layer != pivot.gameObject.layer)
        {
            content.gameObject.layer = pivot.gameObject.layer;
            changed = true;
        }

        var directChildren = new List<Transform>();
        foreach (Transform child in pivot)
        {
            if (child != content) directChildren.Add(child);
        }

        foreach (var child in directChildren)
        {
            child.SetParent(content, false);
            changed = true;
        }

        var pivotRenderer = pivot.GetComponent<SpriteRenderer>();
        var contentRenderer = content.GetComponent<SpriteRenderer>();
        if (pivotRenderer != null)
        {
            ComponentUtility.CopyComponent(pivotRenderer);
            if (contentRenderer == null)
            {
                ComponentUtility.PasteComponentAsNew(content.gameObject);
            }
            else
            {
                ComponentUtility.PasteComponentValues(contentRenderer);
            }

            UnityEngine.Object.DestroyImmediate(pivotRenderer);
            changed = true;
        }

        if (spriteTransform.NativeContent != content)
        {
            spriteTransform.ConfigureNativeHierarchy(content);
            changed = true;
        }

        return changed;
    }

    private static int GetDepth(Transform target)
    {
        var depth = 0;
        while (target.parent != null)
        {
            depth++;
            target = target.parent;
        }

        return depth;
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
        var sunAnchor = anchors != null ? anchors.Find("Sun_Anchor") : null;
        var head = root.Find(HeadPath);
        if (anchors == null || sunAnchor == null || head == null)
        {
            throw new MissingReferenceException("SunFlower production anchor hierarchy is incomplete.");
        }

        // The sun is centered on its root Transform. Keep production independent
        // from the animated head, but place its fixed origin at the default head
        // center instead of the old unconverted (0, 25) plant-space value.
        sunAnchor.localPosition = anchors.InverseTransformPoint(head.position);
        sunAnchor.localRotation = Quaternion.identity;
        sunAnchor.localScale = Vector3.one;
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

    private static void MigrateHeadAnimationBindings(AnimationClip clip)
    {
        var migrations = new List<(EditorCurveBinding OldBinding, EditorCurveBinding NewBinding, AnimationCurve Curve)>();
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(SpriteTransform))
            {
                continue;
            }

            var isLegacyHeadTransform =
                (binding.path == LegacyFacePath || binding.path == LegacyNestedHeadPath) &&
                IsHeadTransformProperty(binding.propertyName);
            if (!isLegacyHeadTransform) continue;

            var migratedBinding = binding;
            migratedBinding.path = HeadPath;
            migrations.Add((binding, migratedBinding, AnimationUtility.GetEditorCurve(clip, binding)));
        }

        foreach (var migration in migrations)
        {
            AnimationUtility.SetEditorCurve(clip, migration.OldBinding, null);
            AnimationUtility.SetEditorCurve(clip, migration.NewBinding, migration.Curve);
        }
    }

    private static void MigrateBasicContentBindings(AnimationClip clip)
    {
        var legacyPrefix = BasicPath + "/";
        var nativePrefix = BasicVisualPath + "/";
        var migrations = new List<(EditorCurveBinding OldBinding, EditorCurveBinding NewBinding, AnimationCurve Curve)>();
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!binding.path.StartsWith(legacyPrefix, StringComparison.Ordinal) ||
                binding.path.StartsWith(nativePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var migratedBinding = binding;
            migratedBinding.path = BasicVisualPath + binding.path.Substring(BasicPath.Length);
            migrations.Add((binding, migratedBinding, AnimationUtility.GetEditorCurve(clip, binding)));
        }

        foreach (var migration in migrations)
        {
            AnimationUtility.SetEditorCurve(clip, migration.OldBinding, null);
            AnimationUtility.SetEditorCurve(clip, migration.NewBinding, migration.Curve);
        }
    }

    private static bool IsHeadTransformProperty(string propertyName)
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
