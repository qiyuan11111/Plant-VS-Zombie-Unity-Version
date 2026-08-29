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

    }
}
