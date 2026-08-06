using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LitJson;
using PvZ.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PeaShooterSinglePrefabOptimizer
{
    private const string PrefabPath = "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab";
    private const string SourcePath = "Assets/StreamingAssets/PeaShooterSingle.json";
    private const string IdleClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/idle.anim";
    private const string HeadIdleClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/head_idle.anim";
    private const string ShootClipPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/shoot.anim";
    private const string ControllerPath = "Assets/Prefab/Plant/PeaShooterSingle/Animation/PeaShooterSingle.controller";
    private const string OptimizationVersion = "pea-shooter-single-animation-v2-linear-xml-curves";
    private const float FrameRate = 12f;

    private static readonly Dictionary<string, string> PartPaths = new()
    {
        { "PeaShooterSingle_backleaf", "component/basic/leaf/backleaf/backleaf" },
        { "PeaShooterSingle_backleaf_left_tip", "component/basic/leaf/backleaf/left_tip" },
        { "PeaShooterSingle_backleaf_right_tip", "component/basic/leaf/backleaf/right_tip" },
        { "PeaShooterSingle_front_leaf", "component/basic/leaf/frontleaf/frontleaf" },
        { "PeaShooterSingle_frontleaf_left_tip", "component/basic/leaf/frontleaf/left_tip" },
        { "PeaShooterSingle_frontleaf_right_tip", "component/basic/leaf/frontleaf/right_tip" },
        { "PeaShooterSingle_head", "component/basic/head" },
        { "PeaShooterSingle_stalk_bottom", "component/basic/stalk/bottom" },
        { "PeaShooterSingle_stalk_top", "component/basic/stalk/top" },
        { "PeaShooterSingle_head/PeaShooterSingle_head", "component/basic/head/pod/head" },
        { "PeaShooterSingle_head/PeaShooterSingle_mouth", "component/basic/head/pod/mouth" },
        { "PeaShooterSingle_head/PeaShooterSingle_sprout", "component/basic/head/sprout" }
    };

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

        if (!File.Exists(GetAbsoluteSourcePath()) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            return;
        }

        try
        {
            var source = LoadSource();
            if (IsCurrent(source)) return;
            OptimizePeaShooterSingle(source);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

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
        var idleClip = GetOrCreateClip(IdleClipPath, "idle");
        var headIdleClip = GetOrCreateClip(HeadIdleClipPath, "head_idle");
        var shootClip = GetOrCreateClip(ShootClipPath, "shoot");

        SynchronizeClip(idleClip, source["idle"], true, Array.Empty<AnimationEvent>());
        SynchronizeClip(headIdleClip, source["head_idle"], true, Array.Empty<AnimationEvent>());
        SynchronizeClip(shootClip, source["shoot"], false, new[]
        {
            new AnimationEvent
            {
                time = 1f,
                functionName = "ShootProjectilePea"
            }
        });

        var controller = ConfigureController(idleClip, headIdleClip, shootClip);
        ConfigurePrefab(controller, idleClip, headIdleClip);

        var importer = AssetImporter.GetAtPath(PrefabPath);
        importer.userData = BuildOptimizationMarker();
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(PrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PeaShooterSingle prefab, controller and independent animation clips synchronized from JSON.");
    }

    private static bool IsCurrent(JsonData source)
    {
        var importer = AssetImporter.GetAtPath(PrefabPath);
        if (importer == null || importer.userData != BuildOptimizationMarker()) return false;

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        var headIdleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HeadIdleClipPath);
        var shootClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ShootClipPath);
        if (!ClipMatchesSource(idleClip, source["idle"], true, false) ||
            !ClipMatchesSource(headIdleClip, source["head_idle"], true, false) ||
            !ClipMatchesSource(shootClip, source["shoot"], false, true))
        {
            return false;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!ControllerIsCurrent(controller, idleClip, headIdleClip, shootClip))
        {
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var animator = prefab != null ? prefab.GetComponent<Animator>() : null;
        return animator != null && animator.runtimeAnimatorController == controller &&
               PartPaths.Values.All(path => prefab.transform.Find(path) != null) &&
               FirstFrameMatchesPrefab(prefab.transform, idleClip) &&
               FirstFrameMatchesPrefab(prefab.transform, headIdleClip);
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
        var sourceHash = Hash128.Compute(File.ReadAllText(GetAbsoluteSourcePath()));
        return $"{OptimizationVersion}-{sourceHash}";
    }

    private static AnimationClip GetOrCreateClip(string path, string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;

        clip = new AnimationClip { name = clipName };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
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
              Mathf.Abs(events[0].time - 1f) <= 0.000001f
            : events.Length == 0;
    }

    private static AnimatorController ConfigureController(
        AnimationClip idleClip,
        AnimationClip headIdleClip,
        AnimationClip shootClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        EnsureTrigger(controller, "shoot");
        ConfigureDefaultState(controller, "PeaShooterSingle", "idle", idleClip);
        ConfigureDefaultState(controller, "PeaShooterSingle_head", "head_idle", headIdleClip);
        ConfigureShootLayer(controller, shootClip);
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
        AnimationClip clip)
    {
        var layer = GetOrCreateLayer(controller, layerName);
        var state = GetOrCreateState(layer.stateMachine, stateName);
        state.motion = clip;
        layer.stateMachine.defaultState = state;
        SaveLayer(controller, layer);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(layer.stateMachine);
    }

    private static void ConfigureShootLayer(AnimatorController controller, AnimationClip shootClip)
    {
        var layer = GetOrCreateLayer(controller, "PeaShooterSingle_head_shoot");
        layer.defaultWeight = 1f;
        var stateMachine = layer.stateMachine;
        var shootState = GetOrCreateState(stateMachine, "shoot");
        shootState.motion = shootClip;

        var idleState = stateMachine.defaultState;
        if (idleState == null || idleState == shootState)
        {
            idleState = GetOrCreateState(stateMachine, "shoot_idle");
            stateMachine.defaultState = idleState;
        }

        var hasEnterTransition = idleState.transitions.Any(transition =>
            transition.destinationState == shootState &&
            transition.conditions.Any(condition => condition.parameter == "shoot"));
        if (!hasEnterTransition)
        {
            var transition = idleState.AddTransition(shootState);
            transition.duration = 0.25f;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "shoot");
        }

        if (!shootState.transitions.Any(transition => transition.destinationState == idleState))
        {
            var transition = shootState.AddTransition(idleState);
            transition.duration = 0.25f;
            transition.exitTime = 0.875f;
            transition.hasExitTime = true;
            transition.canTransitionToSelf = false;
        }

        SaveLayer(controller, layer);
        EditorUtility.SetDirty(idleState);
        EditorUtility.SetDirty(shootState);
        EditorUtility.SetDirty(stateMachine);
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
        AnimationClip shootClip)
    {
        if (!StateUsesClip(controller, "PeaShooterSingle", "idle", idleClip) ||
            !StateUsesClip(controller, "PeaShooterSingle_head", "head_idle", headIdleClip) ||
            !StateUsesClip(controller, "PeaShooterSingle_head_shoot", "shoot", shootClip) ||
            !controller.parameters.Any(parameter =>
                parameter.name == "shoot" && parameter.type == AnimatorControllerParameterType.Trigger))
        {
            return false;
        }

        var shootLayer = controller.layers.SingleOrDefault(layer => layer.name == "PeaShooterSingle_head_shoot");
        if (shootLayer == null) return false;
        var idleState = shootLayer.stateMachine.defaultState;
        var shootState = shootLayer.stateMachine.states
            .Select(childState => childState.state)
            .SingleOrDefault(state => state.name == "shoot");
        if (idleState == null || shootState == null || idleState == shootState) return false;

        return idleState.transitions.Any(transition =>
                   transition.destinationState == shootState &&
                   transition.conditions.Any(condition => condition.parameter == "shoot")) &&
               shootState.transitions.Any(transition => transition.destinationState == idleState);
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

    private static void ConfigurePrefab(
        RuntimeAnimatorController controller,
        AnimationClip idleClip,
        AnimationClip headIdleClip)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var changed = false;
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
            changed |= ApplyFirstFrame(root.transform, idleClip);
            changed |= ApplyFirstFrame(root.transform, headIdleClip);
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
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
