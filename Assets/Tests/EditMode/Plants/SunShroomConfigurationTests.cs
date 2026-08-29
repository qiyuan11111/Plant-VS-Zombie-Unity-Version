using System.Linq;
using NUnit.Framework;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PvZ.Tests.EditMode.Plants
{
    public sealed class SunShroomConfigurationTests
    {
        private const string PrefabPath = "Assets/Prefab/Plant/SunShroom/SunShroom.prefab";
        private const string BodyContainerPath = "component/basic/body";
        private const string BodyPath = "component/basic/body/SunShroom_body";
        private const string BlinkPath = BodyPath + "/blink";
        private const string SleepPath = BodyPath + "/SunShroom_sleep";
        private const string AnchorPath = "component/anchors/Sun_Anchor";

        [Test]
        public void PresentationPrefab_VisualNodesOwnRawFlaTransforms()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Blink>(), Is.Not.Null);

            var basicTransform = prefab.transform.Find("component/basic").GetComponent<SpriteTransform>();
            Assert.That(basicTransform, Is.Not.Null);
            Assert.That(basicTransform.providesChildSpritePosition, Is.False);
            Assert.That(basicTransform.providesChildSpriteAffine, Is.False);
            Assert.That(basicTransform.spritePosition, Is.EqualTo(Vector2.zero));

            var bodyContainer = prefab.transform.Find(BodyContainerPath);
            var body = prefab.transform.Find(BodyPath);
            var sleep = prefab.transform.Find(SleepPath);
            var blink1 = prefab.transform.Find(BlinkPath + "/SunShroom_blink1");
            var blink2 = prefab.transform.Find(BlinkPath + "/SunShroom_blink2");

            Assert.That(bodyContainer, Is.Not.Null);
            Assert.That(bodyContainer.GetComponent<SpriteTransform>(), Is.Null,
                "The empty body wrapper must not own FLA transform curves.");
            Assert.That(prefab.transform.Find(BodyContainerPath + "/blink"), Is.Null,
                "Blink sprites belong to the body image, not its empty wrapper.");
            Assert.That(prefab.transform.Find("component/basic/sleep"), Is.Null,
                "The sleep replacement belongs directly to the body image.");

            AssertRawTransform(body, new Vector2(-0.575f, 10.475f),
                new Vector2(79.998779296875f, 79.998779296875f));
            var bodyTransform = body.GetComponent<SpriteTransform>();
            Assert.That(bodyTransform.providesChildSpritePosition, Is.True);
            Assert.That(bodyTransform.providesChildSpriteAffine, Is.True);
            Assert.That(bodyTransform.spritePosition, Is.EqualTo(bodyTransform.position));
            Assert.That(bodyTransform.spriteScale, Is.EqualTo(bodyTransform.scale));
            Assert.That(bodyTransform.spriteSkew, Is.EqualTo(bodyTransform.skew));

            AssertRawTransform(blink1, new Vector2(-0.675f, 8.125f), null);
            AssertRawTransform(blink2, new Vector2(-0.825f, 8.025f), null);
            Assert.That(blink1.gameObject.activeSelf, Is.False);
            Assert.That(blink2.gameObject.activeSelf, Is.False);
            Assert.That(blink1.localPosition.x, Is.EqualTo(-0.1250057f).Within(0.0001f));
            Assert.That(blink1.localPosition.y, Is.EqualTo(2.9375443f).Within(0.0001f));
            Assert.That(blink2.localPosition.x, Is.EqualTo(-0.31250566f).Within(0.0001f));
            Assert.That(blink2.localPosition.y, Is.EqualTo(3.0625443f).Within(0.0001f));

            AssertRawTransform(sleep, new Vector2(-0.825f, 8.325f),
                new Vector2(82.14111328125f, 84.442138671875f));
            Assert.That(sleep.gameObject.activeSelf, Is.False);
            Assert.That(sleep.parent, Is.SameAs(body));
            Assert.That(sleep.localPosition.x, Is.EqualTo(-0.31250566f).Within(0.0001f));
            Assert.That(sleep.localPosition.y, Is.EqualTo(2.687535f).Within(0.0001f));

            foreach (var spriteTransform in prefab.GetComponentsInChildren<SpriteTransform>(true))
            {
                Assert.That(spriteTransform.NativeContent, Is.Not.Null, spriteTransform.name);
                Assert.That(spriteTransform.NativeContent.localPosition, Is.EqualTo(Vector3.zero),
                    spriteTransform.name);
                Assert.That(spriteTransform.NativeContent.localScale, Is.EqualTo(Vector3.one),
                    spriteTransform.name);
            }
        }

        [Test]
        public void BlinkSprites_InheritBodyMovement()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);

            try
            {
                var body = instance.transform.Find(BodyPath);
                var blink = instance.transform.Find(BlinkPath + "/SunShroom_blink1");
                Assert.That(body, Is.Not.Null);
                Assert.That(blink, Is.Not.Null);

                var bodyTransform = body.GetComponent<SpriteTransform>();
                var blinkTransform = blink.GetComponent<SpriteTransform>();
                bodyTransform.Apply();
                blinkTransform.Apply();

                var initialBodyWorldPosition = body.position;
                var initialBlinkWorldPosition = blink.position;
                var initialBlinkLocalPosition = blink.localPosition;

                bodyTransform.position += new Vector2(5f, -3f);
                bodyTransform.Apply();
                blinkTransform.Apply();

                var bodyMovement = body.position - initialBodyWorldPosition;
                var blinkMovement = blink.position - initialBlinkWorldPosition;
                Assert.That(blink.localPosition.x,
                    Is.EqualTo(initialBlinkLocalPosition.x).Within(0.0001f));
                Assert.That(blink.localPosition.y,
                    Is.EqualTo(initialBlinkLocalPosition.y).Within(0.0001f));
                Assert.That(blinkMovement.x, Is.EqualTo(bodyMovement.x).Within(0.0001f));
                Assert.That(blinkMovement.y, Is.EqualTo(bodyMovement.y).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PresentationAnimations_TargetExistingVisualNodes()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var clipPaths = new[]
            {
                "Assets/Prefab/Plant/SunShroom/Animation/idle.anim",
                "Assets/Prefab/Plant/SunShroom/Animation/sleep.anim",
                "Assets/Prefab/Plant/SunShroom/Animation/blink.anim",
                "Assets/Prefab/Plant/SunShroom/Animation/nosun.anim",
                "Assets/Prefab/Plant/SunShroom/Animation/sun.anim"
            };

            foreach (var clipPath in clipPaths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                Assert.That(clip, Is.Not.Null, clipPath);

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    Assert.That(prefab.transform.Find(binding.path), Is.Not.Null,
                        $"{clip.name} targets missing hierarchy path '{binding.path}'.");
                }
            }

            var bodyBindings = clipPaths.Take(3)
                .Select(AssetDatabase.LoadAssetAtPath<AnimationClip>)
                .SelectMany(AnimationUtility.GetCurveBindings)
                .Where(binding => binding.propertyName != "m_IsActive")
                .ToArray();
            Assert.That(bodyBindings.Any(binding => binding.path == BodyContainerPath), Is.False);
            Assert.That(bodyBindings.Any(binding => binding.path == BodyPath), Is.True);
        }

        [Test]
        public void Controller_SleepStateUsesSleepClip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var controller = prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
            var sleepClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Prefab/Plant/SunShroom/Animation/sleep.anim");

            Assert.That(controller, Is.Not.Null);
            Assert.That(sleepClip, Is.Not.Null);
            var sleepState = controller.layers
                .Where(layer => layer.name == "SunShroom")
                .SelectMany(layer => layer.stateMachine.states)
                .Select(state => state.state)
                .Single(state => state.name == "sleep");
            Assert.That(sleepState.motion, Is.SameAs(sleepClip));
        }

        [Test]
        public void PresentationPrefab_UsesGameplayProductionAnchorBranch()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var anchor = prefab.transform.Find(AnchorPath);
            Assert.That(anchor, Is.Not.Null);
            Assert.That(prefab.transform.Find(
                "component/basic/head/SunShroom_head/Sun_Anchor"), Is.Null);
            Assert.That(anchor.localPosition,
                Is.EqualTo(new Vector3(-0.025001526f, 6.4749985f, 0f)));
            Assert.That(anchor.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(anchor.localScale, Is.EqualTo(Vector3.one));
            Assert.That(anchor.GetComponent<SpriteTransform>(), Is.Null);

            var producer = prefab.GetComponent<SunProducer>();
            Assert.That(producer, Is.Not.Null);
            var serializedProducer = new SerializedObject(producer);
            Assert.That(serializedProducer.FindProperty("productionAnchor").objectReferenceValue,
                Is.SameAs(anchor));
        }

        private static void AssertRawTransform(
            Transform target,
            Vector2 expectedPosition,
            Vector2? expectedScale)
        {
            Assert.That(target, Is.Not.Null);
            var spriteTransform = target.GetComponent<SpriteTransform>();
            Assert.That(spriteTransform, Is.Not.Null, target.name);
            Assert.That(spriteTransform.position, Is.EqualTo(expectedPosition));
            Assert.That(spriteTransform.updatePosition, Is.True);
            if (expectedScale.HasValue)
            {
                Assert.That(spriteTransform.scale, Is.EqualTo(expectedScale.Value));
            }
        }
    }
}
