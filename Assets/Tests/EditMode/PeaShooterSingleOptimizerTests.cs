using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PvZ.Gameplay.Plants.Abilities;
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
        private const string BlinkSpriteDirectory =
            "Assets/Prefab/Plant/PeaShooterSingle/Sprite";

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
        public void BlinkClip_UsesOpeningFlaFramesAtTwelveFps()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/blink.anim");
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.frameRate, Is.EqualTo(12f));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.False);
            Assert.That(AnimationUtility.GetObjectReferenceCurveBindings(clip), Is.Empty);
            Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty);

            const string blinkRoot = "component/basic/head/pod/head/blink";
            AssertStepCurve(
                clip,
                blinkRoot + "/PeaShooter_blink1",
                0f, 1f, 0f, 1f, 0f);
            AssertStepCurve(
                clip,
                blinkRoot + "/PeaShooter_blink2",
                0f, 0f, 1f, 0f, 0f);
        }

        [Test]
        public void Prefab_HasHeadRelativeBlinkSpritesAndBlinkAbility()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var animator = prefab.GetComponent<Animator>();
            var shooter = prefab.GetComponent<PeaShooterSingle>();
            var blink = prefab.GetComponent<Blink>();
            Assert.That(shooter, Is.Not.Null);
            Assert.That(blink, Is.Not.Null);

            var shooterData = new SerializedObject(shooter);
            var blinkData = new SerializedObject(blink);
            Assert.That(shooterData.FindProperty("blink").objectReferenceValue, Is.SameAs(blink));
            Assert.That(blinkData.FindProperty("animator").objectReferenceValue, Is.SameAs(animator));
            Assert.That(blinkData.FindProperty("minimumIntervalSeconds").floatValue,
                Is.EqualTo(2.5f).Within(0.000001f));
            Assert.That(blinkData.FindProperty("maximumIntervalSeconds").floatValue,
                Is.EqualTo(5.5f).Within(0.000001f));

            var head = prefab.transform.Find("component/basic/head/pod/head");
            var headTransform = head.GetComponent<SpriteTransform>();
            var headRenderer = head.GetComponent<SpriteRenderer>();
            Assert.That(headTransform.providesChildSpritePosition, Is.True);
            Assert.That(headTransform.spritePosition, Is.EqualTo(headTransform.position));

            var blink1 = head.Find("blink/PeaShooter_blink1");
            var blink2 = head.Find("blink/PeaShooter_blink2");
            AssertBlinkPart(
                blink1,
                "PeaShooter_blink1",
                headRenderer,
                new Vector2(45.325665f, 30.587154f),
                new Vector2(100f, 100f));
            AssertBlinkPart(
                blink2,
                "PeaShooter_blink2",
                headRenderer,
                new Vector2(45.2199f, 30.532415f),
                new Vector2(100f, 100f));
        }

        [Test]
        public void BlinkSprites_UseImageCenterAsUnityPivot()
        {
            foreach (var spriteName in new[] { "PeaShooter_blink1", "PeaShooter_blink2" })
            {
                var path = $"{BlinkSpriteDirectory}/{spriteName}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(1f).Within(0.000001f), path);
                Assert.That(sprite.pivot.x, Is.EqualTo(sprite.rect.width * 0.5f).Within(0.000001f), path);
                Assert.That(sprite.pivot.y, Is.EqualTo(sprite.rect.height * 0.5f).Within(0.000001f), path);
            }
        }

        [Test]
        public void Controller_HasIndependentTriggeredBlinkOverlay()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AssertStateMotion(controller, "PeaShooterSingle_blink", "blink", "blink");
            Assert.That(controller.parameters.Single(parameter => parameter.name == "blink").type,
                Is.EqualTo(AnimatorControllerParameterType.Trigger));

            var layer = controller.layers.Single(candidate => candidate.name == "PeaShooterSingle_blink");
            var idle = layer.stateMachine.defaultState;
            var blink = FindState(controller, "PeaShooterSingle_blink", "blink");
            Assert.That(layer.defaultWeight, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(idle.name, Is.EqualTo("blink_idle"));
            Assert.That(idle.motion, Is.Null);
            Assert.That(idle.writeDefaultValues, Is.False);
            Assert.That(blink.writeDefaultValues, Is.False);
            Assert.That(blink.behaviours
                    .OfType<PeaShooterBlinkOverlayStateBehaviour>().Count(),
                Is.EqualTo(1));

            var enter = idle.transitions.Single();
            Assert.That(enter.destinationState, Is.SameAs(blink));
            Assert.That(enter.hasExitTime, Is.False);
            Assert.That(enter.duration, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(enter.conditions.Single().parameter, Is.EqualTo("blink"));

            var exit = blink.transitions.Single();
            Assert.That(exit.destinationState, Is.SameAs(idle));
            Assert.That(exit.hasExitTime, Is.True);
            Assert.That(exit.exitTime, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(exit.duration, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(exit.conditions, Is.Empty);
        }

        [Test]
        public void BlinkTrigger_DoesNotPauseBodyOrHeadIdleLayers()
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
                int blinkLayer = animator.GetLayerIndex("PeaShooterSingle_blink");
                int blinkState = Animator.StringToHash("blink");
                int blinkIdleState = Animator.StringToHash("blink_idle");
                var stalkTop = instance.transform.Find("component/basic/stalk/top")
                    .GetComponent<SpriteTransform>();
                var headPod = instance.transform.Find("component/basic/head/pod/head")
                    .GetComponent<SpriteTransform>();

                animator.Update(0.2f);
                float bodyTimeBefore = animator.GetCurrentAnimatorStateInfo(bodyLayer).normalizedTime;
                float headTimeBefore = animator.GetCurrentAnimatorStateInfo(headLayer).normalizedTime;
                var poseBefore = new Vector2(stalkTop.position.x, headPod.position.y);

                animator.SetTrigger("blink");
                animator.Update(0.01f);
                animator.Update(0.01f);

                Assert.That(
                    animator.GetCurrentAnimatorStateInfo(blinkLayer).shortNameHash == blinkState ||
                    animator.GetNextAnimatorStateInfo(blinkLayer).shortNameHash == blinkState,
                    Is.True);
                Assert.That(animator.GetLayerWeight(blinkLayer), Is.EqualTo(1f).Within(0.000001f));

                // The prefab's existing shoot clip can emit its projectile event while
                // this isolated animation test has no gameplay dependencies attached.
                LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException"));
                animator.Update(0.15f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(bodyLayer).normalizedTime,
                    Is.GreaterThan(bodyTimeBefore));
                Assert.That(animator.GetCurrentAnimatorStateInfo(headLayer).normalizedTime,
                    Is.GreaterThan(headTimeBefore));
                var poseDuringBlink = new Vector2(stalkTop.position.x, headPod.position.y);
                Assert.That((poseDuringBlink - poseBefore).sqrMagnitude,
                    Is.GreaterThan(0.000001f),
                    "Blinking must not hold the evaluated idle pose.");

                animator.Update(0.3f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(blinkLayer).shortNameHash,
                    Is.EqualTo(blinkIdleState));
                Assert.That(animator.GetLayerWeight(blinkLayer), Is.EqualTo(0f).Within(0.000001f));

                var poseAfterBlink = new Vector2(stalkTop.position.x, headPod.position.y);
                animator.Update(0.15f);
                var poseLater = new Vector2(stalkTop.position.x, headPod.position.y);
                Assert.That((poseLater - poseAfterBlink).sqrMagnitude,
                    Is.GreaterThan(0.000001f),
                    "Idle animation must keep moving after the blink overlay returns to idle.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
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

        private static void AssertStepCurve(
            AnimationClip clip,
            string path,
            params float[] expectedValues)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(candidate => candidate.path == path &&
                                     candidate.type == typeof(GameObject) &&
                                     candidate.propertyName == "m_IsActive");
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            Assert.That(curve.keys.Length, Is.EqualTo(expectedValues.Length));
            for (var index = 0; index < expectedValues.Length; index++)
            {
                Assert.That(curve.keys[index].time,
                    Is.EqualTo(index / 12f).Within(0.000001f));
                Assert.That(curve.keys[index].value,
                    Is.EqualTo(expectedValues[index]).Within(0.000001f));
                Assert.That(AnimationUtility.GetKeyLeftTangentMode(curve, index),
                    Is.EqualTo(AnimationUtility.TangentMode.Constant));
                Assert.That(AnimationUtility.GetKeyRightTangentMode(curve, index),
                    Is.EqualTo(AnimationUtility.TangentMode.Constant));
            }
        }

        private static void AssertBlinkPart(
            Transform target,
            string expectedSpriteName,
            SpriteRenderer headRenderer,
            Vector2 expectedPosition,
            Vector2 expectedScale)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(target.gameObject.activeSelf, Is.False);
            var renderer = target.GetComponent<SpriteRenderer>();
            var spriteTransform = target.GetComponent<SpriteTransform>();
            Assert.That(renderer.sprite.name, Is.EqualTo(expectedSpriteName));
            Assert.That(renderer.sharedMaterial, Is.SameAs(headRenderer.sharedMaterial));
            Assert.That(renderer.sortingLayerID, Is.EqualTo(headRenderer.sortingLayerID));
            Assert.That(renderer.sortingOrder, Is.EqualTo(10));
            Assert.That(spriteTransform.position, Is.EqualTo(expectedPosition));
            Assert.That(spriteTransform.scale, Is.EqualTo(expectedScale));
            Assert.That(spriteTransform.updatePosition, Is.True);
            Assert.That(target.GetComponent<CenterInheritedSpritePivot>(), Is.Not.Null);
            spriteTransform.Apply();
            var renderedCenter = renderer.bounds.center;
            Assert.That(target.position.x, Is.EqualTo(renderedCenter.x).Within(0.0001f));
            Assert.That(target.position.y, Is.EqualTo(renderedCenter.y).Within(0.0001f));
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
