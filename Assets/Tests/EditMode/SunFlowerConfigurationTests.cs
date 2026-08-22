using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.World;
using PvZ.Presentation;
using PvZ.Config;
using PvZ.Gameplay.Board;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class SunFlowerConfigurationTests
    {
        private const string BasicPath = "component/basic";
        private const string BasicVisualPath = BasicPath + "/__AffineContent";
        private const string HeadPath = BasicVisualPath + "/head/SunFlower_head";
        private const string HeadVisualPath = HeadPath + "/__AffineContent";

        [Test]
        public void ProductionAnchor_IsFixedAtDefaultHeadCenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/Plant/SunFlower/SunFlower.prefab");
            Assert.That(prefab, Is.Not.Null);

            var anchor = prefab.transform.Find("component/anchors/Sun_Anchor");
            var head = prefab.transform.Find(HeadPath);
            Assert.That(anchor, Is.Not.Null);
            Assert.That(head, Is.Not.Null);
            Assert.That(anchor.parent, Is.SameAs(prefab.transform.Find("component/anchors")),
                "The production point must remain independent from animated head motion.");

            var anchorInPlantSpace = prefab.transform.InverseTransformPoint(anchor.position);
            var headInPlantSpace = prefab.transform.InverseTransformPoint(head.position);
            Assert.That(anchorInPlantSpace.x, Is.EqualTo(headInPlantSpace.x).Within(0.0001f));
            Assert.That(anchorInPlantSpace.y, Is.EqualTo(headInPlantSpace.y).Within(0.0001f));
            Assert.That(anchor.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(anchor.localScale, Is.EqualTo(Vector3.one));

            var producer = prefab.GetComponent<SunProducer>();
            Assert.That(producer, Is.Not.Null);
            var serializedProducer = new SerializedObject(producer);
            Assert.That(serializedProducer.FindProperty("productionAnchor").objectReferenceValue,
                Is.SameAs(anchor));
        }

        [Test]
        public void BlinkAnimation_ReturnsDedicatedSpritesToInactiveDefaults()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/Plant/SunFlower/SunFlower.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var animator = instance.GetComponent<Animator>();
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);

                var blink1 = instance.transform.Find(
                    HeadVisualPath + "/blink/SunFlower_blink1");
                var blink2 = instance.transform.Find(
                    HeadVisualPath + "/blink/SunFlower_blink2");
                var blinkLayer = animator.GetLayerIndex("SunFlower_blink");
                var idleState = Animator.StringToHash("blink_idle");

                animator.SetTrigger("blink");
                animator.Update(0.01f);
                animator.Update(0.01f);
                animator.Update(0.15f);
                for (var frame = 0; frame < 30; frame++)
                {
                    animator.Update(0.01f);
                }

                Assert.That(animator.GetCurrentAnimatorStateInfo(blinkLayer).shortNameHash,
                    Is.EqualTo(idleState));
                Assert.That(blink1.gameObject.activeSelf, Is.False);
                Assert.That(blink2.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Definition_ProvidesPlaceableSunProducingPrefab()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);

            config.Init();
            var definition = config.GetPlantDefinition(GameConfigObject.PlantType.SunFlower);

            Assert.That(definition.SunPrice, Is.EqualTo(50));
            Assert.That(definition.Cooldown, Is.EqualTo(5f));
            Assert.That(definition.PresentationNormalizedTime, Is.EqualTo(0.15f));
            Assert.That(definition.Prefab, Is.Not.Null);
            Assert.That(definition.Prefab.GetComponent<SunFlower>(), Is.Not.Null);
            Assert.That(definition.Prefab.GetComponent<SunProducer>(), Is.Not.Null);
        }

        [Test]
        public void Definition_DefaultPresentationTime_IsFirstFrame()
        {
            Assert.That(new PlantDefinition().PresentationNormalizedTime, Is.EqualTo(0f));
        }

        [Test]
        public void Plants_UseCenteredRootShadowPositions()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            config.Init();

            Assert.That(Shadow.GetScale(ShadowSizePreset.Large), Is.EqualTo(1f));
            Assert.That(Shadow.GetScale(ShadowSizePreset.Small), Is.EqualTo(0.5f));
            Assert.That(
                Shadow.GetScale(ShadowSizePreset.Large),
                Is.GreaterThan(Shadow.GetScale(ShadowSizePreset.Small)));

            var sunShroom = config.GetPlantDefinition(GameConfigObject.PlantType.SunShroom)
                .Prefab.GetComponent<PlantEntity>();
            Assert.That(sunShroom.ShadowSize, Is.EqualTo(ShadowSizePreset.Small));
            Assert.That(sunShroom.ShadowLocalPosition,
                Is.EqualTo(new Vector2(-2.275f, -14.125f)));

            foreach (var type in new[]
                     {
                         GameConfigObject.PlantType.PeaShooterSingle,
                         GameConfigObject.PlantType.SunFlower
                     })
            {
                var plant = config.GetPlantDefinition(type).Prefab.GetComponent<PlantEntity>();
                Assert.That(plant, Is.Not.Null, type.ToString());
                Assert.That(plant.ShadowSize, Is.EqualTo(ShadowSizePreset.Large), type.ToString());
                var expected = type == GameConfigObject.PlantType.PeaShooterSingle
                    ? new Vector2(0.55f, -21.25f)
                    : new Vector2(-0.4f, -26.4f);
                Assert.That(plant.ShadowLocalPosition, Is.EqualTo(expected), type.ToString());
            }
        }

        [Test]
        public void AllPlantPrefabs_UseCenteredTopLevelRoots()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            config.Init();

            var parent = new GameObject("Parent").transform;
            try
            {
                foreach (GameConfigObject.PlantType type in System.Enum.GetValues(
                             typeof(GameConfigObject.PlantType)))
                {
                    var definition = config.GetPlantDefinition(type);
                    var instance = Object.Instantiate(definition.PresentationPrefab, parent, false);
                    var plant = instance.GetComponent<PlantEntity>();

                    Assert.That(plant, Is.Not.Null, type.ToString());
                    var basic = instance.transform.Find("component/basic")
                        ?.GetComponent<SpriteTransform>();
                    Assert.That(basic, Is.Not.Null, type.ToString());
                    Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero), type.ToString());
                    Assert.That(basic.transform.localPosition, Is.EqualTo(Vector3.zero), type.ToString());
                    Assert.That(basic.providesChildSpritePosition, Is.False, type.ToString());
                    Assert.That(basic.providesChildSpriteAffine, Is.False, type.ToString());
                    Assert.That(basic.spritePosition, Is.EqualTo(Vector2.zero), type.ToString());
                }
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void BoardPlacement_UsesGridCenterWithoutPlantSpecificCorrection()
        {
            var boardRoot = new GameObject("BoardRoot").transform;
            try
            {
                var instance = new GameObject("CenteredPlant", typeof(SpriteGroup), typeof(BoxCollider2D));
                instance.transform.SetParent(boardRoot, false);
                var plant = instance.AddComponent<CenteredPlacementTestPlant>();
                plant.ResetRuntimeState();
                var grid = new GridManager.Grid(
                    new Vector2Int(3, 2),
                    new Vector2(100f, 200f));
                var plantData = new SerializedObject(plant);
                plantData.FindProperty("drawsShadow").boolValue = false;
                plantData.ApplyModifiedPropertiesWithoutUndo();

                plant.EnterBoard(grid);

                Assert.That(plant.transform.localPosition,
                    Is.EqualTo(new Vector3(grid.Position.x, grid.Position.y, 10f)));
            }
            finally
            {
                Object.DestroyImmediate(boardRoot.gameObject);
            }
        }

        [Test]
        public void PlantShadowSprite_IsCenteredOnItsPrefabOrigin()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);

            var shadowSprite = config.PlanteShadow.transform.Find("plantshadow");
            Assert.That(shadowSprite, Is.Not.Null);
            Assert.That(shadowSprite.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(shadowSprite.localScale, Is.EqualTo(Vector3.one));
            var sprite = shadowSprite.GetComponent<SpriteTransform>().VisualRenderer.sprite;
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(86f, 36f)));
        }

        [Test]
        public void PlantDefinition_DefaultCardLayoutUsesCenteredRoot()
        {
            var definition = new PlantDefinition();
            Assert.That(definition.SeedPacketScale, Is.EqualTo(0.5f));
            Assert.That(definition.SeedPacketLocalPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CardPresentation_PlacesCenteredRootAtConfiguredPacketPosition()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            config.Init();

            var packet = new GameObject("Packet", typeof(RectTransform)).GetComponent<RectTransform>();
            packet.sizeDelta = new Vector2(50f, 70f);
            packet.pivot = new Vector2(0.5f, 0.5f);
            var definition = config.GetPlantDefinition(GameConfigObject.PlantType.PeaShooterSingle);
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

            var definition = config.GetPlantDefinition(GameConfigObject.PlantType.SunFlower);
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
        public void PresentationPrefab_HasProductionGlowAndParallelBlinkLayers()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            config.Init();

            var prefab = config.GetPlantDefinition(GameConfigObject.PlantType.SunFlower).PresentationPrefab;
            Assert.That(prefab.GetComponent<Blink>(), Is.Not.Null);
            var basicTransform = prefab.transform.Find("component/basic").GetComponent<SpriteTransform>();
            Assert.That(basicTransform, Is.Not.Null);
            Assert.That(basicTransform.providesChildSpritePosition, Is.False);
            Assert.That(basicTransform.providesChildSpriteAffine, Is.False);
            Assert.That(basicTransform.spritePosition, Is.EqualTo(Vector2.zero));
            Assert.That(prefab.transform.Find(BasicVisualPath + "/head/face"), Is.Null);
            var head = prefab.transform.Find(HeadPath);
            Assert.That(head, Is.Not.Null);
            var headTransform = head.GetComponent<SpriteTransform>();
            Assert.That(headTransform, Is.Not.Null);
            Assert.That(headTransform.providesChildSpritePosition, Is.True);
            Assert.That(headTransform.providesChildSpriteAffine, Is.True);
            Assert.That(headTransform.spritePosition, Is.EqualTo(headTransform.position));
            Assert.That(headTransform.spriteScale, Is.EqualTo(headTransform.scale));
            Assert.That(headTransform.spriteSkew, Is.EqualTo(headTransform.skew));
            var blink1 = head.Find("__AffineContent/blink/SunFlower_blink1");
            var blink2 = head.Find("__AffineContent/blink/SunFlower_blink2");
            Assert.That(blink1, Is.Not.Null);
            Assert.That(blink2, Is.Not.Null);
            Assert.That(blink1.gameObject.activeSelf, Is.False);
            Assert.That(blink2.gameObject.activeSelf, Is.False);
            var blink1Transform = blink1.GetComponent<SpriteTransform>();
            var blink2Transform = blink2.GetComponent<SpriteTransform>();
            Assert.That(blink1Transform.position,
                Is.EqualTo(new Vector2(2.57259f, -16.20185f)));
            Assert.That(blink1Transform.scale,
                Is.EqualTo(new Vector2(80f, 80.56224f)));
            Assert.That(blink1Transform.VisualRenderer.sortingOrder, Is.EqualTo(16));
            Assert.That(blink2Transform.position,
                Is.EqualTo(new Vector2(2.57259f, -16.20185f)));
            Assert.That(blink2Transform.scale,
                Is.EqualTo(new Vector2(80f, 80.56224f)));
            Assert.That(blink2Transform.VisualRenderer.sortingOrder, Is.EqualTo(15));

            foreach (var blink in new[] { blink1, blink2 })
            {
                var sprite = blink.GetComponent<SpriteTransform>().VisualRenderer.sprite;
                Assert.That(sprite.pivot.x, Is.EqualTo(sprite.rect.width * 0.5f).Within(0.001f));
                Assert.That(sprite.pivot.y, Is.EqualTo(sprite.rect.height * 0.5f).Within(0.001f));
            }

            var controller = prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters.Select(parameter => parameter.name),
                Is.SupersetOf(new[] { "produce", "blink" }));
            Assert.That(controller.layers.Select(layer => layer.name),
                Is.SupersetOf(new[] { "SunFlower", "SunFlower_sun", "SunFlower_blink" }));

            var sunClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Prefab/Plant/SunFlower/Animation/sun.anim");
            Assert.That(sunClip, Is.Not.Null);
            Assert.That(AnimationUtility.GetAnimationEvents(sunClip)
                    .Any(animationEvent =>
                        animationEvent.functionName == "ProduceSun" &&
                        animationEvent.intParameter == 1),
                Is.True);

            var blinkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Prefab/Plant/SunFlower/Animation/blink.anim");
            Assert.That(blinkClip, Is.Not.Null);
            Assert.That(blinkClip.frameRate, Is.EqualTo(12f));
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

        [Test]
        public void PresentationPrefab_DefaultPoseMatchesIdleFifthFrame()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/Plant/SunFlower/SunFlower.prefab");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Prefab/Plant/SunFlower/Animation/idle.anim");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(idle, Is.Not.Null);

            var sampleTime = 4f / idle.frameRate;
            var animatedTransforms = new HashSet<SpriteTransform>();
            foreach (var binding in AnimationUtility.GetCurveBindings(idle))
            {
                if (binding.type != typeof(SpriteTransform)) continue;

                var target = prefab.transform.Find(binding.path);
                Assert.That(target, Is.Not.Null, binding.path);
                var spriteTransform = target.GetComponent<SpriteTransform>();
                Assert.That(spriteTransform, Is.Not.Null, binding.path);
                var curve = AnimationUtility.GetEditorCurve(idle, binding);
                Assert.That(curve, Is.Not.Null, $"{binding.path}: {binding.propertyName}");

                var actual = GetAnimatedValue(spriteTransform, binding.propertyName);
                if (float.IsNaN(actual)) continue;

                Assert.That(actual, Is.EqualTo(curve.Evaluate(sampleTime)).Within(0.0002f),
                    $"{binding.path}: {binding.propertyName}");
                animatedTransforms.Add(spriteTransform);
            }

            foreach (var spriteTransform in animatedTransforms)
            {
                var sourceParent = FindNativeSourceParent(spriteTransform.transform.parent);
                if (sourceParent != null && spriteTransform.NativeContent != null)
                {
                    var nativeExpectedLocalPosition = ResolveSourceLocalPosition(
                        spriteTransform,
                        sourceParent);
                    var expectedWorldPosition = sourceParent.NativeContent.TransformPoint(
                        nativeExpectedLocalPosition);
                    Assert.That(spriteTransform.transform.position.x,
                        Is.EqualTo(expectedWorldPosition.x).Within(0.0002f), spriteTransform.name);
                    Assert.That(spriteTransform.transform.position.y,
                        Is.EqualTo(expectedWorldPosition.y).Within(0.0002f), spriteTransform.name);
                    continue;
                }

                var provider = FindPositionProvider(spriteTransform.transform.parent);
                var origin = provider != null ? provider.spritePosition : Vector2.zero;
                var delta = spriteTransform.position - origin;
                var expectedLocalPosition = new Vector3(
                    delta.x,
                    -delta.y,
                    spriteTransform.transform.localPosition.z);

                Assert.That(spriteTransform.transform.localPosition.x,
                    Is.EqualTo(expectedLocalPosition.x).Within(0.0002f), spriteTransform.name);
                Assert.That(spriteTransform.transform.localPosition.y,
                    Is.EqualTo(expectedLocalPosition.y).Within(0.0002f), spriteTransform.name);
            }
        }

        [Test]
        public void PresentationAnimations_TargetExistingHierarchyNodes()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/Plant/SunFlower/SunFlower.prefab");
            Assert.That(prefab, Is.Not.Null);

            var clipPaths = new[]
            {
                "Assets/Prefab/Plant/SunFlower/Animation/idle.anim",
                "Assets/Prefab/Plant/SunFlower/Animation/blink.anim",
                "Assets/Prefab/Plant/SunFlower/Animation/nosun.anim",
                "Assets/Prefab/Plant/SunFlower/Animation/sun.anim"
            };

            foreach (var clipPath in clipPaths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                Assert.That(clip, Is.Not.Null, clipPath);

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    Assert.That(
                        prefab.transform.Find(binding.path),
                        Is.Not.Null,
                        $"{clip.name} targets missing hierarchy path '{binding.path}'.");
                }
            }
        }

        private static float GetAnimatedValue(SpriteTransform spriteTransform, string propertyName)
        {
            switch (propertyName)
            {
                case "position.x": return spriteTransform.position.x;
                case "position.y": return spriteTransform.position.y;
                case "scale.x": return spriteTransform.scale.x;
                case "scale.y": return spriteTransform.scale.y;
                case "skew.x": return spriteTransform.skew.x;
                case "skew.y": return spriteTransform.skew.y;
                case "brightness": return spriteTransform.brightness;
                case "alpha": return spriteTransform.alpha;
                case "alphaCoef": return spriteTransform.alphaCoef;
                default: return float.NaN;
            }
        }

        private static SpriteTransform FindPositionProvider(Transform parent)
        {
            while (parent != null)
            {
                var spriteTransform = parent.GetComponent<SpriteTransform>();
                if (spriteTransform != null && spriteTransform.providesChildSpritePosition)
                {
                    return spriteTransform;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static SpriteTransform FindNativeSourceParent(Transform parent)
        {
            while (parent != null)
            {
                var spriteTransform = parent.GetComponent<SpriteTransform>();
                if (spriteTransform != null && spriteTransform.NativeContent != null &&
                    (spriteTransform.updatePosition ||
                     spriteTransform.providesChildSpritePosition ||
                     spriteTransform.providesChildSpriteAffine))
                {
                    return spriteTransform;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static Vector3 ResolveSourceLocalPosition(
            SpriteTransform child,
            SpriteTransform sourceParent)
        {
            var useStaticReference = sourceParent.providesChildSpriteAffine ||
                                     !sourceParent.updatePosition;
            var parentPosition = useStaticReference
                ? sourceParent.spritePosition
                : sourceParent.position;
            var parentScale = useStaticReference
                ? sourceParent.spriteScale
                : sourceParent.scale;
            var parentSkew = useStaticReference
                ? sourceParent.spriteSkew
                : sourceParent.skew;
            if (parentScale == Vector2.zero) parentScale = new Vector2(100f, 100f);

            NativeAffineDecomposition.BuildSourceMatrix(
                parentScale,
                parentSkew,
                out var m00,
                out var m01,
                out var m10,
                out var m11);
            var determinant = m00 * m11 - m01 * m10;
            Assert.That(Mathf.Abs(determinant), Is.GreaterThan(0.0000001f));

            var delta = child.position - parentPosition;
            var x = delta.x;
            var y = -delta.y;
            return new Vector3(
                (m11 * x - m01 * y) / determinant,
                (-m10 * x + m00 * y) / determinant,
                0f);
        }
    }

    internal sealed class CenteredPlacementTestPlant : PlantEntity
    {
        public override string GetChineseName() => "中心落点测试植物";
        public override string GetEnglishName() => "CenteredPlacementTestPlant";
    }
}
