using System;
using System.Linq;
using NUnit.Framework;
using PvZ.Gameplay.Zombies.Types;
using PvZ.Presentation.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PvZ.Tests.EditMode.Zombies
{
    public sealed class ZombieEatingAnimationTests
    {
        private const string BasePath = "Assets/Prefab/Zombie/ZombieNormal/";
        private const string PartPath = "component/basic/__AffineContent/";

        [Test]
        public void EatClip_LoopsAtSourceRateAndHasValidBindingsWithoutRootTranslation()
        {
            var prefab = LoadPrefab();
            var clip = LoadClip("eat");
            Assert.That(clip.frameRate, Is.EqualTo(12f));
            Assert.That(clip.length, Is.EqualTo(39f / 12f).Within(0.0001f));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);

            var curves = AnimationUtility.GetCurveBindings(clip);
            Assert.That(curves, Is.Not.Empty, "The eating clip must animate the zombie parts.");
            Assert.That(curves.Any(binding => binding.type == typeof(SpriteTransform)), Is.True);
            foreach (var binding in curves)
            {
                AssertBindingTarget(prefab, binding);
                Assert.That(binding.path, Does.Not.Contain("Zombie_hair"),
                    "Eating should preserve the current prefab's walk/idle part set.");
                Assert.That(binding.path, Does.Not.StartWith("component/anchors"),
                    "Ground anchors must remain independent of the body animation.");

                if (binding.path.Length != 0 || !IsRootTranslation(binding)) continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                for (var frame = 0; frame <= 78; frame++)
                {
                    Assert.That(curve.Evaluate(frame / 24f), Is.EqualTo(0f).Within(0.0001f),
                        $"Eating must not translate the zombie root: {binding.propertyName}.");
                }
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                AssertBindingTarget(prefab, binding);
                foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                {
                    Assert.That(key.value, Is.Not.Null,
                        $"Missing object reference at {binding.path}/{binding.propertyName}.");
                }
            }
        }

        [Test]
        public void Controller_SwitchesBetweenWalkingAndEatingWithoutWaitingForClipEnd()
        {
            var prefab = LoadPrefab();
            var animator = prefab.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.True,
                "Returning to walk must retain the existing walking root motion.");
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            var parameter = controller.parameters.Single(candidate => candidate.name == "isEating");
            Assert.That(parameter.type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameter.defaultBool, Is.False);

            var stateMachine = controller.layers[0].stateMachine;
            var walk = stateMachine.states.Single(child => child.state.name == "walk").state;
            var eat = stateMachine.states.Single(child => child.state.name == "eat").state;
            Assert.That(stateMachine.defaultState, Is.SameAs(walk));
            Assert.That(eat.motion, Is.SameAs(LoadClip("eat")));
            AssertTransition(walk, eat, AnimatorConditionMode.If);
            AssertTransition(eat, walk, AnimatorConditionMode.IfNot);
        }

        [Test]
        public void EatingParts_PreserveStandardHierarchyAndRestoreWalkingHandVisibility()
        {
            var prefab = LoadPrefab();
            Assert.That(prefab.transform.childCount, Is.EqualTo(1));
            var content = prefab.transform.Find(PartPath.TrimEnd('/'));
            Assert.That(content, Is.Not.Null);
            Assert.That(prefab.transform.Find(PartPath + "Zombie_hair"), Is.Null,
                "This animation change should retain the current prefab's walk/idle appearance.");

            var hand = prefab.transform.Find(PartPath + "Zombie_outerarm_hand");
            var eatingHand = prefab.transform.Find(PartPath + "Zombie_outerarm_hand2");
            Assert.That(hand, Is.Not.Null);
            Assert.That(eatingHand, Is.Not.Null);
            Assert.That(eatingHand.parent, Is.SameAs(content));
            var handTransform = eatingHand.GetComponent<SpriteTransform>();
            Assert.That(handTransform, Is.Not.Null);
            Assert.That(handTransform.NativeContent, Is.Not.Null);
            var renderer = handTransform.NativeContent.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(1.25f).Within(0.0001f));

            var shadowAnchor = prefab.transform.Find("component/anchors/shadow");
            Assert.That(shadowAnchor, Is.Not.Null);
            Assert.That(shadowAnchor.localPosition, Is.EqualTo(new Vector3(12.75f, -50f, 0f)));
            Assert.That(prefab.GetComponent<ZombieNormal>().ShadowAnchor, Is.SameAs(shadowAnchor));

            var instance = Object.Instantiate(prefab);
            try
            {
                foreach (var clipName in new[] { "eat", "walk", "eat", "idle" })
                {
                    var clip = LoadClip(clipName);
                    foreach (var time in new[] { 0f, clip.length * 0.5f, clip.length - 1f / 24f })
                    {
                        clip.SampleAnimation(instance, time);
                        Assert.That(instance.transform.Find(PartPath + "Zombie_outerarm_hand").gameObject.activeSelf,
                            Is.EqualTo(clipName != "eat"), $"Original hand visibility in {clipName} at {time}.");
                        Assert.That(instance.transform.Find(PartPath + "Zombie_outerarm_hand2").gameObject.activeSelf,
                            Is.EqualTo(clipName == "eat"), $"Eating hand visibility in {clipName} at {time}.");
                        Assert.That(instance.transform.Find("component/anchors/shadow").localPosition,
                            Is.EqualTo(new Vector3(12.75f, -50f, 0f)));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static GameObject LoadPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath + "ZombieNormal.prefab");
            Assert.That(prefab, Is.Not.Null);
            return prefab;
        }

        private static AnimationClip LoadClip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BasePath + "Animation/" + name + ".anim");
            Assert.That(clip, Is.Not.Null, name);
            return clip;
        }

        private static void AssertBindingTarget(GameObject prefab, EditorCurveBinding binding)
        {
            var target = string.IsNullOrEmpty(binding.path) ? prefab.transform : prefab.transform.Find(binding.path);
            Assert.That(target, Is.Not.Null, $"Missing animation path: {binding.path}.");
            var animatedObject = binding.type == typeof(GameObject)
                ? (Object)target.gameObject
                : target.GetComponent(binding.type);
            Assert.That(animatedObject, Is.Not.Null,
                $"Missing {binding.type.Name} at {binding.path}.");
            using var serialized = new SerializedObject(animatedObject);
            Assert.That(serialized.FindProperty(binding.propertyName), Is.Not.Null,
                $"Missing animated property: {binding.path}/{binding.propertyName}.");
        }

        private static bool IsRootTranslation(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform) && binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal) ||
                   binding.type == typeof(Animator) &&
                   (binding.propertyName.StartsWith("RootT", StringComparison.Ordinal) ||
                    binding.propertyName.StartsWith("MotionT", StringComparison.Ordinal));
        }

        private static void AssertTransition(AnimatorState source, AnimatorState destination, AnimatorConditionMode conditionMode)
        {
            var transition = source.transitions.Single(candidate => candidate.destinationState == destination);
            Assert.That(transition.hasExitTime, Is.False,
                $"{source.name} must react to isEating before reaching the clip end.");
            Assert.That(transition.duration, Is.LessThanOrEqualTo(0.1f));
            Assert.That(transition.conditions.Any(condition =>
                condition.parameter == "isEating" && condition.mode == conditionMode), Is.True);
        }
    }
}
