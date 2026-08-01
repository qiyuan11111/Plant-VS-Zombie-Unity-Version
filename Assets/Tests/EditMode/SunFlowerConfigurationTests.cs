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
            Assert.That(faceMotion.Find("SunFlower_head"), Is.Not.Null);
            Assert.That(faceMotion.Find("blink/SunFlower_blink1"), Is.Not.Null);
            Assert.That(faceMotion.Find("blink/SunFlower_blink2"), Is.Not.Null);

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
