using System.Linq;
using NUnit.Framework;
using PvZ.Editor.PrefabPipeline.Plants.PeaShooterSingle;
using PvZ.Gameplay.Plants.Presentation.Animation;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace PvZ.Tests.EditMode.Plants
{
    public sealed partial class PeaShooterSingleOptimizerTests
    {
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
    }
}
