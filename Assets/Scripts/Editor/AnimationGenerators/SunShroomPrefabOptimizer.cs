using System;
using System.Globalization;
using System.IO;
using LitJson;
using PvZ.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SunShroomPrefabOptimizer
{
    private const string PrefabPath = "Assets/Prefab/Plant/SunShroom/SunShroom.prefab";
    private const string IdleClipPath = "Assets/Prefab/Plant/SunShroom/Animation/idle.anim";
    private const string SleepClipPath = "Assets/Prefab/Plant/SunShroom/Animation/sleep.anim";
    private const string BlinkClipPath = "Assets/Prefab/Plant/SunShroom/Animation/blink.anim";
    private const string NoSunClipPath = "Assets/Prefab/Plant/SunShroom/Animation/nosun.anim";
    private const string SunClipPath = "Assets/Prefab/Plant/SunShroom/Animation/sun.anim";
    private const string ControllerPath = "Assets/Prefab/Plant/SunShroom/Animation/SunShroom.controller";
    private const string BodyContainerPath = "component/basic/body";
    private const string BasicPath = "component/basic";
    private const string BodyPath = "component/basic/body/SunShroom_body";
    private const string AnchorsPath = "component/anchors";
    private const string AnchorPath = "component/anchors/Sun_Anchor";
    private const string OldAnchorPath = "component/basic/head/SunShroom_head/Sun_Anchor";
    private const string OldSleepContainerPath = "component/basic/sleep";
    private const string OldSleepPath = "component/basic/sleep/SunShroom_sleep";
    private const string SleepPath = "component/basic/body/SunShroom_body/SunShroom_sleep";
    private const string OldBlinkPath = "component/basic/body/blink";
    private const string BlinkPath = "component/basic/body/SunShroom_body/blink";
    private const string OptimizationVersion = "sunshroom-structure-v6-sprite-position-provider";
    private static readonly Vector2 SpritePosition = new(42.275f, 45.875f);

    [InitializeOnLoadMethod]
    private static void SchedulePendingOptimization()
    {
        EditorApplication.delayCall += OptimizePendingAssets;
    }

    private static void OptimizePendingAssets()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += OptimizePendingAssets;
            return;
        }

        var importer = AssetImporter.GetAtPath(PrefabPath);
        if (importer == null || importer.userData == OptimizationVersion) return;

        try
        {
            OptimizeSunShroom();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/PvZ/Optimize SunShroom Prefab")]
    public static void OptimizeSunShroom()
    {
        var source = LoadSource();
        var idleClip = RequireClip(IdleClipPath);
        var sleepClip = RequireClip(SleepClipPath);

        RebuildSourceClip(idleClip, source["idle"]);
        RebuildSourceClip(sleepClip, source["sleep"]);
        MigrateSupplementalClip(RequireClip(BlinkClipPath));
        MigrateSupplementalClip(RequireClip(NoSunClipPath));
        MigrateSupplementalClip(RequireClip(SunClipPath));
        ConfigureController(sleepClip);
        ConfigurePrefab();

        var importer = AssetImporter.GetAtPath(PrefabPath);
        importer.userData = OptimizationVersion;
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(PrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static JsonData LoadSource()
    {
        var absolutePath = Path.Combine(Application.dataPath, "StreamingAssets/SunShroom.json");
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Missing SunShroom FLA export data.", absolutePath);
        }

        return JsonMapper.ToObject(File.ReadAllText(absolutePath));
    }

    private static AnimationClip RequireClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) throw new MissingReferenceException($"Missing animation clip: {path}");
        return clip;
    }

    private static void RebuildSourceClip(AnimationClip clip, JsonData objects)
    {
        ClearClip(clip);
        clip.frameRate = 12f;

        for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++)
        {
            var sourceObject = objects[objectIndex];
            var sourceName = sourceObject["obj"].ToString();
            var path = ResolveSourcePath(sourceName);
            var properties = sourceObject["ani_list"];
            var frames = sourceObject["ani"];

            for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
            {
                var property = properties[propertyIndex].ToString();
                var curve = new AnimationCurve();
                for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
                {
                    var frame = frames[frameIndex];
                    var time = ParseFloat(frame["fram"]) / clip.frameRate;
                    var value = ParseFloat(frame[property]);
                    curve.AddKey(new Keyframe(time, value));
                }

                var bindingType = property == "m_IsActive"
                    ? typeof(GameObject)
                    : typeof(SpriteTransform);
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, bindingType, property),
                    curve);
            }
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static float ParseFloat(JsonData value)
    {
        return float.Parse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static string ResolveSourcePath(string sourceName)
    {
        return sourceName switch
        {
            "SunShroom_head" => "component/basic/head/SunShroom_head",
            "SunShroom_body" => BodyPath,
            "SunShroom_sleep" => SleepPath,
            _ => throw new InvalidOperationException($"Unknown SunShroom FLA object: {sourceName}")
        };
    }

    private static void MigrateSupplementalClip(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var newPath = MigratePath(binding.path);
            if (newPath == binding.path) continue;

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var migratedBinding = binding;
            migratedBinding.path = newPath;
            AnimationUtility.SetEditorCurve(clip, binding, null);
            AnimationUtility.SetEditorCurve(clip, migratedBinding, curve);
        }

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var newPath = MigratePath(binding.path);
            if (newPath == binding.path) continue;

            var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var migratedBinding = binding;
            migratedBinding.path = newPath;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            AnimationUtility.SetObjectReferenceCurve(clip, migratedBinding, curve);
        }

        EditorUtility.SetDirty(clip);
    }

    private static string MigratePath(string path)
    {
        if (path == OldSleepPath) return SleepPath;
        if (path == OldBlinkPath) return BlinkPath;
        if (path.StartsWith(OldBlinkPath + "/", StringComparison.Ordinal))
        {
            return BlinkPath + path.Substring(OldBlinkPath.Length);
        }

        return path;
    }

    private static void ConfigureController(AnimationClip sleepClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) throw new MissingReferenceException($"Missing controller: {ControllerPath}");

        foreach (var layer in controller.layers)
        {
            if (layer.name != "SunShroom") continue;

            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name != "sleep") continue;

                state.state.motion = sleepClip;
                EditorUtility.SetDirty(state.state);
                EditorUtility.SetDirty(controller);
                return;
            }
        }

        throw new MissingReferenceException("SunShroom controller has no sleep state.");
    }

    private static void ConfigurePrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var basic = RequireTransform(root.transform, BasicPath);
            var bodyContainer = RequireTransform(root.transform, BodyContainerPath);
            var body = RequireTransform(root.transform, BodyPath);
            var anchors = RequireTransform(root.transform, AnchorsPath);
            var sunAnchor = root.transform.Find(AnchorPath) ?? root.transform.Find(OldAnchorPath);
            if (sunAnchor == null) throw new MissingReferenceException("SunShroom sun anchor is missing.");
            var sleep = root.transform.Find(SleepPath) ?? root.transform.Find(OldSleepPath);
            if (sleep == null) throw new MissingReferenceException("SunShroom sleep sprite is missing.");
            var blinkRoot = body.Find("blink") ?? root.transform.Find(OldBlinkPath);
            if (blinkRoot == null) throw new MissingReferenceException("SunShroom blink hierarchy is missing.");

            if (blinkRoot.parent != body) blinkRoot.SetParent(body, false);
            if (sleep.parent != body) sleep.SetParent(body, false);
            if (sunAnchor.parent != anchors) sunAnchor.SetParent(anchors, false);
            sunAnchor.localPosition = new Vector3(-0.025001526f, 6.4749985f, 0f);
            sunAnchor.localRotation = Quaternion.identity;
            sunAnchor.localScale = Vector3.one;
            var anchorSpriteTransform = sunAnchor.GetComponent<SpriteTransform>();
            if (anchorSpriteTransform != null)
            {
                UnityEngine.Object.DestroyImmediate(anchorSpriteTransform);
            }

            var oldSleepContainer = root.transform.Find(OldSleepContainerPath);
            if (oldSleepContainer != null && oldSleepContainer.childCount == 0)
            {
                UnityEngine.Object.DestroyImmediate(oldSleepContainer.gameObject);
            }

            var containerTransform = bodyContainer.GetComponent<SpriteTransform>();
            if (containerTransform != null) UnityEngine.Object.DestroyImmediate(containerTransform);
            bodyContainer.localPosition = Vector3.zero;
            bodyContainer.localRotation = Quaternion.identity;
            bodyContainer.localScale = Vector3.one;

            ConfigureSpritePosition(basic, SpritePosition);

            var bodyTransform = GetOrAddSpriteTransform(body);
            ConfigureVisualTransform(
                body,
                bodyTransform,
                new Vector2(41.7f, 56.35f),
                new Vector2(79.998779296875f, 79.998779296875f));
            bodyTransform.providesChildSpritePosition = true;
            bodyTransform.spritePosition = bodyTransform.position;
            bodyTransform.Apply();
            bodyTransform.RefreshDescendantPositionReferences();

            ConfigureBlink(
                RequireTransform(root.transform, BlinkPath + "/SunShroom_blink1"),
                new Vector2(41.6f, 54f));
            ConfigureBlink(
                RequireTransform(root.transform, BlinkPath + "/SunShroom_blink2"),
                new Vector2(41.45f, 53.9f));

            var sleepTransform = GetOrAddSpriteTransform(sleep);
            ConfigureVisualTransform(
                sleep,
                sleepTransform,
                new Vector2(41.45f, 54.2f),
                new Vector2(82.14111328125f, 84.442138671875f));
            sleep.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform RequireTransform(Transform root, string path)
    {
        var target = root.Find(path);
        if (target == null) throw new MissingReferenceException($"Missing SunShroom hierarchy path: {path}");
        return target;
    }

    private static SpriteTransform GetOrAddSpriteTransform(Transform target)
    {
        var spriteTransform = target.GetComponent<SpriteTransform>();
        return spriteTransform != null
            ? spriteTransform
            : target.gameObject.AddComponent<SpriteTransform>();
    }

    private static void ConfigureVisualTransform(
        Transform target,
        SpriteTransform spriteTransform,
        Vector2 position,
        Vector2 scale)
    {
        spriteTransform.enabled = true;
        spriteTransform.position = position;
        spriteTransform.scale = scale;
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = true;
        spriteTransform.providesChildSpritePosition = false;
        spriteTransform.spritePosition = Vector2.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
        spriteTransform.Apply();
        EditorUtility.SetDirty(spriteTransform);
    }

    private static void ConfigureSpritePosition(Transform target, Vector2 spritePosition)
    {
        var spriteTransform = GetOrAddSpriteTransform(target);
        spriteTransform.enabled = true;
        spriteTransform.position = Vector2.zero;
        spriteTransform.scale = new Vector2(100f, 100f);
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = false;
        spriteTransform.providesChildSpritePosition = true;
        spriteTransform.spritePosition = spritePosition;
        EditorUtility.SetDirty(spriteTransform);
    }

    private static void ConfigureBlink(Transform blink, Vector2 globalPosition)
    {
        var spriteTransform = GetOrAddSpriteTransform(blink);
        spriteTransform.position = globalPosition;
        spriteTransform.updatePosition = true;
        spriteTransform.Apply();
        blink.localRotation = Quaternion.identity;
        blink.localScale = Vector3.one;
        blink.gameObject.SetActive(false);
        EditorUtility.SetDirty(spriteTransform);
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
}
