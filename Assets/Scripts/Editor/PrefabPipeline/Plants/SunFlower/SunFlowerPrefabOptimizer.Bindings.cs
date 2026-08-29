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
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.SunFlower
{
    public static partial class SunFlowerPrefabOptimizer
    {
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

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
