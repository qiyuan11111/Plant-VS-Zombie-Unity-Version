using System.Linq;
using NUnit.Framework;
using Prefab.Plant.SunFlower.Script;
using Prefab.Plant.SunShroom.Script;
using Script;
using Script.Util;
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
            Assert.That(prefab.GetComponent<SunFlowerBlink>(), Is.Not.Null);
            var faceMotion = prefab.transform.Find("component/basic/head/face");
            Assert.That(faceMotion, Is.Not.Null);
            Assert.That(faceMotion.GetComponent<SpriteTransform>(), Is.Not.Null);
            var faceSpace = faceMotion.GetComponent<SpriteCoordinateSpace>();
            Assert.That(faceSpace, Is.Not.Null);
            Assert.That(faceSpace.SpritePosition, Is.EqualTo(new Vector2(37.1f, 35.7f)));
            Assert.That(faceMotion.Find("SunFlower_head"), Is.Not.Null);
            var blink1 = faceMotion.Find("blink/SunFlower_blink1");
            var blink2 = faceMotion.Find("blink/SunFlower_blink2");
            Assert.That(blink1, Is.Not.Null);
            Assert.That(blink2, Is.Not.Null);
            Assert.That(blink1.GetComponent<SpriteTransform>().position,
                Is.EqualTo(new Vector2(39.1f, 31.5f)));
            Assert.That(blink2.GetComponent<SpriteTransform>().position,
                Is.EqualTo(new Vector2(39.1f, 31.5f)));

            foreach (var blink in new[] { blink1, blink2 })
            {
                var sprite = blink.GetComponent<SpriteRenderer>().sprite;
                Assert.That(sprite.pivot.x, Is.EqualTo(sprite.rect.width).Within(0.001f));
                Assert.That(sprite.pivot.y, Is.Zero.Within(0.001f));
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
    }
}
