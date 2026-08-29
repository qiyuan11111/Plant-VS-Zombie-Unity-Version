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
using SunFlowerEntity = PvZ.Gameplay.Plants.Types.SunFlower;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.SunFlower
{
    public static partial class SunFlowerPrefabOptimizer
    {
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
    
            var sunFlower = GetOrAddComponent<SunFlowerEntity>(root);
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
    }
}
