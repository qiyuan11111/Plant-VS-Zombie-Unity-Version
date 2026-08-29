using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LitJson;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Detection.Plants;
using PvZ.Gameplay.Detection.Zombies;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Plants.Presentation.Animation;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using PeaShooterSingleEntity = PvZ.Gameplay.Plants.Types.PeaShooterSingle;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.PeaShooterSingle
{
    public static partial class PeaShooterSinglePrefabOptimizer
    {
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
                ConfigureCollision(root);
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
    
            var shooter = root.GetComponent<PeaShooterSingleEntity>();
            if (shooter != null)
            {
                var data = new SerializedObject(shooter);
                data.FindProperty("shadowLocalPosition").vector2Value = new Vector2(0.55f, -21.25f);
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureCollision(GameObject root)
        {
            var plantLayer = LayerMask.NameToLayer("Plant");
            var detectZombieLayer = LayerMask.NameToLayer("DetectZombieRegion");
            if (plantLayer < 0 || detectZombieLayer < 0)
            {
                throw new InvalidOperationException(
                    "Plant and DetectZombieRegion layers must exist before configuring PeaShooterSingle.");
            }
    
            var shooter = GetOrAddComponent<PeaShooterSingleEntity>(root);
            var bodyCollider = root.GetComponent<BoxCollider2D>();
            if (bodyCollider == null)
            {
                throw new MissingComponentException("PeaShooterSingle root requires its Plant BoxCollider2D.");
            }
    
            root.layer = plantLayer;
            var plantBody = GetOrAddComponent<PlantBodyCollider>(root);
            plantBody.Configure(shooter, bodyCollider);
            shooter.ConfigureBodyCollider(plantBody);
    
            var rigidbody = GetOrAddComponent<Rigidbody2D>(root);
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.simulated = true;
            rigidbody.gravityScale = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rigidbody.interpolation = RigidbodyInterpolation2D.None;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
    
            var detectRoot = GetOrCreatePath(root.transform, DetectPath);
            detectRoot.localPosition = Vector3.zero;
            detectRoot.localRotation = Quaternion.identity;
            detectRoot.localScale = Vector3.one;
            detectRoot.gameObject.layer = plantLayer;
    
            var zombieNode = GetOrCreatePath(root.transform, DetectZombiePath);
            zombieNode.localPosition = Vector3.zero;
            zombieNode.localRotation = Quaternion.identity;
            zombieNode.localScale = Vector3.one;
            zombieNode.gameObject.layer = detectZombieLayer;
    
            var detectorCollider = GetOrAddComponent<BoxCollider2D>(zombieNode.gameObject);
            detectorCollider.isTrigger = true;
            var detector = GetOrAddComponent<ZombieDetector>(zombieNode.gameObject);
            detector.Configure(shooter, detectorCollider);
    
            shooter.ConfigureShootingDetectorBinding(zombieNode);
            shooter.LoadDetectorCallbacks();
    
            EditorUtility.SetDirty(plantBody);
            EditorUtility.SetDirty(rigidbody);
            EditorUtility.SetDirty(detectorCollider);
            EditorUtility.SetDirty(detector);
        }

        private static bool PrefabCollisionIsCurrent(GameObject root)
        {
            if (root == null) return false;
    
            var shooter = root.GetComponent<PeaShooterSingleEntity>();
            var bodyCollider = root.GetComponent<BoxCollider2D>();
            var plantBody = root.GetComponent<PlantBodyCollider>();
            var rigidbody = root.GetComponent<Rigidbody2D>();
            var detectRoot = root.transform.Find(DetectPath);
            var zombieNode = root.transform.Find(DetectZombiePath);
            var detectorCollider = zombieNode != null
                ? zombieNode.GetComponent<BoxCollider2D>()
                : null;
            var detector = zombieNode != null
                ? zombieNode.GetComponent<ZombieDetector>()
                : null;
            var hasShootingDetector = shooter != null &&
                                      shooter.DetectorSlots.Count == 1 &&
                                      shooter.DetectorSlots[0].Transform == zombieNode &&
                                      shooter.DetectorSlots[0].Callback is IZombieDetectorCallback;
    
            return shooter != null &&
                   bodyCollider != null &&
                   plantBody != null &&
                   plantBody.Collider == bodyCollider &&
                   plantBody.Plant == shooter &&
                   shooter.PlantBody == plantBody &&
                   rigidbody != null &&
                   rigidbody.bodyType == RigidbodyType2D.Kinematic &&
                   detectRoot != null &&
                   detectRoot.parent == root.transform &&
                   zombieNode != null &&
                   zombieNode.parent == detectRoot &&
                   detectorCollider != null &&
                   detectorCollider.isTrigger &&
                   zombieNode.gameObject.layer == LayerMask.NameToLayer("DetectZombieRegion") &&
                   detector != null &&
                   detector.Collider == detectorCollider &&
                   detector.Owner == shooter &&
                   hasShootingDetector;
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
    
            var shooter = GetOrAddComponent<PeaShooterSingleEntity>(root);
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
    
            var shooter = root.GetComponent<PeaShooterSingleEntity>();
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

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
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
    }
}
