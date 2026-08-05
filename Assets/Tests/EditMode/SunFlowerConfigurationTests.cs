using System.Linq;
using NUnit.Framework;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
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
            Assert.That(definition.CardIconFrame, Is.EqualTo(5));
            Assert.That(definition.Prefab, Is.Not.Null);
            Assert.That(definition.Prefab.GetComponent<SunFlower>(), Is.Not.Null);
            Assert.That(definition.Prefab.GetComponent<SunProducer>(), Is.Not.Null);
        }

        [Test]
        public void Definition_DefaultCardIconFrame_IsFirstFrame()
        {
            Assert.That(new PlantDefinition().CardIconFrame, Is.EqualTo(1));
        }

        [Test]
        public void PresentationPrefab_HasProductionGlowAndParallelBlinkLayers()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            config.Init();

            var prefab = config.GetPlantDefinition(GameConfigObject.PlantType.SunFlower).PresentationPrefab;
            Assert.That(prefab.GetComponent<Blink>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("component/basic/head/face"), Is.Null);
            var head = prefab.transform.Find("component/basic/head/SunFlower_head");
            Assert.That(head, Is.Not.Null);
            var headTransform = head.GetComponent<SpriteTransform>();
            Assert.That(headTransform, Is.Not.Null);
            Assert.That(headTransform.position, Is.EqualTo(new Vector2(37.1f, 35.7f)));
            Assert.That(headTransform.scale, Is.EqualTo(new Vector2(100f, 89f)));
            Assert.That(headTransform.providesChildSpritePosition, Is.True);
            Assert.That(headTransform.childSpritePosition, Is.EqualTo(headTransform.position));
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
    }
}
