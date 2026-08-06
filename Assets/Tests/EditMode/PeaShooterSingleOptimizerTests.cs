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
