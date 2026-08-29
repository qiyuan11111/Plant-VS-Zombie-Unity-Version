using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using PvZ.Config;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Detection.Zombies;
using PvZ.Gameplay.Zombies;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ZombieNormalEntity = PvZ.Gameplay.Zombies.Types.ZombieNormal;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Zombies.ZombieNormal
{
    public static partial class ZombieNormalPrefabBuilder
    {
        private static GameObject CreatePrefab(AnimationClip defaultPose, RuntimeAnimatorController controller)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return UpdateExistingPrefab(defaultPose, controller);
            }
    
            var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
            if (material == null) throw new MissingReferenceException($"Missing sprite material: {SharedMaterialPath}");
    
            var zombieLayer = LayerMask.NameToLayer("Zombie");
            if (zombieLayer < 0) throw new InvalidOperationException("The project has no Zombie layer.");
    
            var root = new GameObject("ZombieNormal") { layer = zombieLayer };
            try
            {
                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    
                root.AddComponent<ZombieNormalEntity>();
                root.AddComponent<SpriteGroup>();
    
                ConfigureCollider(root);
                ConfigureDetectedCollider(root);
                SynchronizeParts(root.transform);
                ConfigureShadow(root);
    
                // Apply the authored first frame so the prefab thumbnail and scene view
                // match the state that the Animator produces at runtime.
                defaultPose.SampleAnimation(root, 0f);
                foreach (var spriteTransform in root.GetComponentsInChildren<SpriteTransform>(true))
                {
                    spriteTransform.Apply();
                }
    
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new InvalidOperationException($"Failed to save prefab: {PrefabPath}");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject UpdateExistingPrefab(
            AnimationClip defaultPose,
            RuntimeAnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var animator = root.GetComponent<Animator>();
                if (animator == null) throw new MissingComponentException("ZombieNormal Animator is missing.");
    
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                SynchronizeParts(root.transform);
                ConfigureCollider(root);
                ConfigureDetectedCollider(root);
                ConfigureShadow(root);
                defaultPose.SampleAnimation(root, 0f);
                foreach (var spriteTransform in root.GetComponentsInChildren<SpriteTransform>(true))
                {
                    spriteTransform.Apply();
                }
    
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new InvalidOperationException($"Failed to update prefab: {PrefabPath}");
                return prefab;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SynchronizeParts(Transform root)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
            if (material == null) throw new MissingReferenceException($"Missing sprite material: {SharedMaterialPath}");
            var zombieLayer = LayerMask.NameToLayer("Zombie");
            if (zombieLayer < 0) throw new InvalidOperationException("The project has no Zombie layer.");
    
            root.gameObject.layer = zombieLayer;
            SetIdentity(root);
            var component = GetOrCreateChild(root, "component", zombieLayer);
            var basic = GetOrCreateChild(component, "basic", zombieLayer);
            var anchors = GetOrCreateChild(component, "anchors", zombieLayer);
            SetIdentity(component);
            SetIdentity(basic);
            SetIdentity(anchors);
    
            var basicTransform = basic.GetComponent<SpriteTransform>();
            if (basicTransform == null) basicTransform = basic.gameObject.AddComponent<SpriteTransform>();
            basicTransform.position = Vector2.zero;
            basicTransform.scale = new Vector2(100f, 100f);
            basicTransform.skew = Vector2.zero;
            basicTransform.brightness = 1f;
            basicTransform.alpha = 1f;
            basicTransform.alphaCoef = 1f;
            basicTransform.updatePosition = false;
            basicTransform.providesChildSpritePosition = false;
            basicTransform.providesChildSpriteAffine = false;
            basicTransform.spritePosition = Vector2.zero;
            basicTransform.spriteScale = new Vector2(100f, 100f);
            basicTransform.spriteSkew = Vector2.zero;
    
            var basicContent = GetOrCreateChild(basic, SpriteTransform.NativeContentName, zombieLayer);
            SetIdentity(basicContent);
            basicTransform.ConfigureNativeHierarchy(basicContent);
    
            var expectedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in Parts) expectedNames.Add(part.Name);
    
            var existingTransforms = root.GetComponentsInChildren<SpriteTransform>(true);
            foreach (var spriteTransform in existingTransforms)
            {
                if (spriteTransform == basicTransform) continue;
                if (!expectedNames.Contains(spriteTransform.name))
                {
                    UnityEngine.Object.DestroyImmediate(spriteTransform.gameObject);
                }
            }
    
            for (var partIndex = 0; partIndex < Parts.Length; partIndex++)
            {
                var part = Parts[partIndex];
                Transform target = null;
                foreach (var spriteTransform in root.GetComponentsInChildren<SpriteTransform>(true))
                {
                    if (spriteTransform != basicTransform && spriteTransform.name == part.Name)
                    {
                        target = spriteTransform.transform;
                        break;
                    }
                }
    
                if (target == null)
                {
                    target = CreatePart(basicContent, part, zombieLayer, material).transform;
                }
                else if (target.parent != basicContent)
                {
                    target.SetParent(basicContent, false);
                }
    
                target.SetSiblingIndex(partIndex);
                SetLayerRecursively(target.gameObject, zombieLayer);
            }
    
            basicTransform.RefreshDescendantPositionReferences();
        }

        private static void SetIdentity(Transform target)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private static void ConfigureCollider(GameObject root)
        {
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider == null) collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.offset = ColliderOffset;
            collider.size = ColliderSize;
        }

        private static void ConfigureDetectedCollider(GameObject root)
        {
            var zombie = root.GetComponent<ZombieNormalEntity>();
            var collider = root.GetComponent<BoxCollider2D>();
            if (zombie == null || collider == null)
            {
                throw new MissingComponentException(
                    "ZombieNormal requires its entity and body Collider2D before detection is configured.");
            }
    
            var zombieBody = root.GetComponent<ZombieBodyCollider>();
            if (zombieBody == null) zombieBody = root.AddComponent<ZombieBodyCollider>();
            zombieBody.Configure(zombie, collider);
            zombie.ConfigureBodyCollider(zombieBody);
            EditorUtility.SetDirty(zombieBody);
        }

        private static void ConfigureShadow(GameObject root)
        {
            var zombie = root.GetComponent<ZombieNormalEntity>();
            if (zombie == null) throw new MissingComponentException("ZombieNormal component is missing.");
    
            var serializedZombie = new SerializedObject(zombie);
            serializedZombie.FindProperty("drawsShadow").boolValue = true;
            serializedZombie.FindProperty("shadowCenterLocalPosition").vector2Value = ShadowLocalPosition;
            serializedZombie.FindProperty("shadowScale").floatValue = 1f;
            serializedZombie.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePart(Transform root, Part part, int layer, Material material)
        {
            var spritePath = $"{SpritePath}/{part.Name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) throw new MissingReferenceException($"Sprite was not imported: {spritePath}");
    
            var pivot = new GameObject(part.Name) { layer = layer };
            pivot.transform.SetParent(root, false);
    
            var spriteTransform = pivot.AddComponent<SpriteTransform>();
            spriteTransform.position = Vector2.zero;
            spriteTransform.scale = new Vector2(100f, 100f);
            spriteTransform.skew = Vector2.zero;
            spriteTransform.brightness = 1f;
            spriteTransform.alpha = 1f;
            spriteTransform.alphaCoef = 1f;
            spriteTransform.updatePosition = true;
    
            var content = new GameObject(SpriteTransform.NativeContentName) { layer = layer };
            content.transform.SetParent(pivot.transform, false);
            var renderer = content.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = SortingLayer;
            renderer.sortingOrder = part.SortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;
    
            spriteTransform.ConfigureNativeHierarchy(content.transform);
            return pivot;
        }

        private static void ConnectGameConfig(GameObject prefab)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigObject>(GameConfigPath);
            if (config == null) throw new MissingReferenceException($"Missing game config: {GameConfigPath}");
    
            config.ZombieNormal = prefab;
            EditorUtility.SetDirty(config);
        }
    }
}
