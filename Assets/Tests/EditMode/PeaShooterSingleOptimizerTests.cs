using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    public sealed class PeaShooterSingleOptimizerTests
    {
        private const string PrefabPath = "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab";
        private const string AnimationDirectory = "Assets/Prefab/Plant/PeaShooterSingle/Animation";
        private const string ControllerPath = AnimationDirectory + "/PeaShooterSingle.controller";

        [Test]
        public void SourceHash_IsIndependentOfLineEndings()
        {
            const string lf = "{\n\t\"idle\": []\n}\n";
            const string crlf = "{\r\n\t\"idle\": []\r\n}\r\n";
            const string cr = "{\r\t\"idle\": []\r}\r";

            var expected = PeaShooterSinglePrefabOptimizer.ComputeSourceHash(lf);
            Assert.That(PeaShooterSinglePrefabOptimizer.ComputeSourceHash(crlf), Is.EqualTo(expected));
            Assert.That(PeaShooterSinglePrefabOptimizer.ComputeSourceHash(cr), Is.EqualTo(expected));
        }

        [Test]
        public void ShootClip_HasExactlyOneProjectileEvent()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/shoot.anim");
            var events = AnimationUtility.GetAnimationEvents(clip);
            Assert.That(events.Length, Is.EqualTo(1));
            Assert.That(events[0].functionName, Is.EqualTo("ShootProjectilePea"));
            Assert.That(events[0].time, Is.EqualTo(0.896f).Within(0.000001f));
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

        [Test]
        public void HeadAttachmentAndCurves_UseSourceReanimationCoordinates()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var head = prefab.transform.Find("component/basic/head").GetComponent<SpriteTransform>();
            Assert.That(head.providesChildSpritePosition, Is.True);
            Assert.That(head.spritePosition.x, Is.EqualTo(37.6f).Within(0.000001f));
            Assert.That(head.spritePosition.y, Is.EqualTo(48.7f).Within(0.000001f));

            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/idle.anim");
            AssertFirstCurveValue(idle, "component/basic/head", "position.x", 37.6f);
            AssertFirstCurveValue(idle, "component/basic/head", "position.y", 48.7f);

            var headIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/head_idle.anim");
            AssertFirstCurveValue(headIdle, "component/basic/head/pod/head", "position.x", 38.55f);
            AssertFirstCurveValue(headIdle, "component/basic/head/pod/head", "position.y", 34.05f);
            AssertFirstCurveValue(headIdle, "component/basic/head/pod/mouth", "position.x", 61.55f);
            AssertFirstCurveValue(headIdle, "component/basic/head/sprout", "position.x", 15.95f);

            var shoot = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/shoot.anim");
            AssertFirstCurveValue(shoot, "component/basic/head/pod/head", "position.y", 32f);
            AssertFirstCurveValue(shoot, "component/basic/head/pod/mouth", "position.y", 27.5f);
            AssertFirstCurveValue(shoot, "component/basic/head/sprout", "position.x", 15.7f);
        }

        [Test]
        public void ShootOverlay_CoversContinuouslyPlayingHeadIdleAndReturnsToEmptyState()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var bodyIdle = FindState(controller, "PeaShooterSingle", "idle");
            var headIdle = FindState(controller, "PeaShooterSingle_head", "head_idle");
            var headLayer = controller.layers.Single(layer => layer.name == "PeaShooterSingle_head");
            var shootLayer = controller.layers.Single(layer => layer.name == "PeaShooterSingle_head_shoot");
            var shootIdle = FindState(controller, "PeaShooterSingle_head_shoot", "shoot_idle");
            var shoot = FindState(controller, "PeaShooterSingle_head_shoot", "shoot");

            Assert.That(bodyIdle.writeDefaultValues, Is.False);
            Assert.That(bodyIdle.speed, Is.EqualTo(1.4f).Within(0.000001f));
            Assert.That(headIdle.writeDefaultValues, Is.False);
            Assert.That(headIdle.speed, Is.EqualTo(1.4f).Within(0.000001f));
            Assert.That(headLayer.stateMachine.defaultState, Is.SameAs(headIdle));
            Assert.That(headIdle.transitions, Is.Empty);
            Assert.That(headLayer.stateMachine.states.Any(childState => childState.state.name == "shoot"), Is.False);
            Assert.That(controller.layers.Last().stateMachine, Is.SameAs(shootLayer.stateMachine));
            Assert.That(shootLayer.defaultWeight, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(shootLayer.stateMachine.defaultState, Is.SameAs(shootIdle));
            Assert.That(shootIdle.motion, Is.Null);
            Assert.That(shootIdle.writeDefaultValues, Is.False);
            Assert.That(shoot.writeDefaultValues, Is.False);
            Assert.That(shoot.speed, Is.EqualTo(2.8f).Within(0.000001f));
            Assert.That(shoot.behaviours.OfType<PeaShooterShootOverlayStateBehaviour>().Count(), Is.EqualTo(1));

            var enter = shootIdle.transitions.Single(transition => transition.destinationState == shoot);
            Assert.That(enter.hasExitTime, Is.False);
            Assert.That(enter.duration, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(enter.conditions.Single().parameter, Is.EqualTo("shoot"));
            Assert.That(shoot.transitions, Is.Empty);
        }

        [Test]
        public void ShootTrigger_DoesNotRestartBodyOrRemainLatched()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var animator = instance.GetComponent<Animator>();
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);

                int bodyLayer = animator.GetLayerIndex("PeaShooterSingle");
                int headLayer = animator.GetLayerIndex("PeaShooterSingle_head");
                int shootLayer = animator.GetLayerIndex("PeaShooterSingle_head_shoot");
                int shootTrigger = Animator.StringToHash("shoot");
                int shootState = Animator.StringToHash("shoot");
                int headIdleState = Animator.StringToHash("head_idle");
                int shootIdleState = Animator.StringToHash("shoot_idle");
                var stalkTop = instance.transform.Find("component/basic/stalk/top")
                    .GetComponent<SpriteTransform>();
                var headAttachment = instance.transform.Find("component/basic/head")
                    .GetComponent<SpriteTransform>();

                animator.Update(0.25f);
                float bodyPhaseBeforeShoot = Mathf.Repeat(
                    animator.GetCurrentAnimatorStateInfo(bodyLayer).normalizedTime,
                    1f);
                float headPhaseBeforeShoot = Mathf.Repeat(
                    animator.GetCurrentAnimatorStateInfo(headLayer).normalizedTime,
                    1f);
                float stalkPositionBeforeShoot = stalkTop.position.x;
                float attachmentPositionBeforeShoot = headAttachment.position.x;

                LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException"));
                animator.SetTrigger(shootTrigger);
                animator.Update(0.01f);
                animator.Update(0.01f);

                float bodyPhaseAfterShoot = Mathf.Repeat(
                    animator.GetCurrentAnimatorStateInfo(bodyLayer).normalizedTime,
                    1f);
                float headPhaseAfterShoot = Mathf.Repeat(
                    animator.GetCurrentAnimatorStateInfo(headLayer).normalizedTime,
                    1f);
                Assert.That(bodyPhaseAfterShoot, Is.GreaterThan(bodyPhaseBeforeShoot),
                    "Starting the head shoot animation must not restart the body idle layer.");
                Assert.That(headPhaseAfterShoot, Is.GreaterThan(headPhaseBeforeShoot),
                    "The lower head_idle layer must continue while shoot overlays it.");
                Assert.That(stalkTop.position.x, Is.GreaterThan(stalkPositionBeforeShoot),
                    "The evaluated stalk transform jumped back when the head entered shoot.");
                Assert.That(headAttachment.position.x, Is.GreaterThan(attachmentPositionBeforeShoot),
                    "The evaluated anim_stem attachment jumped back when the head entered shoot.");
                Assert.That(
                    animator.GetCurrentAnimatorStateInfo(shootLayer).shortNameHash == shootState ||
                    animator.GetNextAnimatorStateInfo(shootLayer).shortNameHash == shootState,
                    Is.True,
                    "The shoot trigger was not consumed by the overlay-layer transition.");
                Assert.That(animator.GetLayerWeight(shootLayer), Is.GreaterThan(0f));

                for (int frame = 0; frame < 110; frame++)
                {
                    animator.Update(0.01f);
                }
                Assert.That(animator.IsInTransition(shootLayer), Is.False);
                Assert.That(animator.GetCurrentAnimatorStateInfo(headLayer).shortNameHash,
                    Is.EqualTo(headIdleState),
                    "The continuously playing head_idle layer changed state.");
                Assert.That(animator.GetCurrentAnimatorStateInfo(shootLayer).shortNameHash,
                    Is.EqualTo(shootIdleState),
                    "A latched shoot trigger immediately entered shoot again after returning to the empty overlay.");
                Assert.That(animator.GetLayerWeight(shootLayer), Is.EqualTo(0f).Within(0.000001f));

                var headPod = instance.transform.Find("component/basic/head/pod/head")
                    .GetComponent<SpriteTransform>();
                var mouth = instance.transform.Find("component/basic/head/pod/mouth")
                    .GetComponent<SpriteTransform>();
                Vector4 headPoseBefore = new(
                    headPod.position.x,
                    headPod.position.y,
                    mouth.position.x,
                    mouth.position.y);
                animator.Update(0.15f);
                Vector4 headPoseAfter = new(
                    headPod.position.x,
                    headPod.position.y,
                    mouth.position.x,
                    mouth.position.y);
                Assert.That((headPoseAfter - headPoseBefore).sqrMagnitude, Is.GreaterThan(0.000001f),
                    "The inactive shoot overlay held its last pose and froze the visible head_idle animation.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertFirstCurveValue(
            AnimationClip clip,
            string path,
            string propertyName,
            float expectedValue)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(candidate => candidate.path == path && candidate.propertyName == propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            Assert.That(curve.keys[0].value, Is.EqualTo(expectedValue).Within(0.000001f),
                $"{path}/{propertyName}, first key");
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

        private static AnimatorState FindState(
            AnimatorController controller,
            string layerName,
            string stateName)
        {
            return controller.layers
                .Single(layer => layer.name == layerName)
                .stateMachine.states
                .Select(childState => childState.state)
                .Single(state => state.name == stateName);
        }
    }
}
