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
}
