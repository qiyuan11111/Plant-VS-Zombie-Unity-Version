using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Presentation.Shadows;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using PvZ.Config;
using PvZ.Gameplay.Board;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PvZ.Tests.EditMode.Plants
{
    public sealed partial class SunFlowerConfigurationTests
    {
    [Test]
            public void CardPresentation_PlacesCenteredRootAtConfiguredPacketPosition()
            {
                var config = Resources.Load<GameConfigObject>("GameConfigObject");
                Assert.That(config, Is.Not.Null);
                config.Init();
    
                var packet = new GameObject("Packet", typeof(RectTransform)).GetComponent<RectTransform>();
                packet.sizeDelta = new Vector2(50f, 70f);
                packet.pivot = new Vector2(0.5f, 0.5f);
                var definition = config.GetPlantDefinition(PlantType.PeaShooterSingle);
                var instance = Object.Instantiate(definition.PresentationPrefab, packet, false);
                try
                {
                    var plant = instance.GetComponent<PlantEntity>();
                    plant.ResetRuntimeState();
                    EntityPresentation.ConfigureCardIcon(plant, definition);
                    Assert.That(plant.transform.localPosition,
                        Is.EqualTo((Vector3)definition.SeedPacketLocalPosition));
                    Assert.That(definition.SeedPacketScale, Is.EqualTo(0.5f));
                }
                finally
                {
                    Object.DestroyImmediate(packet.gameObject);
                }
            }

    [Test]
            public void CardPresentation_SamplesOriginalSunflowerNormalizedTime()
            {
                var config = Resources.Load<GameConfigObject>("GameConfigObject");
                Assert.That(config, Is.Not.Null);
                config.Init();
    
                var definition = config.GetPlantDefinition(PlantType.SunFlower);
                var instance = Object.Instantiate(definition.PresentationPrefab);
                try
                {
                    var animator = instance.GetComponent<Animator>();
                    Assert.That(animator, Is.Not.Null);
                    var sampleMethod = typeof(EntityPresentation).GetMethod(
                        "SampleAnimatorTime",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.That(sampleMethod, Is.Not.Null);
                    sampleMethod.Invoke(null, new object[]
                    {
                        animator,
                        definition.PresentationNormalizedTime
                    });
    
                    var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        "Assets/Prefab/Plant/SunFlower/Animation/idle.anim");
                    Assert.That(idle, Is.Not.Null);
    
                    var binding = AnimationUtility.GetCurveBindings(idle).First(candidate =>
                        candidate.type == typeof(SpriteTransform) &&
                        candidate.path == HeadPath &&
                        candidate.propertyName == "position.x");
                    var curve = AnimationUtility.GetEditorCurve(idle, binding);
                    var head = instance.transform.Find(binding.path).GetComponent<SpriteTransform>();
    
                    Assert.That(head.position.x,
                        Is.EqualTo(curve.Evaluate(idle.length * 0.15f)).Within(0.0002f));
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

    [Test]
            public void PresentationPrefab_UsesNativeHierarchyForEverySpriteTransform()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefab/Plant/SunFlower/SunFlower.prefab");
                var spriteTransforms = prefab.GetComponentsInChildren<SpriteTransform>(true);
    
                Assert.That(spriteTransforms, Is.Not.Empty);
                Assert.That(prefab.transform.Find(BasicPath)?.GetComponent<SpriteTransform>(), Is.Not.Null);
                foreach (var spriteTransform in spriteTransforms)
                {
                    var target = spriteTransform.transform;
                    var path = AnimationUtility.CalculateTransformPath(target, prefab.transform);
                    Assert.That(spriteTransform.NativeContent, Is.Not.Null, path);
                    Assert.That(spriteTransform.NativeContent.parent, Is.SameAs(target), path);
                    Assert.That(spriteTransform.NativeContent.localPosition, Is.EqualTo(Vector3.zero), path);
                    Assert.That(target.GetComponent<SpriteRenderer>(), Is.Null, path);
                    Assert.That(target.childCount, Is.EqualTo(1), path);
                }
    
                Assert.That(prefab.GetComponentsInChildren<Transform>(true)
                        .Count(item => item.name == "SunFlower_blink1"),
                    Is.EqualTo(1));
                Assert.That(prefab.GetComponentsInChildren<Transform>(true)
                        .Count(item => item.name == "SunFlower_blink2"),
                    Is.EqualTo(1));
            }

    [Test]
            public void NativeHierarchy_KeepsBrightnessAndAlphaInRendererMaterialProperties()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefab/Plant/SunFlower/SunFlower.prefab");
                var instance = Object.Instantiate(prefab);
                try
                {
                    var spriteTransform = instance.transform.Find(HeadPath).GetComponent<SpriteTransform>();
                    var renderer = spriteTransform.VisualRenderer;
                    spriteTransform.brightness = 1.75f;
                    spriteTransform.alpha = 0.8f;
                    spriteTransform.alphaCoef = 0.5f;
                    spriteTransform.Apply();
    
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat("_Brightness"), Is.EqualTo(1.75f).Within(0.0001f));
                    Assert.That(properties.GetFloat("_Alpha"), Is.EqualTo(0.4f).Within(0.0001f));
                    Assert.That(renderer.sharedMaterial.HasProperty("_SkewX"), Is.False);
                    Assert.That(renderer.sharedMaterial.HasProperty("_ScaleX"), Is.False);
                    Assert.That(renderer.sharedMaterial.HasProperty("_AffineRow0"), Is.False);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
    }
}
