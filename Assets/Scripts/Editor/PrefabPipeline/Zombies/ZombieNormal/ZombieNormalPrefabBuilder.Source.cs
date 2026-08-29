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
        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefab", "Zombie");
            EnsureFolder("Assets/Prefab/Zombie", "ZombieNormal");
            EnsureFolder(BasePath, "Sprite");
            EnsureFolder(BasePath, "Animation");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void ConfigureTextureImporters()
        {
            var pixelsPerUnit = ReadWalkPixelsPerUnit();
            foreach (var part in Parts)
            {
                var path = $"{SpritePath}/{part.Name}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new MissingReferenceException($"Missing zombie sprite: {path}");
                if (!pixelsPerUnit.TryGetValue(part.Name, out var partPixelsPerUnit))
                {
                    throw new InvalidDataException($"Walk XML has no size for zombie part: {part.Name}");
                }
    
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = partPixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, float> ReadWalkPixelsPerUnit()
        {
            var xmlAbsolutePath = Path.Combine(Application.dataPath, WalkXmlPath.Substring("Assets/".Length));
            if (!File.Exists(xmlAbsolutePath))
            {
                throw new FileNotFoundException("Missing normal zombie walk XML.", xmlAbsolutePath);
            }
    
            var document = new XmlDocument();
            document.Load(xmlAbsolutePath);
            var layers = document.SelectNodes("/animate/layer");
            if (layers == null) throw new InvalidDataException($"Walk XML has no layers: {WalkXmlPath}");
    
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (XmlNode layer in layers)
            {
                var layerName = layer.Attributes?["name"]?.Value;
                if (string.IsNullOrEmpty(layerName) || layerName == "_ground") continue;
    
                var targetName = ResolveWalkLayerPath(layerName);
                var texturePath = $"{SpritePath}/{targetName}.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null) throw new MissingReferenceException($"Missing zombie texture: {texturePath}");
    
                var authoredWidth = ParseXmlFloat(layer, "width");
                var authoredHeight = ParseXmlFloat(layer, "height");
                if (authoredWidth <= 0f || authoredHeight <= 0f)
                {
                    throw new InvalidDataException($"Walk XML layer has an invalid size: {layerName}");
                }
    
                var widthRatio = texture.width / authoredWidth;
                var heightRatio = texture.height / authoredHeight;
                var ppu = ChoosePixelsPerUnit(targetName, widthRatio, heightRatio);
                if (result.TryGetValue(targetName, out var existing) && !Mathf.Approximately(existing, ppu))
                {
                    throw new InvalidDataException($"Walk XML gives conflicting sizes for {targetName}.");
                }
                result[targetName] = ppu;
            }
            return result;
        }

        private static float ChoosePixelsPerUnit(string partName, float widthRatio, float heightRatio)
        {
            if (Mathf.Abs(widthRatio - heightRatio) <= 0.0001f)
            {
                return (widthRatio + heightRatio) * 0.5f;
            }
    
            var widthSimple = NearestSimpleFraction(widthRatio);
            var heightSimple = NearestSimpleFraction(heightRatio);
            var widthError = Mathf.Abs(widthRatio - widthSimple);
            var heightError = Mathf.Abs(heightRatio - heightSimple);
            var chosen = widthError <= heightError ? widthSimple : heightSimple;
            Debug.LogWarning(
                $"{partName} has mismatched PPU ratios ({widthRatio} vs {heightRatio}); " +
                $"using the more plausible simple value {chosen}.");
            return chosen;
        }

        private static float NearestSimpleFraction(float value)
        {
            var best = value;
            var bestError = float.PositiveInfinity;
            for (var denominator = 1; denominator <= 12; denominator++)
            {
                var numerator = Mathf.RoundToInt(value * denominator);
                var candidate = (float)numerator / denominator;
                var error = Mathf.Abs(value - candidate);
                if (error < bestError - 0.000001f)
                {
                    best = candidate;
                    bestError = error;
                }
            }
            return best;
        }

        private static AnimationClip CreateAnimationClip(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath) == null)
            {
                var sourceAbsolutePath = Path.Combine(
                    Application.dataPath,
                    sourcePath.Substring("Assets/".Length));
                var destinationAbsolutePath = Path.Combine(
                    Application.dataPath,
                    destinationPath.Substring("Assets/".Length));
                if (!File.Exists(sourceAbsolutePath))
                {
                    throw new FileNotFoundException("Missing generated idle animation source.", sourceAbsolutePath);
                }
    
                File.Copy(sourceAbsolutePath, destinationAbsolutePath, false);
            }
    
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (clip == null) throw new MissingReferenceException($"Failed to import animation clip: {destinationPath}");
    
            clip.frameRate = 12f;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip CreateReanimationClip(
            string xmlPath,
            string destinationPath,
            string clipName,
            bool includeRootMotion)
        {
            var xmlAbsolutePath = Path.Combine(Application.dataPath, xmlPath.Substring("Assets/".Length));
            if (!File.Exists(xmlAbsolutePath))
            {
                throw new FileNotFoundException("Missing normal zombie walk XML.", xmlAbsolutePath);
            }
    
            var document = new XmlDocument();
            document.Load(xmlAbsolutePath);
            var layers = document.SelectNodes("/animate/layer");
            if (layers == null || layers.Count == 0)
            {
                throw new InvalidDataException($"Walk XML has no animation layers: {xmlPath}");
            }
    
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, destinationPath);
            }
    
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }
    
            clip.name = clipName;
            clip.frameRate = 12f;
            foreach (XmlNode layer in layers)
            {
                var layerName = layer.Attributes?["name"]?.Value;
                var frames = layer.SelectNodes("frame");
                if (string.IsNullOrEmpty(layerName) || frames == null || frames.Count == 0)
                {
                    throw new InvalidDataException("Walk XML contains an unnamed or empty layer.");
                }
    
                if (layerName == "_ground")
                {
                    if (!includeRootMotion)
                    {
                        throw new InvalidDataException($"Unexpected _ground layer in {xmlPath}.");
                    }
                    var initialGroundX = ParseXmlFloat(frames[0], "posx");
                    SetLinearCurve(
                        clip,
                        string.Empty,
                        typeof(Transform),
                        "m_LocalPosition.x",
                        CreateXmlCurve(frames, "posx", value => -(value - initialGroundX)));
                    continue;
                }
    
                var targetPath = PartPathPrefix + ResolveWalkLayerPath(layerName);
                SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "position.x", CreateXmlCurve(frames, "posx"));
                SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "position.y", CreateXmlCurve(frames, "posy"));
                SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "scale.x", CreateXmlCurve(frames, "scalex"));
                SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "scale.y", CreateXmlCurve(frames, "scaley"));
                SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "skew.x", CreateXmlCurve(frames, "skewx"));
                SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "skew.y", CreateXmlCurve(frames, "skewy"));
            }
    
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            return clip;
        }

        private static AnimationCurve CreateXmlCurve(
            XmlNodeList frames,
            string attribute,
            Func<float, float> convert = null)
        {
            var keys = new Keyframe[frames.Count];
            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                var time = ParseXmlFloat(frame, "index") / 12f;
                var value = ParseXmlFloat(frame, attribute);
                keys[i] = new Keyframe(time, convert == null ? value : convert(value));
            }
            return new AnimationCurve(keys);
        }

        private static float ParseXmlFloat(XmlNode node, string attribute)
        {
            var text = node.Attributes?[attribute]?.Value;
            if (string.IsNullOrEmpty(text) ||
                !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new InvalidDataException($"Walk XML frame is missing a valid '{attribute}' value.");
            }
            return value;
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            Type type,
            string propertyName,
            AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName), curve);
        }

        private static string ResolveWalkLayerPath(string layerName)
        {
            return layerName switch
            {
                "anim_head1" => "Zombie_head",
                "anim_head2" => "Zombie_jaw",
                "anim_innerarm1" => "Zombie_innerarm_upper",
                "anim_innerarm2" => "Zombie_innerarm_lower",
                "anim_innerarm3" => "Zombie_innerarm_hand",
                _ => layerName
            };
        }
    }
}
