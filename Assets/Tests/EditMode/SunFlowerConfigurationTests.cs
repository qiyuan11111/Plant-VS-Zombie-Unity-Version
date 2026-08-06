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
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class SunFlowerConfigurationTests
    {
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
        public void Plants_UseOriginalShadowPresetsAndOffsets()
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
            Assert.That(sunShroom.ShadowImageTopLeft, Is.EqualTo(new Vector2(-3f, 42f)));
            Assert.That(sunShroom.ShadowCenterLocalPosition, Is.EqualTo(new Vector3(40f, -60f, 0f)));

            foreach (var type in new[]
                     {
                         GameConfigObject.PlantType.PeaShooterSingle,
                         GameConfigObject.PlantType.SunFlower
                     })
            {
                var plant = config.GetPlantDefinition(type).Prefab.GetComponent<PlantEntity>();
                Assert.That(plant, Is.Not.Null, type.ToString());
                Assert.That(plant.ShadowSize, Is.EqualTo(ShadowSizePreset.Large), type.ToString());
                Assert.That(plant.ShadowImageTopLeft,
                    Is.EqualTo(new Vector2(-3f, 51f)), type.ToString());
                Assert.That(plant.ShadowCenterLocalPosition,
                    Is.EqualTo(new Vector3(40f, -69f, 0f)), type.ToString());
            }
        }

        [Test]
        public void AllPlantPrefabs_UseTheirRootAsTheOriginalDrawOrigin()
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
                    Assert.That(basic.transform.localPosition.x,
                        Is.EqualTo(basic.spritePosition.x).Within(0.001f), type.ToString());
                    Assert.That(basic.transform.localPosition.y,
                        Is.EqualTo(-basic.spritePosition.y).Within(0.001f), type.ToString());
                }
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void BoardPlacement_UsesGridLogicalOriginWithoutPlantSpecificCorrection()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            config.Init();

            var boardRoot = new GameObject("BoardRoot").transform;
            var logicalOrigin = new Vector3(100f, 200f, 10f);
            try
            {
                foreach (GameConfigObject.PlantType type in System.Enum.GetValues(
                             typeof(GameConfigObject.PlantType)))
                {
                    var prefab = config.GetPlantDefinition(type).PresentationPrefab;
                    var instance = Object.Instantiate(prefab, boardRoot, false);
                    var plant = instance.GetComponent<PlantEntity>();

                    Assert.That(plant, Is.Not.Null, type.ToString());
                    plant.transform.localScale = Vector3.one;
                    plant.transform.localPosition = logicalOrigin;
                    Assert.That(plant.transform.localPosition, Is.EqualTo(logicalOrigin), type.ToString());
                }
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
            var sprite = shadowSprite.GetComponent<SpriteRenderer>().sprite;
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(86f, 36f)));
        }

        [Test]
        public void PlantDefinition_DefaultCardLayoutMatchesOriginalPacket()
        {
            var definition = new PlantDefinition();
            Assert.That(definition.SeedPacketScale, Is.EqualTo(0.5f));
            Assert.That(definition.SeedPacketDrawOffset, Is.EqualTo(new Vector2(5f, 9f)));
        }

        [Test]
        public void CardPresentation_ConvertsOriginalTopLeftDrawOffsetToPacketLocalSpace()
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
                var drawOriginMethod = typeof(EntityPresentation).GetMethod(
                    "GetSeedPacketDrawOrigin",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(drawOriginMethod, Is.Not.Null);
                var drawOrigin = (Vector3)drawOriginMethod.Invoke(null, new object[]
                {
                    plant,
                    definition.SeedPacketDrawOffset
                });

                Assert.That(drawOrigin, Is.EqualTo(new Vector3(-20f, 26f, 0f)));
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
                    candidate.path == "component/basic/head/SunFlower_head" &&
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
            Assert.That(basicTransform.providesChildSpritePosition, Is.True);
            Assert.That(basicTransform.spritePosition, Is.EqualTo(new Vector2(40.4f, 42.6f)));
            Assert.That(prefab.transform.Find("component/basic/head/face"), Is.Null);
            var head = prefab.transform.Find("component/basic/head/SunFlower_head");
            Assert.That(head, Is.Not.Null);
            var headTransform = head.GetComponent<SpriteTransform>();
            Assert.That(headTransform, Is.Not.Null);
            Assert.That(headTransform.position.x, Is.EqualTo(40.97259f).Within(0.0002f));
            Assert.That(headTransform.position.y, Is.EqualTo(29.84815f).Within(0.0002f));
            Assert.That(headTransform.scale.x, Is.EqualTo(100f).Within(0.0002f));
            Assert.That(headTransform.scale.y, Is.EqualTo(100.70281f).Within(0.0002f));
            Assert.That(headTransform.providesChildSpritePosition, Is.True);
            Assert.That(headTransform.spritePosition, Is.EqualTo(headTransform.position));
            var blink1 = head.Find("blink/SunFlower_blink1");
            var blink2 = head.Find("blink/SunFlower_blink2");
            Assert.That(blink1, Is.Not.Null);
            Assert.That(blink2, Is.Not.Null);
            Assert.That(blink1.gameObject.activeSelf, Is.False);
            Assert.That(blink2.gameObject.activeSelf, Is.False);
            Assert.That(blink1.GetComponent<SpriteTransform>().position,
                Is.EqualTo(new Vector2(39.1f, 31.5f)));
            Assert.That(blink2.GetComponent<SpriteTransform>().position,
                Is.EqualTo(new Vector2(39.1f, 31.5f)));

            foreach (var blink in new[] { blink1, blink2 })
            {
                var sprite = blink.GetComponent<SpriteRenderer>().sprite;
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
                var provider = FindPositionProvider(spriteTransform.transform.parent);
                var origin = provider != null ? provider.spritePosition : Vector2.zero;
                var expectedLocalPosition = new Vector3(
                    spriteTransform.position.x - origin.x,
                    -(spriteTransform.position.y - origin.y),
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
    }
}
