using System.Linq;
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
        private const string BasicPath = "component/basic";
        private const string BasicVisualPath = BasicPath + "/__AffineContent";
        private const string HeadAttachmentPath = BasicVisualPath + "/head";
        private const string HeadAttachmentVisualPath = HeadAttachmentPath + "/__AffineContent";
        private const string HeadPath = HeadAttachmentVisualPath + "/head";
        private const string MouthPath = HeadAttachmentVisualPath + "/mouth";
        private const string SproutPath = HeadAttachmentVisualPath + "/sprout";
        private const string StalkTopPath = BasicVisualPath + "/stalk/__AffineContent/top";
        private const string StalkBottomPath = BasicVisualPath + "/stalk/__AffineContent/bottom";
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
            Assert.That(prefab.transform.Find(HeadPath), Is.Not.Null);
            Assert.That(prefab.transform.Find(MouthPath), Is.Not.Null);
            Assert.That(prefab.transform.Find(SproutPath), Is.Not.Null);
        }

        [Test]
        public void BlinkClip_UsesOpeningFlaFramesAtTwelveFps()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/blink.anim");
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.frameRate, Is.EqualTo(12f));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.False);
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).stopTime,
                Is.EqualTo(4f / 12f).Within(0.000001f));
            Assert.That(AnimationUtility.GetObjectReferenceCurveBindings(clip), Is.Empty);
            Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty);

            const string blinkRoot = HeadPath + "/__AffineContent/blink";
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

            var head = prefab.transform.Find(HeadPath);
            var headTransform = head.GetComponent<SpriteTransform>();
            var headRenderer = headTransform.VisualRenderer;
            Assert.That(headTransform.providesChildSpritePosition, Is.True);
            Assert.That(headTransform.providesChildSpriteAffine, Is.True);
            Assert.That(headTransform.spritePosition, Is.EqualTo(new Vector2(-0.9f, -13.7f)));
            Assert.That(headTransform.spriteScale,
                Is.EqualTo(new Vector2(55.499268f, 50f)));
            Assert.That(headTransform.spriteSkew, Is.EqualTo(Vector2.zero));

            var blink1 = head.Find("__AffineContent/blink/PeaShooter_blink1");
            var blink2 = head.Find("__AffineContent/blink/PeaShooter_blink2");
            AssertBlinkPart(
                blink1,
                "PeaShooter_blink1",
                headRenderer,
                new Vector2(5.85f, -17.9f),
                new Vector2(55.540466f, 55.540466f),
                new Vector2(12.162323f, 8.4f),
                new Vector2(1.0007423f, 1.1108093f));
            AssertBlinkPart(
                blink2,
                "PeaShooter_blink2",
                headRenderer,
                new Vector2(5.7699f, -17.967585f),
                new Vector2(55.499268f, 55.499268f),
                new Vector2(12.017996f, 8.53517f),
                new Vector2(1f, 1.1099854f));
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
            Assert.That(layer.defaultWeight, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(idle.name, Is.EqualTo("blink_idle"));
            Assert.That(idle.motion, Is.Null);
            Assert.That(idle.speed, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(idle.cycleOffset, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(idle.writeDefaultValues, Is.False);
            Assert.That(blink.writeDefaultValues, Is.False);
            Assert.That(blink.behaviours, Is.Empty);

            var enter = idle.transitions.Single();
            Assert.That(enter.destinationState, Is.SameAs(blink));
            Assert.That(enter.hasExitTime, Is.False);
            Assert.That(enter.duration, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(enter.conditions.Single().parameter, Is.EqualTo("blink"));

            var restart = blink.transitions.Single();
            Assert.That(restart.destinationState, Is.SameAs(blink));
            Assert.That(restart.hasExitTime, Is.False);
            Assert.That(restart.canTransitionToSelf, Is.True);
            Assert.That(restart.duration, Is.EqualTo(0f).Within(0.000001f));
            Assert.That(restart.conditions.Single().parameter, Is.EqualTo("blink"));
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
                var blink1 = instance.transform.Find(
                    HeadPath + "/__AffineContent/blink/PeaShooter_blink1");
                var blink2 = instance.transform.Find(
                    HeadPath + "/__AffineContent/blink/PeaShooter_blink2");
                var stalkTop = instance.transform.Find(StalkTopPath)
                    .GetComponent<SpriteTransform>();
                var headPod = instance.transform.Find(HeadPath)
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
                    Is.EqualTo(blinkState));
                Assert.That(animator.GetLayerWeight(blinkLayer), Is.EqualTo(1f).Within(0.000001f));

                Assert.That(blink1.gameObject.activeSelf, Is.False,
                    "The first blink sprite did not apply the clip's final key.");
                Assert.That(blink2.gameObject.activeSelf, Is.False,
                    "The half-closed blink sprite did not apply the clip's final key.");

                animator.SetTrigger("blink");
                animator.Update(0.02f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(blinkLayer).normalizedTime,
                    Is.LessThan(0.2f),
                    "The blink trigger did not restart the held blink state.");
                animator.Update(0.15f);
                animator.Update(0.3f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(blinkLayer).shortNameHash,
                    Is.EqualTo(blinkState),
                    "The blink overlay did not finish a second cycle.");
                Assert.That(blink1.gameObject.activeSelf, Is.False);
                Assert.That(blink2.gameObject.activeSelf, Is.False);

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
            AssertCurveValues(clip, StalkTopPath, "position.x",
                -1.25f, -0.25f, 1.1f, 3.75f, 7.3f, 4.05f, 1.05f, -0.25f, -1.25f);
            AssertCurveValues(clip, StalkTopPath, "position.y",
                2.25f, -1.85f, -2.7f, -1.8f, -0.4f, -1.8f, -2.7f, -1.85f, 2.25f);
            AssertCurveValues(clip, StalkBottomPath, "position.x",
                0.8f, 1.85f, 3.9f, 1.75f, 0.8f);
            AssertCurveValues(clip, StalkBottomPath, "position.y",
                10.15f, 9.2f, 10.65f, 9.2f, 10.15f);
        }

        [Test]
        public void HeadAttachmentAndCurves_UseSourceReanimationCoordinates()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var head = prefab.transform.Find(HeadAttachmentPath).GetComponent<SpriteTransform>();
            Assert.That(head.providesChildSpritePosition, Is.True);
            Assert.That(head.providesChildSpriteAffine, Is.True);
            Assert.That(head.spritePosition.x, Is.EqualTo(-1.85f).Within(0.000001f));
            Assert.That(head.spritePosition.y, Is.EqualTo(0.95f).Within(0.000001f));
            Assert.That(head.spriteScale, Is.EqualTo(new Vector2(100f, 100f)));
            Assert.That(head.spriteSkew, Is.EqualTo(Vector2.zero));

            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/idle.anim");
            AssertFirstCurveValue(idle, HeadAttachmentPath, "position.x", -1.85f);
            AssertFirstCurveValue(idle, HeadAttachmentPath, "position.y", 0.95f);

            var headIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/head_idle.anim");
            AssertFirstCurveValue(headIdle, HeadPath, "position.x", -0.9f);
            AssertFirstCurveValue(headIdle, HeadPath, "position.y", -13.7f);
            AssertFirstCurveValue(headIdle, MouthPath, "position.x", 22.1f);
            AssertFirstCurveValue(headIdle, SproutPath, "position.x", -23.5f);

            var shoot = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/shoot.anim");
            AssertFirstCurveValue(shoot, HeadPath, "position.y", -15.75f);
            AssertFirstCurveValue(shoot, MouthPath, "position.y", -20.25f);
            AssertFirstCurveValue(shoot, SproutPath, "position.x", -23.75f);
        }

        [Test]
        public void HeadSubAnimation_InheritsLiveAttachmentMotion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var animator = instance.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;

                var attachment = instance.transform.Find(HeadAttachmentPath)
                    .GetComponent<SpriteTransform>();
                var head = instance.transform.Find(HeadPath)
                    .GetComponent<SpriteTransform>();
                attachment.Apply();
                head.Apply();
                var localBefore = head.transform.localPosition;
                var worldBefore = head.transform.position;

                attachment.position += new Vector2(2.5f, -1.75f);
                attachment.Apply();
                head.Apply();
                var expectedWorld = attachment.NativeContent.TransformPoint(localBefore);

                Assert.That(head.transform.localPosition.x,
                    Is.EqualTo(localBefore.x).Within(0.0001f));
                Assert.That(head.transform.localPosition.y,
                    Is.EqualTo(localBefore.y).Within(0.0001f));
                Assert.That(head.transform.position.x,
                    Is.EqualTo(expectedWorld.x).Within(0.0001f));
                Assert.That(head.transform.position.y,
                    Is.EqualTo(expectedWorld.y).Within(0.0001f));
                Assert.That(Vector3.Distance(head.transform.position, worldBefore),
                    Is.GreaterThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
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
                var stalkTop = instance.transform.Find(StalkTopPath)
                    .GetComponent<SpriteTransform>();
                var headAttachment = instance.transform.Find(HeadAttachmentPath)
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

                var headPod = instance.transform.Find(HeadPath)
                    .GetComponent<SpriteTransform>();
                var mouth = instance.transform.Find(MouthPath)
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
            Vector2 expectedScale,
            Vector2 expectedLocalPosition,
            Vector2 expectedLocalScale)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(target.gameObject.activeSelf, Is.False);
            var spriteTransform = target.GetComponent<SpriteTransform>();
            var renderer = spriteTransform.VisualRenderer;
            Assert.That(renderer.sprite.name, Is.EqualTo(expectedSpriteName));
            Assert.That(renderer.sharedMaterial, Is.SameAs(headRenderer.sharedMaterial));
            Assert.That(renderer.sortingLayerID, Is.EqualTo(headRenderer.sortingLayerID));
            Assert.That(renderer.sortingOrder, Is.EqualTo(10));
            Assert.That(spriteTransform.position, Is.EqualTo(expectedPosition));
            Assert.That(spriteTransform.scale, Is.EqualTo(expectedScale));
            Assert.That(target.localPosition.x,
                Is.EqualTo(expectedLocalPosition.x).Within(0.0001f));
            Assert.That(target.localPosition.y,
                Is.EqualTo(expectedLocalPosition.y).Within(0.0001f));
            Assert.That(target.localScale.x,
                Is.EqualTo(expectedLocalScale.x).Within(0.0001f));
            Assert.That(target.localScale.y,
                Is.EqualTo(expectedLocalScale.y).Within(0.0001f));
            Assert.That(spriteTransform.updatePosition, Is.True);
            Assert.That(spriteTransform.NativeContent, Is.Not.Null);
        }

        [Test]
        public void Prefab_UsesOneNativeContentLayerPerSpriteTransform()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var spriteTransforms = prefab.GetComponentsInChildren<SpriteTransform>(true);

            Assert.That(spriteTransforms, Is.Not.Empty);
            Assert.That(prefab.transform.Find(BasicPath)?.GetComponent<SpriteTransform>(), Is.Not.Null);
            var headContent = prefab.transform.Find(HeadAttachmentVisualPath);
            Assert.That(headContent, Is.Not.Null);
            Assert.That(headContent.Find("pod"), Is.Null);
            Assert.That(prefab.transform.Find(HeadPath)?.parent, Is.SameAs(headContent));
            Assert.That(prefab.transform.Find(MouthPath)?.parent, Is.SameAs(headContent));
            Assert.That(prefab.transform.Find(SproutPath)?.parent, Is.SameAs(headContent));
            foreach (var spriteTransform in spriteTransforms)
            {
                var path = AnimationUtility.CalculateTransformPath(
                    spriteTransform.transform, prefab.transform);
                Assert.That(spriteTransform.NativeContent, Is.Not.Null,
                    path);
                Assert.That(spriteTransform.NativeContent.parent, Is.SameAs(spriteTransform.transform),
                    path);
                Assert.That(spriteTransform.NativeContent.localPosition, Is.EqualTo(Vector3.zero),
                    path);
                Assert.That(spriteTransform.GetComponent<SpriteRenderer>(), Is.Null,
                    path);
                Assert.That(spriteTransform.transform.childCount, Is.EqualTo(1), path);
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
