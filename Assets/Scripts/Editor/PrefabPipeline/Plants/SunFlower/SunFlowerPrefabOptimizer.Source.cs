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
using static PvZ.Editor.PrefabPipeline.Common.AnimationAssetUtility;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.SunFlower
{
    public static partial class SunFlowerPrefabOptimizer
    {
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
    }
}
