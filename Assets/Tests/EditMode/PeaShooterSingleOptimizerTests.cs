using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class PeaShooterSingleOptimizerTests
    {
        private const string PrefabPath = "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab";
        private const string AnimationDirectory = "Assets/Prefab/Plant/PeaShooterSingle/Animation";
        private const string ControllerPath = AnimationDirectory + "/PeaShooterSingle.controller";

        [Test]
        public void ShootClip_HasExactlyOneProjectileEvent()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/shoot.anim");
            var events = AnimationUtility.GetAnimationEvents(clip);
            Assert.That(events.Length, Is.EqualTo(1));
            Assert.That(events[0].functionName, Is.EqualTo("ShootProjectilePea"));
            Assert.That(events[0].time, Is.EqualTo(1f).Within(0.000001f));
        }

        [Test]
        public void ControllerAndPrefab_UseIndependentAnimationLayers()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(controller, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Animator>().runtimeAnimatorController, Is.SameAs(controller));

            AssertStateMotion(controller, "PeaShooterSingle", "idle", "idle");
            AssertStateMotion(controller, "PeaShooterSingle_head", "head_idle", "head_idle");
            AssertStateMotion(controller, "PeaShooterSingle_head_shoot", "shoot", "shoot");

            var shootParameter = controller.parameters.Single(parameter => parameter.name == "shoot");
            Assert.That(shootParameter.type, Is.EqualTo(AnimatorControllerParameterType.Trigger));
            Assert.That(prefab.transform.Find("component/basic/head/pod/head"), Is.Not.Null);
            Assert.That(prefab.transform.Find("component/basic/head/pod/mouth"), Is.Not.Null);
            Assert.That(prefab.transform.Find("component/basic/head/sprout"), Is.Not.Null);
        }

        [Test]
        public void GeneratedTransformCurves_ArePiecewiseLinear()
        {
            foreach (var clipName in new[] { "idle", "head_idle", "shoot" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    $"{AnimationDirectory}/{clipName}.anim");

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    for (var keyIndex = 0; keyIndex < curve.length; keyIndex++)
                    {
                        Assert.That(
                            AnimationUtility.GetKeyLeftTangentMode(curve, keyIndex),
                            Is.EqualTo(AnimationUtility.TangentMode.Linear),
                            $"{clipName}: {binding.path}/{binding.propertyName}, key {keyIndex} left tangent");
                        Assert.That(
                            AnimationUtility.GetKeyRightTangentMode(curve, keyIndex),
                            Is.EqualTo(AnimationUtility.TangentMode.Linear),
                            $"{clipName}: {binding.path}/{binding.propertyName}, key {keyIndex} right tangent");
                    }
                }
            }
        }

        [Test]
        public void IdleStalkPositionCurves_KeepXmlReferenceSamples()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/idle.anim");
            AssertCurveValues(clip, "component/basic/stalk/top", "position.x",
                38.2f, 39.2f, 40.55f, 43.2f, 46.75f, 43.5f, 40.5f, 39.2f, 38.2f);
            AssertCurveValues(clip, "component/basic/stalk/top", "position.y",
                50f, 45.9f, 45.05f, 45.95f, 47.35f, 45.95f, 45.05f, 45.9f, 50f);
            AssertCurveValues(clip, "component/basic/stalk/bottom", "position.x",
                40.25f, 41.3f, 43.35f, 41.2f, 40.25f);
            AssertCurveValues(clip, "component/basic/stalk/bottom", "position.y",
                57.9f, 56.95f, 58.4f, 56.95f, 57.9f);
        }

        private static void AssertCurveValues(
            AnimationClip clip,
            string path,
            string propertyName,
            params float[] expectedValues)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(candidate => candidate.path == path && candidate.propertyName == propertyName);
            var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
            Assert.That(keys.Length, Is.EqualTo(expectedValues.Length));
            for (var index = 0; index < keys.Length; index++)
            {
                Assert.That(keys[index].value, Is.EqualTo(expectedValues[index]).Within(0.000001f),
                    $"{path}/{propertyName}, key {index}");
            }
        }

        private static void AssertStateMotion(
            AnimatorController controller,
            string layerName,
            string stateName,
            string clipName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/{clipName}.anim");
            var state = controller.layers
                .Single(layer => layer.name == layerName)
                .stateMachine.states
                .Select(childState => childState.state)
                .Single(candidate => candidate.name == stateName);
            Assert.That(state.motion, Is.SameAs(clip));
        }
    }
}
