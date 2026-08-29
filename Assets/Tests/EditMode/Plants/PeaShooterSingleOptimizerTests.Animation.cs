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
    }
}
