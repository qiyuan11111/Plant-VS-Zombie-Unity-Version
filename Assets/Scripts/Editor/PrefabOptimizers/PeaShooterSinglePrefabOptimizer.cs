using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LitJson;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ComponentUtility = UnityEditorInternal.ComponentUtility;

public static class PeaShooterSinglePrefabOptimizer
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
               PrefabBlinkIsCurrent(prefab, animator, faceBasePose);
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

    private static AnimationClip GetOrCreateClip(string path, string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;

        clip = new AnimationClip { name = clipName };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void ConfigureBlinkClip(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationUtility.SetEditorCurve(clip, binding, null);
        }
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        }

        clip.frameRate = FrameRate;
        SetActiveCurve(clip, BlinkPartPaths["PeaShooter_blink1"], 0f, 1f, 0f, 1f, 0f);
        SetActiveCurve(clip, BlinkPartPaths["PeaShooter_blink2"], 0f, 0f, 1f, 0f, 0f);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(clip);
    }

    private static void SetActiveCurve(
        AnimationClip clip,
        string path,
        params float[] values)
    {
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"),
            BuildStepCurve(values));
    }

    private static AnimationCurve BuildStepCurve(params float[] values)
    {
        var curve = new AnimationCurve();
        for (var index = 0; index < values.Length; index++)
        {
            curve.AddKey(index / FrameRate, values[index]);
            AnimationUtility.SetKeyLeftTangentMode(
                curve, index, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(
                curve, index, AnimationUtility.TangentMode.Constant);
        }
        return curve;
    }

    private static bool BlinkClipIsCurrent(AnimationClip clip)
    {
        if (clip == null || !Mathf.Approximately(clip.frameRate, FrameRate) ||
            AnimationUtility.GetAnimationClipSettings(clip).loopTime ||
            AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0 ||
            AnimationUtility.GetAnimationEvents(clip).Length != 0)
        {
            return false;
        }

        var expectedCurves = new Dictionary<string, AnimationCurve>
        {
            { BlinkPartPaths["PeaShooter_blink1"], BuildStepCurve(0f, 1f, 0f, 1f, 0f) },
            { BlinkPartPaths["PeaShooter_blink2"], BuildStepCurve(0f, 0f, 1f, 0f, 0f) }
        };
        var bindings = AnimationUtility.GetCurveBindings(clip);
        if (bindings.Length != expectedCurves.Count) return false;

        foreach (var binding in bindings)
        {
            if (binding.type != typeof(GameObject) ||
                binding.propertyName != "m_IsActive" ||
                !expectedCurves.TryGetValue(binding.path, out var expectedCurve) ||
                !CurvesEqual(AnimationUtility.GetEditorCurve(clip, binding), expectedCurve))
            {
                return false;
            }
        }
        return true;
    }

    private static void SynchronizeClip(
        AnimationClip clip,
        JsonData objects,
        bool loopTime,
        AnimationEvent[] events)
    {
        var changed = false;
        if (!Mathf.Approximately(clip.frameRate, FrameRate))
        {
            clip.frameRate = FrameRate;
            changed = true;
        }
        var expectedBindings = new HashSet<EditorCurveBinding>();

        for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++)
        {
            var sourceObject = objects[objectIndex];
            var sourceName = sourceObject["obj"].ToString();
            if (!PartPaths.TryGetValue(sourceName, out var path))
            {
                throw new InvalidOperationException($"Unknown PeaShooterSingle animation object: {sourceName}");
            }

            var properties = sourceObject["ani_list"];
            var frames = sourceObject["ani"];
            for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
            {
                var property = properties[propertyIndex].ToString();
                var curve = BuildLinearCurve(frames, property);

                var bindingType = property == "m_IsActive" ? typeof(GameObject) : typeof(SpriteTransform);
                var binding = EditorCurveBinding.FloatCurve(path, bindingType, property);
                expectedBindings.Add(binding);
                var existingCurve = AnimationUtility.GetEditorCurve(clip, binding);
                if (!CurvesEqual(existingCurve, curve))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    changed = true;
                }
            }
        }

        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (expectedBindings.Contains(binding)) continue;
            AnimationUtility.SetEditorCurve(clip, binding, null);
            changed = true;
        }

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            changed = true;
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        if (settings.loopTime != loopTime)
        {
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            changed = true;
        }
        if (!EventsEqual(AnimationUtility.GetAnimationEvents(clip), events))
        {
            AnimationUtility.SetAnimationEvents(clip, events);
            changed = true;
        }
        if (changed) EditorUtility.SetDirty(clip);
    }

    private static bool ClipMatchesSource(
        AnimationClip clip,
        JsonData objects,
        bool loopTime,
        bool expectsShootEvent)
    {
        if (clip == null || !Mathf.Approximately(clip.frameRate, FrameRate)) return false;
        if (AnimationUtility.GetAnimationClipSettings(clip).loopTime != loopTime) return false;

        var expectedBindingCount = 0;
        for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++)
        {
            var sourceObject = objects[objectIndex];
            if (!PartPaths.TryGetValue(sourceObject["obj"].ToString(), out var path)) return false;

            var properties = sourceObject["ani_list"];
            var frames = sourceObject["ani"];
            expectedBindingCount += properties.Count;
            for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
            {
                var property = properties[propertyIndex].ToString();
                var bindingType = property == "m_IsActive" ? typeof(GameObject) : typeof(SpriteTransform);
                var binding = EditorCurveBinding.FloatCurve(path, bindingType, property);
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var expectedCurve = BuildLinearCurve(frames, property);
                if (!CurvesEqual(curve, expectedCurve)) return false;
            }
        }

        if (AnimationUtility.GetCurveBindings(clip).Length != expectedBindingCount) return false;
        var events = AnimationUtility.GetAnimationEvents(clip);
        return expectsShootEvent
            ? events.Length == 1 && events[0].functionName == "ShootProjectilePea" &&
              Mathf.Abs(events[0].time - SourceShootDelaySeconds * ShootStateSpeed) <= 0.000001f
            : events.Length == 0;
    }

    private static AnimatorController ConfigureController(
        AnimationClip idleClip,
        AnimationClip headIdleClip,
        AnimationClip shootClip,
        AnimationClip blinkClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        EnsureTrigger(controller, "shoot");
        EnsureTrigger(controller, "blink");
        ConfigureDefaultState(controller, "PeaShooterSingle", "idle", idleClip, IdleStateSpeed);
        ConfigureDefaultState(controller, "PeaShooterSingle_head", "head_idle", headIdleClip, IdleStateSpeed);
        RemoveShootStateFromHeadIdleLayer(controller);
        ConfigureBlinkOverlayLayer(controller, blinkClip);
        ConfigureShootOverlayLayer(controller, shootClip);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void EnsureTrigger(AnimatorController controller, string parameterName)
    {
        var parameters = controller.parameters;
        for (var index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].name != parameterName) continue;
            if (parameters[index].type == AnimatorControllerParameterType.Trigger) return;
            parameters[index] = new AnimatorControllerParameter
            {
                name = parameterName,
                type = AnimatorControllerParameterType.Trigger
            };
            controller.parameters = parameters;
            return;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
    }

    private static void ConfigureDefaultState(
        AnimatorController controller,
        string layerName,
        string stateName,
        AnimationClip clip,
        float speed)
    {
        var layer = GetOrCreateLayer(controller, layerName);
        var state = GetOrCreateState(layer.stateMachine, stateName);
        state.motion = clip;
        state.speed = speed;
        state.writeDefaultValues = false;
        layer.stateMachine.defaultState = state;
        SaveLayer(controller, layer);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(layer.stateMachine);
    }

    private static void RemoveShootStateFromHeadIdleLayer(AnimatorController controller)
    {
        var layer = GetOrCreateLayer(controller, "PeaShooterSingle_head");
        layer.defaultWeight = 1f;
        var stateMachine = layer.stateMachine;
        var idleState = stateMachine.defaultState;
        if (idleState != null)
        {
            foreach (AnimatorStateTransition transition in idleState.transitions)
            {
                idleState.RemoveTransition(transition);
            }
        }

        foreach (AnimatorState shootState in stateMachine.states
                     .Select(childState => childState.state)
                     .Where(state => state.name == "shoot")
                     .ToArray())
        {
            stateMachine.RemoveState(shootState);
        }

        SaveLayer(controller, layer);
        EditorUtility.SetDirty(idleState);
        EditorUtility.SetDirty(stateMachine);
    }

    private static void ConfigureBlinkOverlayLayer(
        AnimatorController controller,
        AnimationClip blinkClip)
    {
        var layer = GetOrCreateLayer(controller, "PeaShooterSingle_blink");
        layer.defaultWeight = 1f;
        var stateMachine = layer.stateMachine;

        var inactiveState = GetOrCreateState(stateMachine, "blink_idle");
        inactiveState.motion = null;
        inactiveState.speed = 1f;
        inactiveState.cycleOffset = 0f;
        inactiveState.writeDefaultValues = false;
        stateMachine.defaultState = inactiveState;

        var blinkState = GetOrCreateState(stateMachine, "blink");
        blinkState.motion = blinkClip;
        blinkState.speed = 1f;
        blinkState.writeDefaultValues = false;
        foreach (var behaviour in blinkState.behaviours.ToArray())
        {
            UnityEngine.Object.DestroyImmediate(behaviour, true);
        }

        ClearTransitions(inactiveState);
        ClearTransitions(blinkState);

        var enterTransition = inactiveState.AddTransition(blinkState);
        enterTransition.duration = 0f;
        enterTransition.hasFixedDuration = true;
        enterTransition.hasExitTime = false;
        enterTransition.offset = 0f;
        enterTransition.canTransitionToSelf = false;
        enterTransition.AddCondition(AnimatorConditionMode.If, 0f, "blink");

        var restartTransition = blinkState.AddTransition(blinkState);
        restartTransition.duration = 0f;
        restartTransition.hasFixedDuration = true;
        restartTransition.hasExitTime = false;
        restartTransition.offset = 0f;
        restartTransition.canTransitionToSelf = true;
        restartTransition.AddCondition(AnimatorConditionMode.If, 0f, "blink");

        SaveLayer(controller, layer);
        EditorUtility.SetDirty(inactiveState);
        EditorUtility.SetDirty(blinkState);
        EditorUtility.SetDirty(stateMachine);
    }

    private static void ClearTransitions(AnimatorState state)
    {
        foreach (var transition in state.transitions)
        {
            state.RemoveTransition(transition);
            UnityEngine.Object.DestroyImmediate(transition, true);
        }
    }

    private static void ConfigureShootOverlayLayer(
        AnimatorController controller,
        AnimationClip shootClip)
    {
        var layer = GetOrCreateLayer(controller, "PeaShooterSingle_head_shoot");
        layer.defaultWeight = 0f;
        var stateMachine = layer.stateMachine;

        var inactiveState = GetOrCreateState(stateMachine, "shoot_idle");
        inactiveState.motion = null;
        inactiveState.speed = 1f;
        inactiveState.writeDefaultValues = false;
        stateMachine.defaultState = inactiveState;

        var shootState = GetOrCreateState(stateMachine, "shoot");
        shootState.motion = shootClip;
        shootState.speed = ShootStateSpeed;
        shootState.writeDefaultValues = false;
        if (!shootState.behaviours.OfType<PeaShooterShootOverlayStateBehaviour>().Any())
        {
            shootState.AddStateMachineBehaviour<PeaShooterShootOverlayStateBehaviour>();
        }

        var enterTransition = inactiveState.transitions.FirstOrDefault(transition =>
            transition.destinationState == shootState &&
            transition.conditions.Length == 1 &&
            transition.conditions[0].parameter == "shoot");
        if (enterTransition == null)
        {
            enterTransition = inactiveState.AddTransition(shootState);
            enterTransition.AddCondition(AnimatorConditionMode.If, 0f, "shoot");
        }
        foreach (AnimatorStateTransition transition in inactiveState.transitions)
        {
            if (transition != enterTransition) inactiveState.RemoveTransition(transition);
        }
        enterTransition.duration = 0f;
        enterTransition.hasFixedDuration = true;
        enterTransition.hasExitTime = false;
        enterTransition.offset = 0f;
        enterTransition.canTransitionToSelf = false;

        foreach (AnimatorStateTransition transition in shootState.transitions)
        {
            shootState.RemoveTransition(transition);
        }

        SaveLayer(controller, layer);
        MoveLayerToLast(controller, layer.stateMachine);
        EditorUtility.SetDirty(inactiveState);
        EditorUtility.SetDirty(shootState);
        EditorUtility.SetDirty(stateMachine);
    }

    private static void MoveLayerToLast(
        AnimatorController controller,
        AnimatorStateMachine stateMachine)
    {
        var layers = controller.layers.ToList();
        int index = layers.FindIndex(layer => layer.stateMachine == stateMachine);
        if (index < 0 || index == layers.Count - 1) return;

        var layer = layers[index];
        layers.RemoveAt(index);
        layers.Add(layer);
        controller.layers = layers.ToArray();
    }

    private static AnimatorControllerLayer GetOrCreateLayer(AnimatorController controller, string layerName)
    {
        foreach (var layer in controller.layers)
        {
            if (layer.name == layerName) return layer;
        }

        var stateMachine = new AnimatorStateMachine { name = layerName };
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

    private static AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == stateName) return childState.state;
        }

        return stateMachine.AddState(stateName);
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

    private static bool StateUsesClip(
        AnimatorController controller,
        string layerName,
        string stateName,
        AnimationClip clip)
    {
        if (controller == null || clip == null) return false;
        return controller.layers
            .Where(layer => layer.name == layerName)
            .SelectMany(layer => layer.stateMachine.states)
            .Any(childState => childState.state.name == stateName && childState.state.motion == clip);
    }

    private static bool ControllerIsCurrent(
        AnimatorController controller,
        AnimationClip idleClip,
        AnimationClip headIdleClip,
        AnimationClip shootClip,
        AnimationClip blinkClip)
    {
        var bodyIdleState = FindState(controller, "PeaShooterSingle", "idle");
        var headIdleState = FindState(controller, "PeaShooterSingle_head", "head_idle");
        if (!StateUsesClip(controller, "PeaShooterSingle", "idle", idleClip) ||
            !StateUsesClip(controller, "PeaShooterSingle_head", "head_idle", headIdleClip) ||
            !StateUsesClip(controller, "PeaShooterSingle_head_shoot", "shoot", shootClip) ||
            !StateUsesClip(controller, "PeaShooterSingle_blink", "blink", blinkClip) ||
            bodyIdleState == null || bodyIdleState.writeDefaultValues ||
            !Mathf.Approximately(bodyIdleState.speed, IdleStateSpeed) ||
            headIdleState == null || headIdleState.writeDefaultValues ||
            !Mathf.Approximately(headIdleState.speed, IdleStateSpeed) ||
            !controller.parameters.Any(parameter =>
                parameter.name == "shoot" && parameter.type == AnimatorControllerParameterType.Trigger) ||
            !controller.parameters.Any(parameter =>
                parameter.name == "blink" && parameter.type == AnimatorControllerParameterType.Trigger) ||
            !BlinkLayerIsCurrent(controller, blinkClip))
        {
            return false;
        }

        var headLayer = controller.layers.SingleOrDefault(layer => layer.name == "PeaShooterSingle_head");
        if (headLayer == null || headLayer.stateMachine.defaultState != headIdleState ||
            headIdleState.transitions.Length != 0 ||
            headLayer.stateMachine.states.Any(childState => childState.state.name == "shoot"))
        {
            return false;
        }

        var shootLayer = controller.layers.SingleOrDefault(layer => layer.name == "PeaShooterSingle_head_shoot");
        if (shootLayer == null ||
            controller.layers[controller.layers.Length - 1].stateMachine != shootLayer.stateMachine ||
            !Mathf.Approximately(shootLayer.defaultWeight, 0f))
        {
            return false;
        }

        var inactiveState = shootLayer.stateMachine.defaultState;
        var shootState = shootLayer.stateMachine.states
            .Select(childState => childState.state)
            .SingleOrDefault(state => state.name == "shoot");
        if (inactiveState == null || inactiveState.name != "shoot_idle" ||
            inactiveState.motion != null || inactiveState.writeDefaultValues ||
            shootState == null || shootState == inactiveState || shootState.writeDefaultValues ||
            !Mathf.Approximately(shootState.speed, ShootStateSpeed) ||
            shootState.behaviours.OfType<PeaShooterShootOverlayStateBehaviour>().Count() != 1)
        {
            return false;
        }

        var enterTransition = inactiveState.transitions.SingleOrDefault();
        return enterTransition != null && enterTransition.destinationState == shootState &&
               !enterTransition.hasExitTime && enterTransition.hasFixedDuration &&
               Mathf.Approximately(enterTransition.duration, 0f) &&
               enterTransition.conditions.Length == 1 &&
               enterTransition.conditions[0].parameter == "shoot" &&
               enterTransition.conditions[0].mode == AnimatorConditionMode.If &&
               shootState.transitions.Length == 0;
    }

    private static bool BlinkLayerIsCurrent(
        AnimatorController controller,
        AnimationClip blinkClip)
    {
        var layer = controller.layers.SingleOrDefault(
            candidate => candidate.name == "PeaShooterSingle_blink");
        if (layer == null || !Mathf.Approximately(layer.defaultWeight, 1f))
        {
            return false;
        }

        var inactiveState = layer.stateMachine.defaultState;
        var blinkState = layer.stateMachine.states
            .Select(childState => childState.state)
            .SingleOrDefault(state => state.name == "blink");
        if (inactiveState == null || inactiveState.name != "blink_idle" ||
            inactiveState.motion != null || inactiveState.writeDefaultValues ||
            !Mathf.Approximately(inactiveState.speed, 1f) ||
            !Mathf.Approximately(inactiveState.cycleOffset, 0f) ||
            blinkState == null || blinkState.motion != blinkClip ||
            blinkState.writeDefaultValues || !Mathf.Approximately(blinkState.speed, 1f) ||
            blinkState.behaviours.Length != 0)
        {
            return false;
        }

        var enterTransition = inactiveState.transitions.SingleOrDefault();
        var restartTransition = blinkState.transitions.SingleOrDefault();
        return enterTransition != null &&
               enterTransition.destinationState == blinkState &&
               !enterTransition.hasExitTime &&
               enterTransition.hasFixedDuration &&
               Mathf.Approximately(enterTransition.duration, 0f) &&
               enterTransition.conditions.Length == 1 &&
               enterTransition.conditions[0].parameter == "blink" &&
               enterTransition.conditions[0].mode == AnimatorConditionMode.If &&
               restartTransition != null &&
               restartTransition.destinationState == blinkState &&
               !restartTransition.hasExitTime &&
               restartTransition.hasFixedDuration &&
               restartTransition.canTransitionToSelf &&
               Mathf.Approximately(restartTransition.duration, 0f) &&
               restartTransition.conditions.Length == 1 &&
               restartTransition.conditions[0].parameter == "blink" &&
               restartTransition.conditions[0].mode == AnimatorConditionMode.If;
    }

    private static AnimatorState FindState(
        AnimatorController controller,
        string layerName,
        string stateName)
    {
        if (controller == null) return null;
        return controller.layers
            .Where(layer => layer.name == layerName)
            .SelectMany(layer => layer.stateMachine.states)
            .Select(childState => childState.state)
            .SingleOrDefault(state => state.name == stateName);
    }

    private static bool FirstFrameMatchesPrefab(Transform root, AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(SpriteTransform)) continue;
            var target = root.Find(binding.path);
            var spriteTransform = target != null ? target.GetComponent<SpriteTransform>() : null;
            if (spriteTransform == null) return false;

            var expected = AnimationUtility.GetEditorCurve(clip, binding).Evaluate(0f);
            var actual = binding.propertyName switch
            {
                "position.x" => spriteTransform.position.x,
                "position.y" => spriteTransform.position.y,
                "scale.x" => spriteTransform.scale.x,
                "scale.y" => spriteTransform.scale.y,
                "skew.x" => spriteTransform.skew.x,
                "skew.y" => spriteTransform.skew.y,
                _ => expected
            };
            if (Mathf.Abs(actual - expected) > 0.000001f) return false;
        }
        return true;
    }

    private readonly struct SourceAffinePose
    {
        public readonly Vector2 Position;
        public readonly Vector2 Scale;
        public readonly Vector2 Skew;

        public SourceAffinePose(Vector2 position, Vector2 scale, Vector2 skew)
        {
            Position = position;
            Scale = scale;
            Skew = skew;
        }
    }

    private static SourceAffinePose GetParentReferencePose(
        AnimationClip parentClip,
        string parentPath)
    {
        if (parentClip == null)
        {
            throw new ArgumentNullException(nameof(parentClip));
        }

        // This is the same base-pose rule as the original Reanimation system:
        // sample the parent track at the first frame of the selected layer.
        return new SourceAffinePose(
            GetBasePoseVector(parentClip, parentPath, "position", Vector2.zero),
            GetBasePoseVector(parentClip, parentPath, "scale", new Vector2(100f, 100f)),
            GetBasePoseVector(parentClip, parentPath, "skew", Vector2.zero));
    }

    private static Vector2 GetBasePoseVector(
        AnimationClip clip,
        string path,
        string property,
        Vector2 fallback)
    {
        var xBinding = EditorCurveBinding.FloatCurve(
            path, typeof(SpriteTransform), $"{property}.x");
        var yBinding = EditorCurveBinding.FloatCurve(
            path, typeof(SpriteTransform), $"{property}.y");
        var xCurve = AnimationUtility.GetEditorCurve(clip, xBinding);
        var yCurve = AnimationUtility.GetEditorCurve(clip, yBinding);
        return new Vector2(
            xCurve != null ? xCurve.Evaluate(0f) : fallback.x,
            yCurve != null ? yCurve.Evaluate(0f) : fallback.y);
    }

    private static void ConfigurePrefab(
        RuntimeAnimatorController controller,
        AnimationClip idleClip,
        AnimationClip headIdleClip)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureCenteredRoot(root);
            var changed = FlattenPodHierarchy(root.transform);
            changed |= MigratePrefabToNativeHierarchy(root);
            foreach (var path in PartPaths.Values)
            {
                var target = root.transform.Find(path);
                if (target == null) throw new MissingReferenceException($"PeaShooterSingle prefab is missing '{path}'.");
                if (target.GetComponent<SpriteTransform>() == null)
                {
                    throw new MissingComponentException($"PeaShooterSingle part '{path}' has no SpriteTransform.");
                }
            }

            var animator = root.GetComponent<Animator>() ?? root.AddComponent<Animator>();
            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                changed = true;
            }
            if (animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
                changed = true;
            }
            var headTransform = root.transform.Find(HeadAttachmentPath).GetComponent<SpriteTransform>();
            var headAttachmentBasePose = GetParentReferencePose(
                idleClip,
                HeadAttachmentPath);
            if (!headTransform.providesChildSpritePosition ||
                !headTransform.providesChildSpriteAffine ||
                headTransform.spritePosition != headAttachmentBasePose.Position ||
                headTransform.spriteScale != headAttachmentBasePose.Scale ||
                headTransform.spriteSkew != headAttachmentBasePose.Skew)
            {
                headTransform.providesChildSpritePosition = true;
                headTransform.providesChildSpriteAffine = true;
                headTransform.spritePosition = headAttachmentBasePose.Position;
                headTransform.spriteScale = headAttachmentBasePose.Scale;
                headTransform.spriteSkew = headAttachmentBasePose.Skew;
                EditorUtility.SetDirty(headTransform);
                changed = true;
            }
            changed |= ApplyFirstFrame(root.transform, idleClip);
            changed |= ApplyFirstFrame(root.transform, headIdleClip);
            ConfigureBlinkParts(root, animator, headIdleClip);
            changed |= MigratePrefabToNativeHierarchy(root);
            changed = true;
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureCenteredRoot(GameObject root)
    {
        if (root.GetComponent<SpriteGroup>() == null) root.AddComponent<SpriteGroup>();
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var basic = root.transform.Find(BasicPath);
        var basicTransform = basic != null ? basic.GetComponent<SpriteTransform>() : null;
        if (basicTransform == null)
        {
            throw new MissingComponentException("PeaShooterSingle basic node requires SpriteTransform.");
        }

        basic.localPosition = Vector3.zero;
        basic.localRotation = Quaternion.identity;
        basic.localScale = Vector3.one;
        basicTransform.enabled = true;
        basicTransform.position = Vector2.zero;
        basicTransform.scale = new Vector2(100f, 100f);
        basicTransform.skew = Vector2.zero;
        basicTransform.updatePosition = false;
        basicTransform.providesChildSpritePosition = false;
        basicTransform.providesChildSpriteAffine = false;
        basicTransform.spritePosition = Vector2.zero;
        basicTransform.spriteScale = new Vector2(100f, 100f);
        basicTransform.spriteSkew = Vector2.zero;
        EditorUtility.SetDirty(basicTransform);

        var shooter = root.GetComponent<PeaShooterSingle>();
        if (shooter != null)
        {
            var data = new SerializedObject(shooter);
            data.FindProperty("shadowLocalPosition").vector2Value = new Vector2(0.55f, -21.25f);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static bool FlattenPodHierarchy(Transform root)
    {
        var headAttachment = root.Find(HeadAttachmentPath) ?? root.Find("component/basic/head");
        if (headAttachment == null) return false;

        var headContent = headAttachment.Find(NativeContent);
        var targetParent = headContent != null ? headContent : headAttachment;
        var pod = targetParent.Find("pod") ?? headAttachment.Find("pod");
        if (pod == null) return false;

        var podTransform = pod.GetComponent<SpriteTransform>();
        var podContent = podTransform != null ? podTransform.NativeContent : null;
        if (podContent == null || podContent.parent != pod)
        {
            podContent = pod.Find(NativeContent);
        }

        var childrenToMove = new List<Transform>();
        if (podContent != null)
        {
            foreach (Transform child in podContent)
            {
                childrenToMove.Add(child);
            }
        }

        foreach (Transform child in pod)
        {
            if (child != podContent)
            {
                childrenToMove.Add(child);
            }
        }

        foreach (var child in childrenToMove)
        {
            child.SetParent(targetParent, false);
        }

        UnityEngine.Object.DestroyImmediate(pod.gameObject);
        return true;
    }

    private static void ConfigureBlinkParts(
        GameObject root,
        Animator animator,
        AnimationClip headIdleClip)
    {
        var headImage = root.transform.Find(HeadImagePath);
        var headTransform = headImage != null ? headImage.GetComponent<SpriteTransform>() : null;
        var headRenderer = headTransform != null ? headTransform.VisualRenderer : null;
        var blink1 = AssetDatabase.LoadAssetAtPath<Sprite>(Blink1SpritePath);
        var blink2 = AssetDatabase.LoadAssetAtPath<Sprite>(Blink2SpritePath);
        if (headRenderer == null || headRenderer.sharedMaterial == null ||
            headTransform == null || blink1 == null || blink2 == null)
        {
            throw new MissingReferenceException(
                "PeaShooter head, blink sprites or shared material are missing.");
        }

        var faceBasePose = GetParentReferencePose(headIdleClip, HeadImagePath);
        headTransform.providesChildSpritePosition = true;
        headTransform.providesChildSpriteAffine = true;
        headTransform.spritePosition = faceBasePose.Position;
        headTransform.spriteScale = faceBasePose.Scale;
        headTransform.spriteSkew = faceBasePose.Skew;
        headTransform.Apply();
        EditorUtility.SetDirty(headTransform);

        var blinkRoot = GetOrCreatePath(root.transform, HeadVisualPath + "/blink");
        blinkRoot.localRotation = Quaternion.identity;
        blinkRoot.localScale = Vector3.one;
        SetLayerRecursively(blinkRoot.gameObject, root.layer);

        ConfigureBlinkPart(
            root.transform,
            BlinkPartPaths["PeaShooter_blink1"],
            blink1,
            headRenderer.sharedMaterial,
            headRenderer.sortingLayerID,
            Blink1Position,
            Blink1Scale);
        ConfigureBlinkPart(
            root.transform,
            BlinkPartPaths["PeaShooter_blink2"],
            blink2,
            headRenderer.sharedMaterial,
            headRenderer.sortingLayerID,
            Blink2Position,
            Blink2Scale);
        headTransform.RefreshDescendantPositionReferences();

        var shooter = GetOrAddComponent<PeaShooterSingle>(root);
        var blink = GetOrAddComponent<Blink>(root);
        var shooterData = new SerializedObject(shooter);
        shooterData.FindProperty("blink").objectReferenceValue = blink;
        shooterData.FindProperty("projectileSpawnAnchor").objectReferenceValue =
            root.transform.Find(PartPaths["PeaShooterSingle_head/PeaShooterSingle_mouth"]);
        shooterData.ApplyModifiedPropertiesWithoutUndo();

        var blinkData = new SerializedObject(blink);
        blinkData.FindProperty("animator").objectReferenceValue = animator;
        blinkData.FindProperty("minimumIntervalSeconds").floatValue = 2.5f;
        blinkData.FindProperty("maximumIntervalSeconds").floatValue = 5.5f;
        blinkData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBlinkPart(
        Transform root,
        string path,
        Sprite sprite,
        Material sharedMaterial,
        int sortingLayerId,
        Vector2 position,
        Vector2 scale)
    {
        var target = GetOrCreatePath(root, path);
        SetLayerRecursively(target.gameObject, root.gameObject.layer);
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;

        var renderer = GetOrAddComponent<SpriteRenderer>(target.gameObject);
        renderer.sprite = sprite;
        renderer.sharedMaterial = sharedMaterial;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = 10;

        var spriteTransform = GetOrAddComponent<SpriteTransform>(target.gameObject);
        spriteTransform.enabled = true;
        spriteTransform.position = position;
        spriteTransform.scale = scale;
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = true;
        EnsureNativeHierarchy(spriteTransform);
        renderer = spriteTransform.VisualRenderer;
        spriteTransform.RefreshPositionReference();
        spriteTransform.Apply();

        target.gameObject.SetActive(false);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(spriteTransform);
    }

    private static bool PrefabBlinkIsCurrent(
        GameObject root,
        Animator animator,
        SourceAffinePose faceBasePose)
    {
        if (root == null || animator == null) return false;

        var shooter = root.GetComponent<PeaShooterSingle>();
        var blink = root.GetComponent<Blink>();
        var headImage = root.transform.Find(HeadImagePath);
        var headTransform = headImage != null ? headImage.GetComponent<SpriteTransform>() : null;
        var headRenderer = headTransform != null ? headTransform.VisualRenderer : null;
        var blink1 = AssetDatabase.LoadAssetAtPath<Sprite>(Blink1SpritePath);
        var blink2 = AssetDatabase.LoadAssetAtPath<Sprite>(Blink2SpritePath);
        if (shooter == null || blink == null || headRenderer == null ||
            headRenderer.sharedMaterial == null || headTransform == null ||
            !headTransform.providesChildSpritePosition ||
            !headTransform.providesChildSpriteAffine ||
            headTransform.spritePosition != faceBasePose.Position ||
            headTransform.spriteScale != faceBasePose.Scale ||
            headTransform.spriteSkew != faceBasePose.Skew ||
            blink1 == null || blink2 == null)
        {
            return false;
        }

        var shooterData = new SerializedObject(shooter);
        var blinkData = new SerializedObject(blink);
        if (shooterData.FindProperty("blink").objectReferenceValue != blink ||
            shooterData.FindProperty("projectileSpawnAnchor").objectReferenceValue !=
            root.transform.Find(PartPaths["PeaShooterSingle_head/PeaShooterSingle_mouth"]) ||
            blinkData.FindProperty("animator").objectReferenceValue != animator ||
            !Mathf.Approximately(
                blinkData.FindProperty("minimumIntervalSeconds").floatValue, 2.5f) ||
            !Mathf.Approximately(
                blinkData.FindProperty("maximumIntervalSeconds").floatValue, 5.5f))
        {
            return false;
        }

        return BlinkPartIsCurrent(
                   root.transform.Find(BlinkPartPaths["PeaShooter_blink1"]),
                   blink1,
                   headRenderer.sharedMaterial,
                   headRenderer.sortingLayerID,
                   Blink1Position,
                   Blink1Scale) &&
               BlinkPartIsCurrent(
                   root.transform.Find(BlinkPartPaths["PeaShooter_blink2"]),
                   blink2,
                   headRenderer.sharedMaterial,
                   headRenderer.sortingLayerID,
                   Blink2Position,
                   Blink2Scale);
    }

    private static bool BlinkPartIsCurrent(
        Transform target,
        Sprite sprite,
        Material sharedMaterial,
        int sortingLayerId,
        Vector2 position,
        Vector2 scale)
    {
        var spriteTransform = target != null ? target.GetComponent<SpriteTransform>() : null;
        var renderer = spriteTransform != null ? spriteTransform.VisualRenderer : null;
        return target != null &&
               !target.gameObject.activeSelf &&
               target.gameObject.layer == target.root.gameObject.layer &&
               renderer != null &&
               renderer.sprite == sprite &&
               renderer.sharedMaterial == sharedMaterial &&
               renderer.sortingLayerID == sortingLayerId &&
               renderer.sortingOrder == 10 &&
               spriteTransform != null &&
               spriteTransform.NativeContent != null &&
               spriteTransform.enabled &&
               spriteTransform.position == position &&
               spriteTransform.scale == scale &&
               spriteTransform.skew == Vector2.zero &&
               Mathf.Approximately(spriteTransform.brightness, 1f) &&
               Mathf.Approximately(spriteTransform.alpha, 1f) &&
               Mathf.Approximately(spriteTransform.alphaCoef, 1f) &&
               spriteTransform.updatePosition;
    }

    private static bool NativeHierarchyIsCurrent(GameObject root)
    {
        if (root == null) return false;

        foreach (var path in PartPaths.Values.Concat(BlinkPartPaths.Values))
        {
            if (root.transform.Find(path)?.GetComponent<SpriteTransform>() == null) return false;
        }

        var spriteTransforms = root.GetComponentsInChildren<SpriteTransform>(true);
        if (spriteTransforms.Length == 0) return false;
        foreach (var spriteTransform in spriteTransforms)
        {
            if (spriteTransform.NativeContent == null ||
                spriteTransform.NativeContent.parent != spriteTransform.transform ||
                spriteTransform.NativeContent.name != NativeContent ||
                spriteTransform.GetComponent<SpriteRenderer>() != null ||
                spriteTransform.transform.childCount != 1)
            {
                return false;
            }
        }

        return true;
    }

    private static bool MigratePrefabToNativeHierarchy(GameObject root)
    {
        var changed = false;
        var spriteTransforms = root.GetComponentsInChildren<SpriteTransform>(true)
            .OrderByDescending(item => GetDepth(item.transform))
            .ToArray();

        foreach (var spriteTransform in spriteTransforms)
        {
            changed |= EnsureNativeHierarchy(spriteTransform);
        }

        foreach (var spriteTransform in spriteTransforms.OrderBy(item => GetDepth(item.transform)))
        {
            spriteTransform.RefreshPositionReference();
            spriteTransform.Apply();
            EditorUtility.SetDirty(spriteTransform);
        }

        return changed;
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

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Transform GetOrCreatePath(Transform root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('/'))
        {
            var child = current.Find(segment);
            if (child == null)
            {
                var childObject = new GameObject(segment)
                {
                    layer = root.gameObject.layer
                };
                child = childObject.transform;
                child.SetParent(current, false);
            }
            current = child;
        }
        return current;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static bool ApplyFirstFrame(Transform root, AnimationClip clip)
    {
        var changedTransforms = new HashSet<SpriteTransform>();
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(SpriteTransform)) continue;
            var target = root.Find(binding.path);
            if (target == null) throw new MissingReferenceException($"Animation targets missing path '{binding.path}'.");
            var spriteTransform = target.GetComponent<SpriteTransform>();
            if (spriteTransform == null) throw new MissingComponentException($"'{binding.path}' has no SpriteTransform.");

            var value = AnimationUtility.GetEditorCurve(clip, binding).Evaluate(0f);
            switch (binding.propertyName)
            {
                case "position.x" when spriteTransform.position.x != value:
                    spriteTransform.position.x = value; break;
                case "position.y" when spriteTransform.position.y != value:
                    spriteTransform.position.y = value; break;
                case "scale.x" when spriteTransform.scale.x != value:
                    spriteTransform.scale.x = value; break;
                case "scale.y" when spriteTransform.scale.y != value:
                    spriteTransform.scale.y = value; break;
                case "skew.x" when spriteTransform.skew.x != value:
                    spriteTransform.skew.x = value; break;
                case "skew.y" when spriteTransform.skew.y != value:
                    spriteTransform.skew.y = value; break;
                default: continue;
            }

            changedTransforms.Add(spriteTransform);
        }

        foreach (var spriteTransform in changedTransforms.OrderBy(item => GetDepth(item.transform)))
        {
            spriteTransform.Apply();
            EditorUtility.SetDirty(spriteTransform);
        }
        return changedTransforms.Count > 0;
    }

    private static int GetDepth(Transform transform)
    {
        var depth = 0;
        while (transform.parent != null)
        {
            depth++;
            transform = transform.parent;
        }
        return depth;
    }

    private static float ParseFloat(JsonData value)
    {
        return float.Parse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static AnimationCurve BuildLinearCurve(JsonData frames, string property)
    {
        var curve = new AnimationCurve();
        for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
        {
            var frame = frames[frameIndex];
            var time = ParseFloat(frame["fram"]) / FrameRate;
            curve.AddKey(new Keyframe(time, ParseFloat(frame[property])));
        }

        // The XML stores sampled transforms and has no easing metadata. Linear
        // intervals keep independently keyed pieces, especially the two stalk
        // sprites and the head anchor, joined between source samples.
        for (var keyIndex = 0; keyIndex < curve.length; keyIndex++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
        }

        return curve;
    }

    private static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
    {
        if (left == null || right == null || left.length != right.length) return false;
        for (var index = 0; index < left.length; index++)
        {
            var leftKey = left.keys[index];
            var rightKey = right.keys[index];
            if (Mathf.Abs(leftKey.time - rightKey.time) > 0.000001f ||
                Mathf.Abs(leftKey.value - rightKey.value) > 0.000001f ||
                Mathf.Abs(leftKey.inTangent - rightKey.inTangent) > 0.0001f ||
                Mathf.Abs(leftKey.outTangent - rightKey.outTangent) > 0.0001f)
            {
                return false;
            }
        }
        return true;
    }

    private static bool EventsEqual(AnimationEvent[] left, AnimationEvent[] right)
    {
        if (left.Length != right.Length) return false;
        for (var index = 0; index < left.Length; index++)
        {
            if (left[index].functionName != right[index].functionName ||
                Mathf.Abs(left[index].time - right[index].time) > 0.000001f)
            {
                return false;
            }
        }
        return true;
    }
}
