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
        private static void Validate(
            GameObject prefab,
            AnimationClip idle,
            AnimationClip walk,
            RuntimeAnimatorController controller)
        {
            if (prefab.GetComponent<ZombieNormalEntity>() == null) throw new MissingComponentException("ZombieNormal component is missing.");
            if (prefab.GetComponent<SpriteGroup>() == null) throw new MissingComponentException("SpriteGroup component is missing.");
            if (prefab.GetComponent<Animator>()?.runtimeAnimatorController != controller)
            {
                throw new MissingReferenceException("ZombieNormal Animator Controller is not assigned.");
            }
            if (!prefab.GetComponent<Animator>().applyRootMotion)
            {
                throw new InvalidOperationException("ZombieNormal must apply the walk clip's root motion.");
            }
    
            var zombie = prefab.GetComponent<ZombieNormalEntity>();
            var expectedShadowCenter = new Vector3(
                ShadowLocalPosition.x,
                ShadowLocalPosition.y,
                0f);
            if (!zombie.DrawsShadow ||
                Vector3.Distance(zombie.ShadowCenterLocalPosition, expectedShadowCenter) > 0.0001f ||
                Mathf.Abs(zombie.ShadowScale - 1f) > 0.0001f)
            {
                throw new InvalidOperationException("ZombieNormal has an invalid decomp-compatible shadow configuration.");
            }
    
            var component = prefab.transform.Find(ComponentPath);
            var basic = prefab.transform.Find(BasicPath);
            var basicVisual = prefab.transform.Find(BasicVisualPath);
            var anchors = prefab.transform.Find(AnchorsPath);
            if (component == null || basic == null || basicVisual == null || anchors == null)
            {
                throw new MissingReferenceException(
                    "ZombieNormal must contain component/basic/__AffineContent and component/anchors.");
            }
            if (prefab.transform.childCount != 1 || component.parent != prefab.transform)
            {
                throw new InvalidOperationException("ZombieNormal root must have component as its only direct child.");
            }
    
            var basicTransform = basic.GetComponent<SpriteTransform>();
            if (basicTransform == null || basicTransform.NativeContent != basicVisual)
            {
                throw new MissingComponentException("ZombieNormal basic container has no valid SpriteTransform hierarchy.");
            }
            if (basicTransform.providesChildSpritePosition ||
                basicTransform.providesChildSpriteAffine ||
                basicTransform.updatePosition ||
                Vector2.Distance(basicTransform.spritePosition, Vector2.zero) > 0.0001f)
            {
                throw new InvalidOperationException("ZombieNormal basic container must use the centered-root contract.");
            }
    
            foreach (var binding in AnimationUtility.GetCurveBindings(idle))
            {
                if (string.IsNullOrEmpty(binding.path)) continue;
                var target = prefab.transform.Find(binding.path);
                if (target == null) throw new MissingReferenceException($"Idle animation target is missing: {binding.path}");
            }
    
            var expectedPartPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in Parts) expectedPartPaths.Add(PartPathPrefix + part.Name);
            var walkPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(walk))
            {
                if (!string.IsNullOrEmpty(binding.path) && binding.type == typeof(SpriteTransform))
                {
                    walkPaths.Add(binding.path);
                }
            }
            if (!expectedPartPaths.SetEquals(walkPaths))
            {
                throw new InvalidOperationException("Walk XML bindings do not exactly match the standard prefab part paths.");
            }
    
            foreach (var part in Parts)
            {
                var target = prefab.transform.Find(PartPathPrefix + part.Name);
                if (target == null || target.parent != basicVisual)
                {
                    throw new MissingReferenceException($"Zombie part is not under the standard basic visual container: {part.Name}");
                }
                if (target.GetComponent<SpriteTransform>() == null)
                {
                    throw new MissingComponentException($"Zombie part has no SpriteTransform: {part.Name}");
                }
            }
    
            if (prefab.GetComponentsInChildren<SpriteTransform>(true).Length != Parts.Length + 1)
            {
                throw new InvalidOperationException("ZombieNormal must contain one basic transform plus the exact XML part count.");
            }
            if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != Parts.Length)
            {
                throw new InvalidOperationException("ZombieNormal does not contain the expected number of sprite renderers.");
            }
    
            var collider = prefab.GetComponent<BoxCollider2D>();
            if (collider == null ||
                Vector2.Distance(collider.offset, ColliderOffset) > 0.0001f ||
                Vector2.Distance(collider.size, ColliderSize) > 0.0001f)
            {
                throw new InvalidOperationException("ZombieNormal collider is not expressed relative to the centered root.");
            }
    
            var zombieBody = prefab.GetComponent<ZombieBodyCollider>();
            if (zombieBody == null ||
                zombieBody.Collider != collider ||
                zombieBody.Zombie != zombie ||
                zombie.ZombieBody != zombieBody)
            {
                throw new InvalidOperationException("ZombieNormal has an invalid detected-collider binding.");
            }
    
            ValidateWalkRootMotion(walk);
        }

        private static void ValidateWalkRootMotion(AnimationClip walk)
        {
            AnimationCurve rootMotion = null;
            foreach (var binding in AnimationUtility.GetCurveBindings(walk))
            {
                if (string.IsNullOrEmpty(binding.path) &&
                    binding.type == typeof(Transform) &&
                    binding.propertyName == "m_LocalPosition.x")
                {
                    rootMotion = AnimationUtility.GetEditorCurve(walk, binding);
                    break;
                }
            }
    
            if (rootMotion == null || rootMotion.length < 2)
            {
                throw new MissingReferenceException("Walk animation has no root m_LocalPosition.x curve.");
            }
    
            var start = rootMotion.keys[0].value;
            var end = rootMotion.keys[rootMotion.length - 1].value;
            if (Mathf.Abs(start) > 0.001f || end >= -0.001f)
            {
                throw new InvalidOperationException(
                    $"Walk root motion must start at zero and move left, but is {start} -> {end}.");
            }
        }
    }
}
